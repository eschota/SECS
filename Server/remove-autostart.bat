@echo off
REM ====================================================================
REM УДАЛЕНИЕ АВТОЗАПУСКА RENDERFIN SERVER
REM Удаляет задачу автозапуска из планировщика Windows
REM ====================================================================

echo.
echo ====================================================================
echo     RENDERFIN SERVER - AUTOSTART REMOVAL
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

echo Removing autostart system...
echo.

REM Запуск PowerShell скрипта для удаления
PowerShell -ExecutionPolicy Bypass -File "%~dp0install-autostart.ps1" -Uninstall

echo.
echo Removal completed. Check the output above for results.
echo.
pause 