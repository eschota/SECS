@echo off
REM ====================================================================
REM УСТАНОВКА АВТОЗАПУСКА RENDERFIN SERVER
REM Запускает PowerShell скрипт для настройки автозапуска системы
REM ====================================================================

echo.
echo ====================================================================
echo     RENDERFIN SERVER - AUTOSTART INSTALLER
echo ====================================================================
echo.

REM Проверка прав администратора
NET SESSION >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script requires administrator privileges!
    echo Please run as Administrator and try again.
    echo.
    pause
    exit /b 1
)

echo Installing autostart system...
echo.

REM Запуск PowerShell скрипта
PowerShell -ExecutionPolicy Bypass -File "%~dp0install-autostart.ps1" -Force

echo.
echo Installation completed. Check the output above for results.
echo.
pause 