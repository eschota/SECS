# 🎯 SECS Match Types System - Система типов матчей

## ✅ Исправления в системе

### 🎮 Unity Client (Игрок)
- **Проблема**: Игрок мог искать только матчи 1v1
- **Решение**: Добавлены кнопки выбора типа матча в UI
- **Результат**: Игрок может выбрать конкретный тип матча (1v1, 2v2, 4-player FFA)

### 🤖 Bots (Боты)
- **Статус**: ✅ Работают правильно
- **Поведение**: Случайно выбирают тип матча из доступных для их поведения
- **Типы**: Каждый бот может искать любой тип матча (1v1, 2v2, 4-player FFA)

### 🖥️ Server (Сервер)
- **Статус**: ✅ Работает правильно
- **Логика**: Принимает КОНКРЕТНЫЙ тип матча и обрабатывает поиск строго по типам
- **Матчмейкинг**: Отдельная обработка для каждого типа матча

## 🔧 КРИТИЧЕСКИЕ ИСПРАВЛЕНИЯ

### 🗑️ Проблема: Матчи не очищались при рестарте сервера
- **Проблема**: Старые матчи оставались в базе данных после рестарта
- **Результат**: Unity видел завершённые матчи как активные
- **Решение**: Добавлена очистка матчей при запуске сервера в `main.cs`

### 📊 Проблема: Unity не проверял статус матча
- **Проблема**: Unity проверял только `matchId > 0`, игнорируя статус матча
- **Результат**: Переход в завершённые матчи
- **Решение**: Добавлена проверка `gameMatch.status == 0` (только InProgress)

### 🔗 Проблема: Эндпоинт возвращал все матчи
- **Проблема**: `/api-game-match/user/{userId}` возвращал все матчи пользователя
- **Результат**: Unity видел завершённые матчи как активные
- **Решение**: Эндпоинт теперь возвращает только активные матчи

## 🎯 Типы матчей

| Тип | Код | Название | Статус | Описание |
|-----|-----|----------|---------|----------|
| 1v1 | 1 | OneVsOne | 0 = InProgress | Один против одного |
| 2v2 | 2 | TwoVsTwo | 1 = Completed | Два против двух |
| FFA | 4 | FourPlayerFFA | 2 = Cancelled | Четыре игрока (каждый сам за себя) |

## 🚀 Как использовать

### 1. Запуск системы
```bash
# Обычный запуск
start_server_with_bots.bat

# Перезапуск с очисткой (рекомендуется после обновлений)
restart_server_with_cleanup.bat
```

### 2. Unity Client
```csharp
// Игрок выбирает тип матча через UI кнопки:
button_1v1.onClick -> SelectMatchType(1)
button_2v2.onClick -> SelectMatchType(2) 
button_4ffa.onClick -> SelectMatchType(4)

// Затем нажимает Play Now:
Lobby.Instance.AddPlayerToQueue(selectedMatchType)

// Unity проверяет только активные матчи:
bool hasActiveMatch = gameMatch.matchId > 0 && gameMatch.status == 0;
```

### 3. Bots
```python
# Боты случайно выбирают тип матча:
match_type = random.choice(settings["match_types"])
self.join_queue(bot_id, bot_data, match_type)
```

### 4. Server API
```json
POST /api-game-queue/{userId}/join
{
    "MatchType": 1  // 1, 2, or 4
}

GET /api-game-match/user/{userId}
// Возвращает только активные матчи (status == 0)

GET /api-game-match/user/{userId}/history
// Возвращает всю историю матчей
```

## 📊 Тестирование

### Автоматическое тестирование
```bash
test_match_types.bat  # Проверка статистики системы
```

### Ручное тестирование
- **Админка**: https://renderfin.com/online-game/admin.html
- **Игра**: https://renderfin.com/online-game/game.html
- **API**: https://renderfin.com/api-game-queue/stats

## 🔧 Техническая информация

### Очистка при запуске сервера
```csharp
// main.cs - CleanupInactivePlayersOnStartup()
// 1. Отменяем все активные матчи
var activeMatches = await context.GameMatches
    .Where(m => m.Status == GameMatchStatus.InProgress)
    .ToListAsync();

// 2. Очищаем ссылки на матчи у игроков
var playersInMatches = await context.Users
    .Where(u => u.CurrentMatchId != null)
    .ToListAsync();

// 3. Очищаем очередь
var allQueueEntries = await context.MatchQueues.ToListAsync();
```

### Unity проверка статуса
```csharp
// Lobby.cs - CheckPlayerMatchStatus()
var gameMatch = JsonUtility.FromJson<GameMatchResponse>(cleanData);

// КРИТИЧНО: проверяем статус матча
bool hasActiveMatch = gameMatch.matchId > 0 && gameMatch.status == 0;

if (hasActiveMatch) {
    // Переход в активный матч
    _playerStatus = PlayerStatus.InGame;
    SceneManager.LoadScene("GameScene");
} else {
    // Матч завершён - возвращаемся в лобби
    _playerStatus = PlayerStatus.Idle;
}
```

### Server эндпоинты
```csharp
// GameMatchController.cs
[HttpGet("user/{userId}")]
public async Task<ActionResult> GetUserMatches(int userId)
{
    // Только активные матчи
    var activeMatches = await _context.GameMatches
        .Where(m => m.Status == GameMatchStatus.InProgress)
        .ToListAsync();
    
    return Ok(activeMatches.Where(m => m.PlayersList.Contains(userId)));
}
```

## 🎮 Игровой процесс

1. **Сервер запускается** → Очищает все старые матчи и ссылки
2. **Игрок** выбирает тип матча в Unity UI
3. **Бот** случайно выбирает тип матча
4. **Сервер** получает конкретный тип матча
5. **Матчмейкинг** обрабатывает поиск строго по типам
6. **Матч создается** → Статус = InProgress (0)
7. **Unity проверяет** → Только активные матчи (status == 0)
8. **Переход в матч** → Только при активном статусе

## 📈 Статистика

### Очередь
- `oneVsOne` - количество игроков в очереди 1v1
- `twoVsTwo` - количество игроков в очереди 2v2
- `fourPlayerFFA` - количество игроков в очереди 4-player FFA

### Матчи
- `OneVsOneMatches` - количество матчей 1v1
- `TwoVsTwoMatches` - количество матчей 2v2
- `FourPlayerFFAMatches` - количество матчей 4-player FFA

## ✅ Результат

✅ **Игрок**: Может выбирать конкретный тип матча через UI  
✅ **Боты**: Случайно выбирают тип матча для разнообразия  
✅ **Сервер**: Обрабатывает поиск строго по типам, не случайно  
✅ **Матчмейкинг**: Работает только с конкретными типами матчей  
✅ **Очистка**: Матчи очищаются при рестарте сервера  
✅ **Статусы**: Unity проверяет только активные матчи  
✅ **Переходы**: Корректный переход в матч  

**Система работает правильно! Все проблемы решены!** 🎯 