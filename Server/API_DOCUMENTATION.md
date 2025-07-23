# SECS Game Server API Documentation

## Архитектурные изменения (2024-07)

- **Матчмейкинг полностью in-memory**: очереди и активные матчи хранятся только в памяти сервера, не в базе данных.
- **В базе данных**: только пользователи и завершённые (исторические) матчи.
- **После рестарта сервера**: очереди и активные матчи сбрасываются, база не содержит "зависших" матчей.
- **Все API для очередей и активных матчей работают с in-memory сервисом**.
- **История матчей**: только завершённые матчи попадают в базу и доступны через соответствующие эндпоинты.

---

## Общая информация

**Базовый URL**: `https://renderfin.com`
**Версия API**: 1.0
**Формат данных**: JSON

## Запуск и работа с сервером

### 1. Запуск сервера

```bash
# Запуск через батник
.\start_server.bat

# Или через dotnet
dotnet run

# Сборка проекта
dotnet build
```

### 2. Тестирование API

```bash
# Запуск тестов
python test_new_user_mmr.py

# Проверка статистики
python Bots/test_api.py
```

### 3. Управление ботами

```bash
# Запуск системы ботов
cd Bots
.\start_bots.bat

# Мониторинг статистики ботов
.\show_stats.bat

# Активность ботов встроена в основной менеджер
# Отдельный heartbeat больше не нужен
```

---

## 🎮 PLAYER API (`/api-game-player`)

### Регистрация и авторизация

#### `POST /api-game-player` - Регистрация нового игрока
```json
{
  "username": "TestPlayer",
  "email": "test@example.com",
  "password": "password123",
  "avatar": "https://example.com/avatar.png" // опционально
}
```

**Ответ (201)**:
```json
{
  "id": 1,
  "username": "TestPlayer",
  "email": "test@example.com",
  "avatar": "https://example.com/avatar.png",
  "createdAt": "2023-10-01T12:00:00Z",
  "gamesPlayed": 0,
  "gamesWon": 0,
  "score": 0,
  "level": 1,
  "mmrOneVsOne": 500,
  "mmrTwoVsTwo": 500,
  "mmrFourPlayerFFA": 500
}
```

#### `POST /api-game-player/login` - Авторизация
```json
{
  "email": "test@example.com",
  "password": "password123"
}
```

**Ответ (200)**: Возвращает данные пользователя (аналогично регистрации)

### Управление игроками

#### `GET /api-game-player` - Список всех игроков
**Ответ (200)**: Массив игроков

#### `GET /api-game-player/{id}` - Получить игрока по ID
**Ответ (200)**: Данные игрока

#### `PUT /api-game-player/{id}` - Обновить данные игрока
```json
{
  "username": "NewNickname", // опционально
  "avatar": "https://new-avatar.com/image.png", // опционально
  "password": "newpassword123" // опционально
}
```

#### `DELETE /api-game-player/{id}` - Удалить игрока (мягкое удаление)
**Ответ (204)**: Нет содержимого

### Heartbeat система

Система heartbeat теперь работает **автоматически** - каждый раз при запросе статуса игрока через `GET /api-game-player/{id}` сервер автоматически обновляет время последней активности игрока (`LastHeartbeat`).

**Преимущества новой системы:**
- Устранена избыточность - нет необходимости в отдельном эндпоинте
- Автоматическое обновление активности при любом запросе статуса
- Упрощение логики клиентского кода

---

## 🎯 QUEUE API (`/api-game-queue`)

> **Внимание!** Очереди теперь полностью in-memory. После рестарта сервера все очереди сбрасываются. В базе данных очереди не хранятся.

### Управление очередью

#### `POST /api-game-queue/{userId}/join` - Войти в очередь
```json
{
  "matchType": 1 // 1 = 1v1, 2 = 2v2, 4 = FFA
}
```

**Ответ (200)**:
```json
{
  "message": "Successfully joined queue",
  "queueType": 1
}
```

#### `POST /api-game-queue/{userId}/leave` - Выйти из очереди
**Ответ (200)**:
```json
{
  "message": "Successfully left queue"
}
```

#### `GET /api-game-queue/{userId}/status` - Статус игрока в очереди
**Ответ (200)**:
```json
{
  "inQueue": true,
  "queueType": 1,
  "queueTime": 45, // секунды
  "currentMmrThreshold": 520,
  "userMmr": 500
}
```

### Статистика очереди

#### `GET /api-game-queue/stats` - Статистика всех очередей
**Ответ (200)**:
```json
{
  "oneVsOne": 5,
  "twoVsTwo": 3,
  "fourPlayerFFA": 2,
  "total": 10
}
```

---

## 🏆 MATCH API (`/api-game-match`)

