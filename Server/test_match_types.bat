@echo off
chcp 65001 > nul
echo.
echo ====================================================
echo 🔬 SECS Match Types Test - Тестирование типов матчей
echo ====================================================
echo.

echo 📊 Проверка статистики очереди...
curl -s -k "https://renderfin.com/api-game-queue/stats" | python -m json.tool

echo.
echo 🎯 Типы матчей:
echo   1 = OneVsOne (1v1)
echo   2 = TwoVsTwo (2v2)  
echo   4 = FourPlayerFFA (1v1x1x1)
echo.

echo 📈 Проверка активных матчей...
curl -s -k "https://renderfin.com/api-game-match/active" | python -m json.tool

echo.
echo 🏆 Админская статистика...
curl -s -k "https://renderfin.com/admin/stats" | python -m json.tool

echo.
echo ✅ Тест завершен!
echo.
echo 🌐 Доступные страницы:
echo   - Админка: https://renderfin.com/online-game/admin.html
echo   - Игра: https://renderfin.com/online-game/game.html
echo.
pause 