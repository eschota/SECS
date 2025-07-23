@echo off
title Server Watchdog - Development Mode
color 0B

echo ================================================
echo  🐕‍🦺 SERVER WATCHDOG - DEVELOPMENT MODE
echo ================================================
echo  - Auto-restart on code changes
echo  - Monitoring *.cs files
echo  - Fast check interval (5s)
echo  Press Ctrl+C to stop
echo ================================================

cd /d "C:\SECS\Server"
powershell.exe -ExecutionPolicy Bypass -File "server-watchdog.ps1" -CheckInterval 5 -MonitorFiles

pause 