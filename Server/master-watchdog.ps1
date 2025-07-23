# ====================================================================
# MASTER WATCHDOG SERVICE - ГЛАВНЫЙ КОНТРОЛЛЕР СИСТЕМЫ
# Следит за основным вотчдогом и гарантирует работу всей системы
# ====================================================================

param(
    [string]$ServerPath = "C:\SECS\Server",
    [string]$LogPath = "C:\SECS\Code\master-watchdog.log",
    [int]$CheckInterval = 30,
    [int]$MaxRestarts = 10,
    [int]$RestartCooldown = 300  # 5 минут между перезапусками
)

# Глобальные переменности
$global:WatchdogProcess = $null
$global:MasterRunning = $true
$global:RestartCount = 0
$global:LastRestartTime = Get-Date
$global:WatchdogScript = Join-Path $ServerPath "server-watchdog-fixed.ps1"
$global:StartupAttempts = 0

# Функция логирования
function Write-MasterLog {
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
        "MASTER"   { "[MASTER]" }
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
            "MASTER" { "Blue" }
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
        Write-Host "[ERROR] Failed to write to master log: $_" -ForegroundColor Red
    }
}

# Функция запуска основного вотчдога
function Start-Watchdog {
    Write-MasterLog "Starting primary watchdog..." "START"
    
    try {
        # Проверяем существование скрипта
        if (!(Test-Path $global:WatchdogScript)) {
            Write-MasterLog "Watchdog script not found: $global:WatchdogScript" "ERROR"
            return $false
        }
        
        # Запускаем вотчдог
        $global:WatchdogProcess = Start-Process -FilePath "powershell.exe" `
            -ArgumentList "-ExecutionPolicy Bypass -File `"$global:WatchdogScript`" -ServerPath `"$ServerPath`" -CheckInterval 10 -MonitorFiles" `
            -WorkingDirectory $ServerPath `
            -PassThru `
            -WindowStyle Hidden
        
        # Ждем немного для инициализации
        Start-Sleep -Seconds 5
        
        if ($global:WatchdogProcess -and !$global:WatchdogProcess.HasExited) {
            Write-MasterLog "Primary watchdog started successfully (PID: $($global:WatchdogProcess.Id))" "SUCCESS"
            $global:RestartCount++
            $global:LastRestartTime = Get-Date
            $global:StartupAttempts = 0
            return $true
        }
        else {
            Write-MasterLog "Primary watchdog failed to start" "ERROR"
            return $false
        }
    }
    catch {
        Write-MasterLog "Error starting primary watchdog: $_" "ERROR"
        return $false
    }
}

# Функция остановки вотчдога
function Stop-Watchdog {
    if ($global:WatchdogProcess -and !$global:WatchdogProcess.HasExited) {
        Write-MasterLog "Stopping primary watchdog (PID: $($global:WatchdogProcess.Id))..." "STOP"
        
        try {
            # Пытаемся graceful shutdown
            $global:WatchdogProcess.CloseMainWindow()
            
            # Ждем 15 секунд
            if (!$global:WatchdogProcess.WaitForExit(15000)) {
                Write-MasterLog "Graceful shutdown failed, force killing watchdog..." "WARN"
                $global:WatchdogProcess.Kill()
            }
            
            Write-MasterLog "Primary watchdog stopped" "SUCCESS"
        }
        catch {
            Write-MasterLog "Error stopping primary watchdog: $_" "ERROR"
        }
    }
}

# Проверка состояния вотчдога
function Test-WatchdogHealth {
    # Проверяем процесс
    if (!$global:WatchdogProcess -or $global:WatchdogProcess.HasExited) {
        return $false
    }
    
    # Проверяем что сервер отвечает (значит вотчдог работает правильно)
    try {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
        $response = Invoke-WebRequest -Uri "https://renderfin.com/api-game-queue/stats" -TimeoutSec 10 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            return $true
        }
    }
    catch {
        Write-MasterLog "Server health check failed via watchdog: $_" "WARN"
    }
    
    return $false
}

# Функция проверки ограничений на перезапуск
function Test-RestartLimits {
    $currentTime = Get-Date
    $timeSinceLastRestart = ($currentTime - $global:LastRestartTime).TotalSeconds
    
    # Проверяем максимальное количество перезапусков
    if ($global:RestartCount -ge $MaxRestarts) {
        Write-MasterLog "Maximum restart limit reached ($MaxRestarts). Entering cooldown..." "WARN"
        
        # Сбрасываем счетчик после длительного периода
        if ($timeSinceLastRestart -gt ($RestartCooldown * 2)) {
            $global:RestartCount = 0
            Write-MasterLog "Restart counter reset after cooldown period" "INFO"
            return $true
        }
        return $false
    }
    
    # Проверяем кулдаун между перезапусками
    if ($timeSinceLastRestart -lt $RestartCooldown) {
        $remainingTime = $RestartCooldown - $timeSinceLastRestart
        Write-MasterLog "Restart cooldown active. Remaining: $([math]::Round($remainingTime)) seconds" "INFO"
        return $false
    }
    
    return $true
}

# Обработчик сигнала остановки
function Stop-Master {
    Write-MasterLog "Master watchdog shutdown requested..." "MASTER"
    $global:MasterRunning = $false
    
    Stop-Watchdog
    
    Write-MasterLog "Master watchdog stopped" "SUCCESS"
    exit 0
}

# Регистрируем обработчик Ctrl+C
try {
    [Console]::TreatControlCAsInput = $false
    Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-Master }
} catch {
    Write-MasterLog "Warning: Could not register exit handler: $_" "WARN"
}

# ====================================================================
# MAIN MASTER WATCHDOG LOOP
# ====================================================================

Write-MasterLog "===== MASTER WATCHDOG SERVICE STARTED =====" "MASTER"
Write-MasterLog "Server Path: $ServerPath" "INFO"
Write-MasterLog "Log Path: $LogPath" "INFO"
Write-MasterLog "Check Interval: $CheckInterval seconds" "INFO"
Write-MasterLog "Max Restarts: $MaxRestarts" "INFO"
Write-MasterLog "Restart Cooldown: $RestartCooldown seconds" "INFO"

# Первоначальный запуск вотчдога
if (!(Start-Watchdog)) {
    Write-MasterLog "Failed to start primary watchdog on startup!" "ERROR"
    $global:StartupAttempts++
}

# Основной цикл мониторинга
while ($global:MasterRunning) {
    try {
        Start-Sleep -Seconds $CheckInterval
        
        # Проверяем состояние вотчдога
        $watchdogHealthy = Test-WatchdogHealth
        
        if ($watchdogHealthy) {
            # Все в порядке, записываем успешную проверку
            if (($global:RestartCount % 10) -eq 0 -or $global:RestartCount -eq 0) {
                Write-MasterLog "System healthy. Watchdog PID: $($global:WatchdogProcess.Id), Restarts: $global:RestartCount" "SUCCESS"
            }
        }
        else {
            # Вотчдог не работает правильно
            Write-MasterLog "Primary watchdog is not healthy!" "ERROR"
            
            # Проверяем лимиты перезапуска
            if (Test-RestartLimits) {
                Write-MasterLog "Attempting to restart primary watchdog..." "RESTART"
                
                # Останавливаем текущий вотчдог
                Stop-Watchdog
                Start-Sleep -Seconds 3
                
                # Запускаем новый
                if (Start-Watchdog) {
                    Write-MasterLog "Primary watchdog restarted successfully" "SUCCESS"
                }
                else {
                    Write-MasterLog "Failed to restart primary watchdog" "ERROR"
                    $global:StartupAttempts++
                    
                    # Если много неудачных попыток, увеличиваем интервал
                    if ($global:StartupAttempts -gt 5) {
                        Write-MasterLog "Multiple startup failures, increasing check interval..." "WARN"
                        Start-Sleep -Seconds 60
                    }
                }
            }
            else {
                Write-MasterLog "Restart limits exceeded, monitoring only..." "WARN"
            }
        }
    }
    catch {
        Write-MasterLog "Error in master watchdog loop: $_" "ERROR"
        Start-Sleep -Seconds 30
    }
}

Write-MasterLog "===== MASTER WATCHDOG SERVICE STOPPED =====" "MASTER" 