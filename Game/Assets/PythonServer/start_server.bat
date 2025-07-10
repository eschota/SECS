@echo off
echo Starting Game Server...
echo.
echo Production Environment: https://renderfin.com
echo Server Port: 3329
echo.
echo Installing dependencies...
pip install flask flask-cors requests
echo.
echo Starting server...
python _GAME_SERVER_MAIN.py
pause 