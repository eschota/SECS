# ====================================================================
# BOTS WATCHDOG - ВОТЧДОГ ДЛЯ МЕНЕДЖЕРА БОТОВ
# Следит за работой менеджера ботов и перезапускает его при необходимости
# ====================================================================

param(
    [string]$ServerPath = "C:\SECS\Server",
    [string]$LogPath = "C:\SECS\Code\bots-watchdog.log",
    [int]$CheckInterval = 60,
    [int]$MaxRestarts = 15,
    [int]$RestartCooldown = 300  # 5 минут между перезапусками
)

# Глобальные переменные
$global:BotsProcess = $null
$global:WatchdogRunning = $true
$global:RestartCount = 0
$global:LastRestartTime = Get-Date
$global:BotsManagerPath = Join-Path $ServerPath "Bots\bot_manager.py"
$global:BotsDirectory = Join-Path $ServerPath "Bots"
$global:StartupAttempts = 0

# Функция логирования
function Write-BotsLog {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $prefix = switch ($Level) {
        "INFO"     { "[INFO]" }
        "WARN"     { "[WARN]" }
        "ERROR"    { "[ERROR]" }
        "SUCCESS"  { "[OK]" }
        "RESTART"  { "[RESTART]" }
        "START"    { "[START]" }
        "STOP"     { "[STOP]" }
        "BOTS"     { "[BOTS]" }
        default    { "[INFO]" }
    }
    
    $logEntry = "$timestamp $Level $prefix $Message"
    Write-Host $logEntry -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARN" { "Yellow" }
            "SUCCESS" { "Green" }
            "START" { "Cyan" }
            "STOP" { "Magenta" }
            "BOTS" { "Blue" }
            default { "White" }
        }
    )
    
    try {
        # Создаем директорию для лога если не существует
        $logDir = Split-Path $LogPath -Parent
        if (!(Test-Path $logDir)) {
            New-Item -Path $logDir -ItemType Directory -Force | Out-Null
        }
        
        Add-Content -Path $LogPath -Value $logEntry -Encoding UTF8
    }
    catch {
        Write-Host "[ERROR] Failed to write to bots log: $_" -ForegroundColor Red
    }
}

# Функция запуска менеджера ботов
function Start-BotsManager {
    Write-BotsLog "Starting bots manager..." "START"
    
    try {
        # Проверяем существование файла менеджера
        if (!(Test-Path $global:BotsManagerPath)) {
            Write-BotsLog "Bots manager script not found: $global:BotsManagerPath" "ERROR"
            return $false
        }
        
        # Проверяем Python
        try {
            $pythonVersion = & python --version 2>&1
            Write-BotsLog "Found Python: $pythonVersion" "INFO"
        }
        catch {
            Write-BotsLog "Python not found! Cannot start bots manager." "ERROR"
            return $false
        }
        
        # Запускаем менеджер ботов
        $global:BotsProcess = Start-Process -FilePath "python" `
            -ArgumentList "bot_manager.py" `
            -WorkingDirectory $global:BotsDirectory `
            -PassThru `
            -WindowStyle Hidden
        
        # Ждем немного для инициализации
        Start-Sleep -Seconds 10
        
        if ($global:BotsProcess -and !$global:BotsProcess.HasExited) {
            Write-BotsLog "Bots manager started successfully (PID: $($global:BotsProcess.Id))" "SUCCESS"
            $global:RestartCount++
            $global:LastRestartTime = Get-Date
            $global:StartupAttempts = 0
            return $true
        }
        else {
            Write-BotsLog "Bots manager failed to start" "ERROR"
            return $false
        }
    }
    catch {
        Write-BotsLog "Error starting bots manager: $_" "ERROR"
        return $false
    }
}

# Функция остановки менеджера ботов
function Stop-BotsManager {
    if ($global:BotsProcess -and !$global:BotsProcess.HasExited) {
        Write-BotsLog "Stopping bots manager (PID: $($global:BotsProcess.Id))..." "STOP"
        
        try {
            # Пытаемся graceful shutdown через Ctrl+C
            $global:BotsProcess.CloseMainWindow()
            
            # Ждем 15 секунд
            if (!$global:BotsProcess.WaitForExit(15000)) {
                Write-BotsLog "Graceful shutdown failed, force killing bots manager..." "WARN"
                $global:BotsProcess.Kill()
            }
            
            Write-BotsLog "Bots manager stopped" "SUCCESS"
        }
        catch {
            Write-BotsLog "Error stopping bots manager: $_" "ERROR"
        }
    }
}