> **Внимание!** Активные матчи теперь полностью in-memory. В базе данных хранятся только завершённые матчи (история). После рестарта сервера все активные матчи сбрасываются.

### Создание матчей

#### `POST /api-game-match/create` - Создать матч
```json
{
  "matchType": 1, // 1 = 1v1, 2 = 2v2, 4 = FFA
  "playerIds": [1, 2],
  "teamIds": [1, 2] // для командных матчей
}
```

**Ответ (200)**: Данные созданного матча

### Управление матчами

#### `POST /api-game-match/{matchId}/finish` - Завершить матч
```json
{
  "winners": [1], // ID игроков-победителей
  "losers": [2]   // ID игроков-проигравших
}
```

#### `POST /api-game-match/{matchId}/cancel` - Отменить матч
```json
{
  "reason": "Connection issues"
}
```

#### `GET /api-game-match/{matchId}` - Получить информацию о матче
**Ответ (200)**: Данные матча

#### `GET /api-game-match/{matchId}/status` - Статус матча
**Ответ (200)**:
```json
{
  "matchId": 1,
  "status": "InProgress",
  "matchType": 1,
  "elapsedTime": 120, // секунды
  "timeLimit": 300,
  "isExpired": false,
  "players": [1, 2],
  "winners": [],
  "losers": [],
  "draw": []
}
```

#### `GET /api-game-match/active` - Активные матчи
**Ответ (200)**: Массив активных матчей

#### `GET /api-game-match/user/{userId}` - Матчи пользователя
**Ответ (200)**: Массив матчей пользователя (последние 20)

#### `POST /api-game-match/check-timeouts` - Проверить таймауты
**Ответ (200)**: Информация о проверке таймаутов

---

## 🏛️ LOBBY API (`/api-game-lobby`)

### Создание лобби

#### `POST /api-game-lobby` - Создать лобби
```json
{
  "name": "My Game Room",
  "creatorId": "user123",
  "creatorName": "PlayerName",
  "maxPlayers": 4,
  "isPublic": true,
  "password": "secret123" // для приватных лобби
}
```

### Управление лобби

#### `GET /api-game-lobby` - Список публичных лобби
**Ответ (200)**: Массив лобби

#### `GET /api-game-lobby/{lobbyId}` - Получить лобби
**Ответ (200)**: Данные лобби

#### `POST /api-game-lobby/{lobbyId}/join` - Присоединиться к лобби
```json
{
  "playerId": "user456",
  "playerName": "PlayerName",
  "password": "secret123" // для приватных лобби
}
```

#### `POST /api-game-lobby/{lobbyId}/leave` - Покинуть лобби
```json
{
  "playerId": "user456"
}
```

#### `POST /api-game-lobby/{lobbyId}/ready` - Готовность игрока
```json
{
  "playerId": "user456",
  "isReady": true
}
```

#### `POST /api-game-lobby/{lobbyId}/start` - Запустить игру
```json
{
  "creatorId": "user123",
  "requireAllReady": false // опционально
}
```

#### `POST /api-game-lobby/{lobbyId}/settings` - Обновить настройки лобби
```json
{
  "creatorId": "user123",
  "name": "New Name", // опционально
  "maxPlayers": 6,    // опционально
  "isPublic": false,  // опционально
  "password": "newsecret" // опционально
}
```

#### `POST /api-game-lobby/{lobbyId}/kick` - Исключить игрока
```json
{
  "creatorId": "user123",
  "playerId": "user456"
}
```

#### `GET /api-game-lobby/{lobbyId}/status` - Статус лобби
**Ответ (200)**:
```json
{
  "lobbyId": "lobby123",
  "name": "My Game Room",
  "status": "Waiting",
  "playerCount": 2,
  "maxPlayers": 4,
  "isPublic": true,
  "createdAt": "2023-10-01T12:00:00Z",
  "players": [
    {
      "playerId": "user123",
      "playerName": "PlayerName",
      "score": 0,
      "isReady": true
    }
  ]
}
```

#### `DELETE /api-game-lobby/{lobbyId}` - Удалить лобби
```json
{
  "creatorId": "user123"
}
```

---

## 📊 STATISTICS API (`/api-game-statistics`)

#### `GET /api-game-statistics` - Общая статистика игры
**Ответ (200)**:
```json
{
  "totalPlayers": 157,
  "onlinePlayers": 12,
  "totalMatches": 245,
  "liveMatches": 3
}
```

---

## 🔧 ADMIN API (`/api-game-admin`)

### Авторизация администратора

#### `POST /api-game-admin/auth` - Авторизация админа
```json
{
  "token": "admin123secure"
}
```

**Ответ (200)**:
```json
{
  "success": true,
  "sessionToken": "generated-session-token"
}
```

