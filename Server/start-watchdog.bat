@echo off
title Server Watchdog - Monitoring Mode
color 0A

echo ================================================
echo  🐕‍🦺 SERVER WATCHDOG - MONITORING MODE
echo ================================================
echo  Starting automatic server monitoring...
echo  Press Ctrl+C to stop
echo ================================================

cd /d "C:\SECS\Server"
powershell.exe -ExecutionPolicy Bypass -File "server-watchdog-fixed.ps1" -CheckInterval 10

pause 