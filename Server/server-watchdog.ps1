# ====================================================================
# 🐕‍🦺 SERVER WATCHDOG DAEMON
# Автоматический мониторинг и перезапуск сервера
# ====================================================================

param(
    [string]$ServerPath = "C:\SECS\Server",
    [string]$LogPath = "C:\SECS\Code\watchdog.log",
    [int]$CheckInterval = 10,
    [switch]$MonitorFiles = $false
)

# Глобальные переменности
$global:ServerProcess = $null
$global:WatchdogRunning = $true
$global:RestartCount = 0
$global:LastRestartTime = Get-Date
$global:FileWatcher = $null

# Функция логирования
function Write-WatchdogLog {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $emoji = switch ($Level) {
        "INFO"  { "[INFO]" }
        "WARN"  { "[WARN]" }
        "ERROR" { "[ERROR]" }
        "SUCCESS" { "[OK]" }
        "RESTART" { "[RESTART]" }
        "START" { "[START]" }
        "STOP"  { "[STOP]" }
        default { "[INFO]" }
    }
    
    $logEntry = "$timestamp $Level $emoji $Message"
    Write-Host $logEntry
    
    try {
        Add-Content -Path $LogPath -Value $logEntry -Encoding UTF8
    }
    catch {
        Write-Host "❌ Failed to write to log file: $_"
    }
}

# Функция запуска сервера
function Start-Server {
    Write-WatchdogLog "Starting server..." "START"
    
    try {
        # Переходим в директорию сервера
        Set-Location $ServerPath
        
        # Запускаем сервер через dotnet run
        $global:ServerProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $ServerPath -PassThru -WindowStyle Hidden
        
        # Ждем немного чтобы процесс запустился
        Start-Sleep -Seconds 3
        
        if ($global:ServerProcess -and !$global:ServerProcess.HasExited) {
            Write-WatchdogLog "Server started successfully (PID: $($global:ServerProcess.Id))" "SUCCESS"
            $global:RestartCount++
            $global:LastRestartTime = Get-Date
            return $true
        }
        else {
            Write-WatchdogLog "Server failed to start" "ERROR"
            return $false
        }
    }
    catch {
        Write-WatchdogLog "❌ Error starting server: $_" "ERROR"
        return $false
    }
}

# Функция остановки сервера
function Stop-Server {
    if ($global:ServerProcess -and !$global:ServerProcess.HasExited) {
        Write-WatchdogLog "🛑 Stopping server (PID: $($global:ServerProcess.Id))..." "STOP"
        
        try {
            # Пытаемся graceful shutdown
            $global:ServerProcess.CloseMainWindow()
            
            # Ждем 10 секунд
            if (!$global:ServerProcess.WaitForExit(10000)) {
                Write-WatchdogLog "⚠️ Graceful shutdown failed, force killing..." "WARN"
                $global:ServerProcess.Kill()
            }
            
            Write-WatchdogLog "✅ Server stopped" "SUCCESS"
        }
        catch {
            Write-WatchdogLog "❌ Error stopping server: $_" "ERROR"
        }
    }
}

# Проверка состояния сервера
function Test-ServerHealth {
    # Проверяем процесс
    if (!$global:ServerProcess -or $global:ServerProcess.HasExited) {
        return $false
    }
    
    # Проверяем HTTP эндпоинт
    try {
        $response = Invoke-WebRequest -Uri "https://renderfin.com/api-game-queue/stats" -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            return $true
        }
    }
    catch {
        Write-WatchdogLog "⚠️ Server health check failed: $_" "WARN"
    }
    
    return $false
}