#### `POST /api-game-admin/validate` - Проверить сессию
```json
{
  "sessionToken": "generated-session-token"
}
```

### Статистика

#### `GET /api-game-admin/statistics` - Детальная статистика
**Ответ (200)**:
```json
{
  "totalPlayers": 157,
  "onlinePlayers": 12,
  "totalMatches": 245,
  "activeMatches": 3,
  "oneVsOneMatches": 120,
  "twoVsTwoMatches": 85,
  "fourPlayerFFAMatches": 40,
  "todayNewPlayers": 5,
  "todayMatches": 15,
  "weekNewPlayers": 25,
  "weekMatches": 98
}
```

#### `GET /api-game-admin/realtime-stats` - Статистика в реальном времени
**Ответ (200)**:
```json
{
  "queueStats": {
    "oneVsOne": 5,
    "twoVsTwo": 3,
    "fourPlayerFFA": 2,
    "total": 10
  },
  "activeMatches": {
    "oneVsOne": 2,
    "twoVsTwo": 1,
    "fourPlayerFFA": 0,
    "total": 3
  },
  "serverInfo": {
    "onlineNow": 12,
    "lastUpdate": "2023-10-01T12:00:00Z"
  }
}
```

#### `GET /api-game-admin/recent-matches` - Последние матчи
**Ответ (200)**: Массив последних 10 матчей

#### `GET /api-game-admin/top-players` - Топ игроков
**Ответ (200)**:
```json
{
  "topByScore": [
    {
      "id": 1,
      "username": "ProPlayer",
      "score": 1250,
      "gamesPlayed": 50,
      "gamesWon": 35,
      "winRate": 70.0
    }
  ],
  "topByOneVsOne": [
    {
      "id": 1,
      "username": "ProPlayer",
      "mmr": 750,
      "gamesPlayed": 30,
      "gamesWon": 22
    }
  ]
}
```

### Управление данными

#### `POST /api-game-admin/clear-players` - Очистить игроков
```json
{
  "sessionToken": "generated-session-token",
  "onlyInactive": false // true = только неактивные
}
```

#### `POST /api-game-admin/clear-matches` - Очистить матчи
```json
{
  "sessionToken": "generated-session-token",
  "onlyCompleted": true,  // true = только завершенные
  "olderThanDays": 30     // старше N дней
}
```

#### `POST /api-game-admin/set-all-players-mmr` - Установить MMR всем игрокам
```json
{
  "sessionToken": "generated-session-token",
  "mmrValue": 500
}
```

---

## 🎮 ONLINE GAME API (`/api-game-online`)

#### `GET /api-game-online` - Веб-интерфейс игры
**Ответ**: HTML страница игры

#### `GET /api-game-online/styles` - CSS стили
**Ответ**: CSS файл

#### `GET /api-game-online/scripts` - JavaScript скрипты
**Ответ**: JavaScript файл

---

## 🎯 LEGACY MATCH API (`/api-game-match`)

### Управление матчами (Legacy)

#### `GET /api-game-match` - Все матчи
**Ответ (200)**: Массив всех матчей

#### `POST /api-game-match` - Создать матч
```json
{
  "name": "Epic Battle",
  "players": [
    {
      "playerId": "user123",
      "playerName": "PlayerName"
    }
  ]
}
```

#### `POST /api-game-match/{matchId}/start` - Запустить матч
**Ответ (200)**: Данные матча

#### `POST /api-game-match/{matchId}/finish` - Завершить матч
```json
{
  "winnerId": "user123",
  "playerScores": [
    {
      "playerId": "user123",
      "score": 100
    }
  ]
}
```

#### `POST /api-game-match/{matchId}/cancel` - Отменить матч
```json
{
  "reason": "Player disconnected"
}
```

#### `POST /api-game-match/{matchId}/player/{playerId}/ready` - Готовность игрока
```json
{
  "isReady": true
}
```

#### `POST /api-game-match/{matchId}/update-score` - Обновить счет
```json
{
  "playerId": "user123",
  "score": 150
}
```

#### `GET /api-game-match/{matchId}/status` - Статус матча
**Ответ (200)**:
```json
{
  "matchId": "match123",
  "status": "InProgress",
  "elapsedTime": 120,
  "players": [
    {
      "playerId": "user123",
      "playerName": "PlayerName",
      "score": 100,
      "isReady": true
    }
  ]
}
```

#### `DELETE /api-game-match/{matchId}` - Удалить матч
**Ответ (204)**: Нет содержимого

---

## 🔧 Типы данных

### MatchType (GameMatchType)
- `1` - OneVsOne (1v1)
- `2` - TwoVsTwo (2v2)
- `4` - FourPlayerFFA (FFA)

### MatchStatus (GameMatchStatus)
- `InProgress` - В процессе
- `Completed` - Завершен
- `Cancelled` - Отменен

