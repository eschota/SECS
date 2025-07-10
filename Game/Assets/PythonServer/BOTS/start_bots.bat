@echo off
echo Starting Bot Manager...
echo.
echo Make sure the game server is running on port 3329
echo.
cd /d "%~dp0"
python BOT_MANAGER.PY
pause 