# Проверка состояния менеджера ботов
function Test-BotsManagerHealth {
    # Проверяем процесс
    if (!$global:BotsProcess -or $global:BotsProcess.HasExited) {
        return $false
    }
    
    # Проверяем что логи обновляются (косвенная проверка активности)
    try {
        $logFile = Join-Path $global:BotsDirectory "bot_manager.log"
        if (Test-Path $logFile) {
            $logContent = Get-Content $logFile -Tail 5
            $recentActivity = $logContent | Where-Object { $_ -match (Get-Date).ToString("yyyy-MM-dd") }
            if ($recentActivity) {
                return $true
            }
        }
    }
    catch {
        Write-BotsLog "Error checking bots manager activity: $_" "WARN"
    }
    
    return $false
}

# Функция проверки ограничений на перезапуск
function Test-RestartLimits {
    $currentTime = Get-Date
    $timeSinceLastRestart = ($currentTime - $global:LastRestartTime).TotalSeconds
    
    # Проверяем максимальное количество перезапусков
    if ($global:RestartCount -ge $MaxRestarts) {
        Write-BotsLog "Maximum restart limit reached ($MaxRestarts). Entering cooldown..." "WARN"
        
        # Сбрасываем счетчик после длительного периода
        if ($timeSinceLastRestart -gt ($RestartCooldown * 2)) {
            $global:RestartCount = 0
            Write-BotsLog "Restart counter reset after cooldown period" "INFO"
            return $true
        }
        return $false
    }
    
    # Проверяем кулдаун между перезапусками
    if ($timeSinceLastRestart -lt $RestartCooldown) {
        $remainingTime = $RestartCooldown - $timeSinceLastRestart
        Write-BotsLog "Restart cooldown active. Remaining: $([math]::Round($remainingTime)) seconds" "INFO"
        return $false
    }
    
    return $true
}

# Обработчик сигнала остановки
function Stop-BotsWatchdog {
    Write-BotsLog "Bots watchdog shutdown requested..." "BOTS"
    $global:WatchdogRunning = $false
    
    Stop-BotsManager
    
    Write-BotsLog "Bots watchdog stopped" "SUCCESS"
    exit 0
}

# Регистрируем обработчик Ctrl+C
try {
    [Console]::TreatControlCAsInput = $false
    Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-BotsWatchdog }
} catch {
    Write-BotsLog "Warning: Could not register exit handler: $_" "WARN"
}

# ====================================================================
# MAIN BOTS WATCHDOG LOOP
# ====================================================================

Write-BotsLog "===== BOTS WATCHDOG SERVICE STARTED =====" "BOTS"
Write-BotsLog "Server Path: $ServerPath" "INFO"
Write-BotsLog "Bots Directory: $global:BotsDirectory" "INFO"
Write-BotsLog "Log Path: $LogPath" "INFO"
Write-BotsLog "Check Interval: $CheckInterval seconds" "INFO"
Write-BotsLog "Max Restarts: $MaxRestarts" "INFO"
Write-BotsLog "Restart Cooldown: $RestartCooldown seconds" "INFO"

# Первоначальный запуск менеджера ботов
if (!(Start-BotsManager)) {
    Write-BotsLog "Failed to start bots manager on startup!" "ERROR"
    $global:StartupAttempts++
}

# Основной цикл мониторинга
while ($global:WatchdogRunning) {
    try {
        Start-Sleep -Seconds $CheckInterval
        
        # Проверяем состояние менеджера ботов
        $botsHealthy = Test-BotsManagerHealth
        
        if ($botsHealthy) {
            # Все в порядке, записываем успешную проверку
            if (($global:RestartCount % 10) -eq 0 -or $global:RestartCount -eq 0) {
                Write-BotsLog "Bots manager healthy. PID: $($global:BotsProcess.Id), Restarts: $global:RestartCount" "SUCCESS"
            }
        }
        else {
            # Менеджер ботов не работает правильно
            Write-BotsLog "Bots manager is not healthy!" "ERROR"
            
            # Проверяем лимиты перезапуска
            if (Test-RestartLimits) {
                Write-BotsLog "Attempting to restart bots manager..." "RESTART"
                
                # Останавливаем текущий менеджер
                Stop-BotsManager
                Start-Sleep -Seconds 5
                
                # Запускаем новый
                if (Start-BotsManager) {
                    Write-BotsLog "Bots manager restarted successfully" "SUCCESS"
                }
                else {
                    Write-BotsLog "Failed to restart bots manager" "ERROR"
                    $global:StartupAttempts++
                    
                    # Если много неудачных попыток, увеличиваем интервал
                    if ($global:StartupAttempts -gt 5) {
                        Write-BotsLog "Multiple startup failures, increasing check interval..." "WARN"
                        Start-Sleep -Seconds 120  # 2 минуты
                    }
                }
            }
            else {
                Write-BotsLog "Restart limits exceeded, monitoring only..." "WARN"
            }
        }
    }
    catch {
        Write-BotsLog "Error in bots watchdog loop: $_" "ERROR"
        Start-Sleep -Seconds 60
    }
}

Write-BotsLog "===== BOTS WATCHDOG SERVICE STOPPED =====" "BOTS" 