### LobbyStatus
- `Waiting` - Ожидание игроков
- `Starting` - Запускается
- `InProgress` - В процессе

### Legacy MatchStatus
- `Starting` - Запускается
- `InProgress` - В процессе
- `Finished` - Завершен
- `Cancelled` - Отменен

---

## 🛡️ Система MMR

### Начальные значения
- Все новые игроки получают **500 MMR** для каждого типа матча
- Минимальный MMR: **500** (не может опуститься ниже)

### Изменение MMR
- **Победа**: +20 MMR
- **Поражение**: -20 MMR
- **Ничья**: MMR не изменяется

### Matchmaking
- Система подбирает игроков с близким MMR
- Пороговое значение увеличивается со временем ожидания в очереди

---

## 🔑 Heartbeat система

### Требования
- Игроки должны периодически проверять свой статус (рекомендуется каждые 30 секунд)
- Игроки считаются офлайн через 2 минуты без активности
- Неактивные игроки автоматически удаляются из очередей

### Автоматическая очистка
- Сервер проверяет и удаляет неактивных игроков каждые 10 секунд
- При старте сервера очищаются все очереди

---

## 🤖 Система ботов

### Конфигурация
- Файл конфигурации: `Bots/bots_data/bots_list.json`
- Автоматическая регистрация и управление ботами
- Разные типы поведения: Aggressive, Random, Casual, Passive

### Управление
```bash
# Запуск ботов
cd Bots && .\start_bots.bat

# Статистика ботов
.\show_stats.bat

# Активность ботов (автоматически при запуске)
.\start_bots.bat
```

---

## 📋 Коды ошибок

### HTTP статусы
- `200` - OK
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error
- `502` - Bad Gateway

### Типичные ошибки
- "User not found" - Пользователь не найден
- "User is already in queue" - Пользователь уже в очереди
- "User is already in a match" - Пользователь уже в матче
- "Match not found" - Матч не найден
- "Invalid password" - Неверный пароль
- "Lobby is full" - Лобби переполнено

---

## 🎯 Примеры использования

### Полный цикл игры

1. **Регистрация**:
   ```
   POST /api-game-player
   ```

2. **Авторизация**:
   ```
   POST /api-game-player/login
   ```

3. **Вход в очередь**:
   ```
   POST /api-game-queue/{userId}/join
   ```

4. **Поддержание активности** (периодически):
   ```
   GET /api-game-player/{userId}
   ```

5. **Матч найден** (автоматически):
   ```
   POST /api-game-match/create
   ```

6. **Завершение матча**:
   ```
   POST /api-game-match/{matchId}/finish
   ```

### Создание лобби

1. **Создание**:
   ```
   POST /api-game-lobby
   ```

2. **Ожидание игроков**:
   ```
   POST /api-game-lobby/{lobbyId}/join
   ```

3. **Готовность**:
   ```
   POST /api-game-lobby/{lobbyId}/ready
   ```

4. **Запуск**:
   ```
   POST /api-game-lobby/{lobbyId}/start
   ```

---

## 📁 Файловая структура проекта

```
Server/
├── Controllers/         # API контроллеры
│   ├── PlayerController.cs
│   ├── AdminController.cs
│   ├── QueueController.cs
│   ├── MatchController.cs
│   ├── LobbyController.cs
│   ├── GameMatchController.cs
│   ├── GameStatisticsController.cs
│   └── OnlineGameController.cs
├── Models/             # Модели данных
│   ├── User.cs
│   ├── GameMatch.cs
│   ├── MatchQueue.cs
│   ├── GameModels.cs
│   └── RequestModels.cs
├── Data/               # База данных
│   ├── GameDbContext.cs
│   └── Migrations/
├── Services/           # Сервисы
│   ├── AuthService.cs
│   └── MatchmakingService.cs
├── Bots/              # Система ботов
│   ├── Bots_manager.py
│   ├── bot_stats.py
│   └── start_bots.bat
├── wwwroot/           # Веб-интерфейс
│   └── online-game/
├── gameserver.db      # База данных SQLite
└── start_server.bat   # Запуск сервера
```

---

## 🚀 Развертывание

### Локальная разработка
```bash
git clone <repository>
cd Server
dotnet build
dotnet run
```

### Продакшн
- Сервер развернут на `https://renderfin.com`
- Использует SQLite базу данных
- Автоматическое управление ботами
- Система мониторинга и логирования

---

## 📞 Поддержка

Для получения помощи:
1. Проверьте логи сервера
2. Используйте тестовые скрипты
3. Мониторьте статистику через админ-панель
4. Проверьте heartbeat систему

**Дата обновления**: 2025-01-10
**Версия документации**: 1.0 