# Настройка мониторинга файлов
function Start-FileWatcher {
    if (!$MonitorFiles) { return }
    
    Write-WatchdogLog "📁 Starting file watcher..." "INFO"
    
    try {
        $global:FileWatcher = New-Object System.IO.FileSystemWatcher
        $global:FileWatcher.Path = $ServerPath
        $global:FileWatcher.Filter = "*.cs"
        $global:FileWatcher.IncludeSubdirectories = $true
        $global:FileWatcher.EnableRaisingEvents = $true
        
        # Регистрируем обработчик событий
        Register-ObjectEvent -InputObject $global:FileWatcher -EventName "Changed" -Action {
            $filePath = $Event.SourceEventArgs.FullPath
            Write-WatchdogLog "📝 File changed: $filePath - scheduling restart..." "INFO"
            
            # Небольшая задержка чтобы избежать множественных перезапусков
            Start-Sleep -Seconds 2
            
            # Перезапускаем сервер
            Stop-Server
            Start-Sleep -Seconds 1
            Start-Server
        }
        
        Write-WatchdogLog "✅ File watcher started" "SUCCESS"
    }
    catch {
        Write-WatchdogLog "❌ Failed to start file watcher: $_" "ERROR"
    }
}

# Остановка мониторинга файлов
function Stop-FileWatcher {
    if ($global:FileWatcher) {
        Write-WatchdogLog "📁 Stopping file watcher..." "INFO"
        $global:FileWatcher.EnableRaisingEvents = $false
        $global:FileWatcher.Dispose()
        $global:FileWatcher = $null
        
        # Удаляем обработчики событий
        Get-EventSubscriber | Where-Object { $_.SourceObject -eq $global:FileWatcher } | Unregister-Event
    }
}

# Обработчик сигнала остановки
function Stop-Watchdog {
    Write-WatchdogLog "🛑 Watchdog shutdown requested..." "STOP"
    $global:WatchdogRunning = $false
    
    Stop-FileWatcher
    Stop-Server
    
    Write-WatchdogLog "✅ Watchdog stopped" "SUCCESS"
    exit 0
}

# Регистрируем обработчик Ctrl+C
[Console]::TreatControlCAsInput = $false
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-Watchdog }

# ====================================================================
# MAIN WATCHDOG LOOP
# ====================================================================

Write-WatchdogLog "🐕‍🦺 ===== SERVER WATCHDOG STARTED =====" "START"
Write-WatchdogLog "📂 Server Path: $ServerPath" "INFO"
Write-WatchdogLog "📋 Log Path: $LogPath" "INFO"
Write-WatchdogLog "⏱️ Check Interval: $CheckInterval seconds" "INFO"
Write-WatchdogLog "📁 File Monitoring: $MonitorFiles" "INFO"

# Запускаем мониторинг файлов если включен
if ($MonitorFiles) {
    Start-FileWatcher
}

# Первоначальный запуск сервера
if (!(Start-Server)) {
    Write-WatchdogLog "❌ Failed to start server initially. Exiting..." "ERROR"
    exit 1
}

# Основной цикл мониторинга
while ($global:WatchdogRunning) {
    try {
        Start-Sleep -Seconds $CheckInterval
        
        # Проверяем здоровье сервера
        if (!(Test-ServerHealth)) {
            Write-WatchdogLog "💔 Server health check failed - restarting..." "RESTART"
            
            Stop-Server
            Start-Sleep -Seconds 2
            
            # Пытаемся перезапустить до 3 раз
            $retryCount = 0
            $maxRetries = 3
            
            while ($retryCount -lt $maxRetries -and !(Start-Server)) {
                $retryCount++
                Write-WatchdogLog "⏳ Restart attempt $retryCount/$maxRetries failed, retrying..." "WARN"
                Start-Sleep -Seconds 5
            }
            
            if ($retryCount -eq $maxRetries) {
                Write-WatchdogLog "❌ Failed to restart server after $maxRetries attempts" "ERROR"
                # Можно добавить отправку уведомлений или другие действия
            }
        }
        
        # Статистика каждые 10 минут
        $timeSinceLastRestart = (Get-Date) - $global:LastRestartTime
        if ($timeSinceLastRestart.TotalMinutes -gt 10 -and $global:RestartCount -gt 0) {
            Write-WatchdogLog "📊 Uptime: $($timeSinceLastRestart.ToString('hh\:mm\:ss')) | Restarts: $global:RestartCount" "INFO"
            $global:LastRestartTime = Get-Date
        }
    }
    catch {
        Write-WatchdogLog "❌ Watchdog loop error: $_" "ERROR"
        Start-Sleep -Seconds $CheckInterval
    }
}

Write-WatchdogLog "🐕‍🦺 ===== SERVER WATCHDOG STOPPED =====" "STOP" 