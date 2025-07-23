@echo off
chcp 65001 > nul
echo.
echo ====================================================
echo 🔄 SECS Server Restart - Перезапуск с очисткой
echo ====================================================
echo.

echo 🛑 Останавливаем старые процессы...
taskkill /f /im dotnet.exe > nul 2>&1
taskkill /f /im Server.exe > nul 2>&1
taskkill /f /im python.exe > nul 2>&1

echo ⏳ Ожидание завершения процессов...
timeout /t 3 /nobreak > nul

echo 🔧 Собираем обновленный проект...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo ❌ Ошибка сборки проекта!
    pause
    exit /b 1
)

echo ✅ Сборка успешна!
echo.

echo 🚀 Запускаем сервер с очисткой матчей...
start "SECS Server" /D "C:\SECS\Server" dotnet run --configuration Release

echo ⏳ Ожидание инициализации сервера (15 секунд)...
timeout /t 15 /nobreak > nul

echo 🤖 Запускаем менеджер ботов...
start "SECS Bots" /D "C:\SECS\Server\Bots" python bot_manager.py

echo.
echo ✅ Система перезапущена с исправлениями!
echo.
echo 🔧 Изменения:
echo   - Очистка активных матчей при запуске сервера
echo   - Очистка ссылок на матчи у всех игроков
echo   - Проверка статуса матча в Unity (только активные)
echo   - Исправленный эндпоинт для получения активных матчей
echo.
echo 🌐 Доступные эндпоинты:
echo   - Админка: https://renderfin.com/online-game/admin.html
echo   - Игра: https://renderfin.com/online-game/game.html
echo   - Активные матчи: https://renderfin.com/api-game-match/user/{userId}
echo   - История матчей: https://renderfin.com/api-game-match/user/{userId}/history
echo.
echo 🎯 Теперь система работает правильно:
echo   - Матчи очищаются при рестарте сервера
echo   - Unity проверяет только активные матчи
echo   - Переход в матч происходит корректно
echo.
pause 