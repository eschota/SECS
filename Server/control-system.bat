@echo off
REM ====================================================================
REM УПРАВЛЕНИЕ СИСТЕМОЙ RENDERFIN SERVER
REM Позволяет управлять задачей автозапуска вотчдога
REM ====================================================================

setlocal enabledelayedexpansion

echo.
echo ====================================================================
echo     RENDERFIN SERVER - SYSTEM CONTROL
echo ====================================================================

:MENU
echo.
echo Choose an action:
echo.
echo [1] Start System        - Запустить систему вотчдога
echo [2] Stop System         - Остановить систему вотчдога
echo [3] Check Status        - Проверить состояние системы
echo [4] View Logs           - Просмотреть логи
echo [5] Restart System      - Перезапустить систему
echo [6] Enable Autostart    - Включить автозапуск
echo [7] Disable Autostart   - Отключить автозапуск
echo [8] Test Server         - Проверить работу сервера
echo [9] Exit                - Выход
echo.
set /p choice="Enter your choice (1-9): "

if "%choice%"=="1" goto START_SYSTEM
if "%choice%"=="2" goto STOP_SYSTEM
if "%choice%"=="3" goto CHECK_STATUS
if "%choice%"=="4" goto VIEW_LOGS
if "%choice%"=="5" goto RESTART_SYSTEM
if "%choice%"=="6" goto ENABLE_AUTOSTART
if "%choice%"=="7" goto DISABLE_AUTOSTART
if "%choice%"=="8" goto TEST_SERVER
if "%choice%"=="9" goto EXIT

echo Invalid choice. Please try again.
goto MENU

:START_SYSTEM
echo.
echo Starting RenderFin Server Watchdog system...
PowerShell -ExecutionPolicy Bypass -Command "Start-ScheduledTask -TaskName 'RenderFin-Server-Watchdog' -ErrorAction SilentlyContinue"
timeout /t 3 /nobreak >nul
echo System start command sent.
goto MENU

:STOP_SYSTEM
echo.
echo Stopping RenderFin Server Watchdog system...
PowerShell -ExecutionPolicy Bypass -Command "Stop-ScheduledTask -TaskName 'RenderFin-Server-Watchdog' -ErrorAction SilentlyContinue"
timeout /t 3 /nobreak >nul
echo System stop command sent.
goto MENU

:CHECK_STATUS
echo.
echo Checking system status...
echo.
PowerShell -ExecutionPolicy Bypass -Command "
$task = Get-ScheduledTask -TaskName 'RenderFin-Server-Watchdog' -ErrorAction SilentlyContinue
if ($task) {
    $taskInfo = Get-ScheduledTaskInfo -TaskName 'RenderFin-Server-Watchdog'
    Write-Host 'Task Status:' $task.State -ForegroundColor Green
    Write-Host 'Last Run:' $taskInfo.LastRunTime -ForegroundColor Cyan
    Write-Host 'Last Result:' $taskInfo.LastTaskResult -ForegroundColor Yellow
    Write-Host 'Next Run:' $taskInfo.NextRunTime -ForegroundColor Cyan
} else {
    Write-Host 'Autostart task not found!' -ForegroundColor Red
}
"
echo.
echo Checking server health...
PowerShell -ExecutionPolicy Bypass -Command "
try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    $response = Invoke-WebRequest -Uri 'https://renderfin.com/api-game-queue/stats' -TimeoutSec 10 -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host 'Server is responding: OK' -ForegroundColor Green
    } else {
        Write-Host 'Server responded with status:' $response.StatusCode -ForegroundColor Yellow
    }
} catch {
    Write-Host 'Server health check failed:' $_.Exception.Message -ForegroundColor Red
}
"
goto MENU

:VIEW_LOGS
echo.
echo Recent logs from master watchdog:
echo.
PowerShell -ExecutionPolicy Bypass -Command "
if (Test-Path 'C:\SECS\Code\master-watchdog.log') {
    Get-Content 'C:\SECS\Code\master-watchdog.log' -Tail 20
} else {
    Write-Host 'Master watchdog log not found' -ForegroundColor Red
}
"
echo.
echo Recent logs from primary watchdog:
echo.
PowerShell -ExecutionPolicy Bypass -Command "
if (Test-Path 'C:\SECS\Code\watchdog.log') {
    Get-Content 'C:\SECS\Code\watchdog.log' -Tail 20
} else {
    Write-Host 'Primary watchdog log not found' -ForegroundColor Red
}
"
goto MENU

:RESTART_SYSTEM
echo.
echo Restarting RenderFin Server Watchdog system...
PowerShell -ExecutionPolicy Bypass -Command "
Stop-ScheduledTask -TaskName 'RenderFin-Server-Watchdog' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Start-ScheduledTask -TaskName 'RenderFin-Server-Watchdog' -ErrorAction SilentlyContinue
"
echo System restart command sent.
goto MENU

:ENABLE_AUTOSTART
echo.
echo Enabling autostart (requires Administrator privileges)...
PowerShell -ExecutionPolicy Bypass -File "%~dp0install-autostart.ps1" -Force
goto MENU

:DISABLE_AUTOSTART
echo.
echo Disabling autostart (requires Administrator privileges)...
PowerShell -ExecutionPolicy Bypass -File "%~dp0install-autostart.ps1" -Uninstall
goto MENU

:TEST_SERVER
echo.
echo Testing server connectivity...
echo.
PowerShell -ExecutionPolicy Bypass -Command "
Write-Host 'Testing server endpoints...' -ForegroundColor Cyan
try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    
    Write-Host 'Testing stats endpoint...' -ForegroundColor Yellow
    $response = Invoke-WebRequest -Uri 'https://renderfin.com/api-game-queue/stats' -TimeoutSec 10 -UseBasicParsing
    Write-Host 'Stats: HTTP' $response.StatusCode -ForegroundColor Green
    
    Write-Host 'Testing player endpoints...' -ForegroundColor Yellow
    $response = Invoke-WebRequest -Uri 'https://renderfin.com/api-game-queue/1/status' -TimeoutSec 10 -UseBasicParsing
    Write-Host 'Player Status: HTTP' $response.StatusCode -ForegroundColor Green
    
    Write-Host 'All endpoints responding correctly!' -ForegroundColor Green
} catch {
    Write-Host 'Server test failed:' $_.Exception.Message -ForegroundColor Red
}
"
goto MENU

:EXIT
echo.
echo Goodbye!
timeout /t 2 /nobreak >nul
exit /b 0 