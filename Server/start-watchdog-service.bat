@echo off
title Server Watchdog Service Manager
color 0E

echo ================================================
echo  🐕‍🦺 SERVER WATCHDOG - SERVICE MODE
echo ================================================

:menu
echo.
echo Select an option:
echo [1] Start Watchdog Service
echo [2] Stop Watchdog Service
echo [3] View Watchdog Status
echo [4] View Watchdog Logs
echo [5] Exit
echo.
set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" goto start_service
if "%choice%"=="2" goto stop_service
if "%choice%"=="3" goto view_status
if "%choice%"=="4" goto view_logs
if "%choice%"=="5" goto exit
goto menu

:start_service
echo.
echo 🚀 Starting Watchdog Service...
cd /d "C:\SECS\Server"
start /min "Server Watchdog Service" powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File "server-watchdog.ps1" -CheckInterval 15
echo ✅ Watchdog Service started in background
timeout /t 2 /nobreak >nul
goto menu

:stop_service
echo.
echo 🛑 Stopping Watchdog Service...
taskkill /f /im powershell.exe /fi "WINDOWTITLE eq Server Watchdog Service*" 2>nul
taskkill /f /im dotnet.exe 2>nul
echo ✅ Watchdog Service stopped
timeout /t 2 /nobreak >nul
goto menu

:view_status
echo.
echo 📊 Watchdog Service Status:
tasklist /fi "IMAGENAME eq powershell.exe" /fi "WINDOWTITLE eq Server Watchdog Service*" 2>nul | find "powershell.exe" >nul
if %errorlevel%==0 (
    echo ✅ Watchdog Service is RUNNING
) else (
    echo ❌ Watchdog Service is STOPPED
)

tasklist /fi "IMAGENAME eq dotnet.exe" 2>nul | find "dotnet.exe" >nul
if %errorlevel%==0 (
    echo ✅ Server is RUNNING
) else (
    echo ❌ Server is STOPPED
)
echo.
pause
goto menu

:view_logs
echo.
echo 📋 Opening Watchdog Logs...
if exist "C:\SECS\Code\watchdog.log" (
    start notepad "C:\SECS\Code\watchdog.log"
) else (
    echo ❌ Watchdog log file not found
    pause
)
goto menu

:exit
echo.
echo �� Goodbye!
exit /b 