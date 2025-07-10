# Игровой веб-сервер на .NET

Этот проект представляет собой веб-сервер для игры, построенный на ASP.NET Core 8.0 с поддержкой WebSocket через SignalR.

## Возможности

- **REST API** для управления играми и игроками
- **SignalR Hub** для реального времени (WebSocket)
- **Swagger UI** для тестирования API
- **CORS** настроен для клиентских приложений
- **Управление игроками**: регистрация, статистика, профили
- **Очередь игроков**: матчмейкинг система
- **Лобби**: создание и управление игровыми комнатами
- **Матчи**: управление активными играми

## Запуск сервера

### Требования
- .NET 8.0 SDK
- Visual Studio 2022 или VS Code

### Команды для запуска

```bash
# Восстановление зависимостей
dotnet restore

# Запуск в режиме разработки
dotnet run

# Сервер запускается на порту 3329
```

## API Endpoints

### Игроки (`/api-game-player`)
- `GET /api-game-player` - Получить всех игроков
- `GET /api-game-player/{id}` - Получить игрока по ID
- `POST /api-game-player` - Создать нового игрока
- `PUT /api-game-player/{id}` - Обновить игрока
- `GET /api-game-player/{id}/stats` - Получить статистику игрока
- `POST /api-game-player/{id}/stats` - Обновить статистику игрока
- `DELETE /api-game-player/{id}` - Удалить игрока

### Очередь (`/api-game-queue`)
- `GET /api-game-queue` - Получить состояние очереди
- `GET /api-game-queue/{playerId}` - Получить статус игрока в очереди
- `POST /api-game-queue/join` - Присоединиться к очереди
- `POST /api-game-queue/leave` - Покинуть очередь
- `POST /api-game-queue/cancel` - Отменить поиск игры
- `GET /api-game-queue/status` - Получить общую статистику очереди

### Лобби (`/api-game-lobby`)
- `GET /api-game-lobby` - Получить список публичных лобби
- `GET /api-game-lobby/{lobbyId}` - Получить лобби по ID
- `POST /api-game-lobby` - Создать новое лобби
- `POST /api-game-lobby/{lobbyId}/join` - Присоединиться к лобби
- `POST /api-game-lobby/{lobbyId}/leave` - Покинуть лобби
- `POST /api-game-lobby/{lobbyId}/ready` - Отметить готовность
- `POST /api-game-lobby/{lobbyId}/start` - Начать игру (только создатель)
- `POST /api-game-lobby/{lobbyId}/settings` - Изменить настройки лобби
- `POST /api-game-lobby/{lobbyId}/kick` - Исключить игрока
- `GET /api-game-lobby/{lobbyId}/status` - Получить статус лобби
- `DELETE /api-game-lobby/{lobbyId}` - Удалить лобби

### Матчи (`/api-game-match`)
- `GET /api-game-match` - Получить список матчей
- `GET /api-game-match/{matchId}` - Получить матч по ID
- `POST /api-game-match` - Создать новый матч
- `POST /api-game-match/{matchId}/start` - Начать матч
- `POST /api-game-match/{matchId}/finish` - Завершить матч
- `POST /api-game-match/{matchId}/cancel` - Отменить матч
- `POST /api-game-match/{matchId}/player/{playerId}/ready` - Готовность игрока
- `POST /api-game-match/{matchId}/update-score` - Обновить счет
- `GET /api-game-match/{matchId}/status` - Получить статус матча
- `DELETE /api-game-match/{matchId}` - Удалить матч

## SignalR Hub

### Подключение к игровому хабу
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")
    .build();

connection.start();
```

### Методы хаба

#### Общие игровые действия
- `JoinGame(gameId)` - Присоединиться к игре
- `LeaveGame(gameId)` - Покинуть игру  
- `SendGameAction(gameId, action, data)` - Отправить игровое действие

#### Лобби
- `JoinLobby(lobbyId)` - Присоединиться к лобби
- `LeaveLobby(lobbyId)` - Покинуть лобби
- `SendLobbyMessage(lobbyId, message)` - Отправить сообщение в лобби

#### Матчи
- `JoinMatch(matchId)` - Присоединиться к матчу
- `LeaveMatch(matchId)` - Покинуть матч
- `SendMatchAction(matchId, action, data)` - Отправить действие в матче

### События хаба

#### Общие события
- `PlayerJoined` - Игрок присоединился
- `PlayerLeft` - Игрок покинул игру
- `GameAction` - Игровое действие

#### Лобби события
- `PlayerJoinedLobby` - Игрок присоединился к лобби
- `PlayerLeftLobby` - Игрок покинул лобби
- `LobbyMessage` - Сообщение в лобби

#### Матч события
- `PlayerJoinedMatch` - Игрок присоединился к матчу
- `PlayerLeftMatch` - Игрок покинул матч
- `MatchAction` - Действие в матче

## Nginx конфигурация

Сервер настроен для работы с Nginx на порту 3329:

```nginx
location ~ ^/api-game- {
    proxy_pass http://127.0.0.1:3329;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host $server_name;
    proxy_set_header X-Forwarded-Port $server_port;
}

location /online-game/ {
    proxy_pass http://127.0.0.1:3329/online-game/;
    # ... остальные настройки
}
```

## Примеры использования

### Создание игрока
```bash
curl -X POST "http://localhost:3329/api-game-player" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "player1",
    "email": "player1@example.com"
  }'
```

### Присоединение к очереди
```bash
curl -X POST "http://localhost:3329/api-game-queue/join" \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "player-id",
    "playerName": "Player1"
  }'
```

### Создание лобби
```bash
curl -X POST "http://localhost:3329/api-game-lobby" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Моё лобби",
    "creatorId": "player-id",
    "creatorName": "Player1",
    "maxPlayers": 4,
    "isPublic": true
  }'
```

### Создание матча
```bash
curl -X POST "http://localhost:3329/api-game-match" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Матч 1",
    "players": [
      {"playerId": "player1", "playerName": "Player1"},
      {"playerId": "player2", "playerName": "Player2"}
    ]
  }'
```

## Swagger UI

После запуска сервера перейдите по адресу:
- `http://localhost:3329/swagger` (в режиме разработки)

## Структура проекта

```
Server/
├── Controllers/
│   ├── PlayerController.cs   # API для управления игроками
│   ├── QueueController.cs    # API для очереди матчмейкинга
│   ├── LobbyController.cs    # API для управления лобби
│   └── MatchController.cs    # API для управления матчами
├── Models/
│   └── GameModels.cs         # Модели данных игры
├── main.cs                   # Точка входа приложения
├── Server.csproj            # Файл проекта
├── appsettings.json         # Конфигурация сервера
└── README.md                # Документация
```

## Технологии

- ASP.NET Core 8.0
- SignalR для WebSocket
- Swagger/OpenAPI для документации
- JSON для API responses
- CORS для кросс-доменных запросов
- Thread-safe коллекции для многопользовательских данных

## Логика работы

1. **Игроки** регистрируются через `PlayerController`
2. **Очередь** (`QueueController`) выполняет матчмейкинг
3. **Лобби** (`LobbyController`) позволяет создавать приватные/публичные комнаты
4. **Матчи** (`MatchController`) управляют активными играми
5. **SignalR** обеспечивает реальное время общения

Все контроллеры используют thread-safe операции для поддержки множественных подключений. 