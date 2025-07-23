@echo off
chcp 65001 > nul
echo.
echo ====================================================
echo 🎮 SECS Server + Bots - Автозапуск системы
echo ====================================================
echo.

echo 🔧 Запуск сервера...
start "SECS Server" /D "C:\SECS\Server" dotnet run --project Server.csproj

echo ⏳ Ожидание инициализации сервера (15 секунд)...
timeout /t 15 /nobreak > nul

echo 🤖 Запуск менеджера ботов...
start "SECS Bots" /D "C:\SECS\Server\Bots" python bot_manager.py

echo.
echo ✅ Система запущена!
echo.
echo 📊 Открытые окна:
echo   - SECS Server (ASP.NET Core)
echo   - SECS Bots (Python Bot Manager)
echo.
echo 🌐 Доступные эндпоинты:
echo   - Админка: https://renderfin.com/online-game/admin.html
echo   - Игра: https://renderfin.com/online-game/game.html
echo   - API: https://renderfin.com/api-game-*
echo.
echo 🎯 Типы матчей:
echo   - OneVsOne (1v1) = 1
echo   - TwoVsTwo (2v2) = 2  
echo   - FourPlayerFFA (1v1x1x1) = 4
echo.
echo 🔄 Система автоматически:
echo   - Создает и регистрирует ботов
echo   - Запускает поиск матчей по всем типам
echo   - Обрабатывает матчи с timeout
echo   - Обновляет MMR и статистику
echo.
echo 📋 Для остановки: закройте все окна или нажмите Ctrl+C
echo.
pause 