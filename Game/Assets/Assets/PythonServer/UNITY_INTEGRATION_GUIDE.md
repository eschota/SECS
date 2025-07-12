# Unity Integration Guide

## 🎮 Интеграция Unity с игровым сервером

### Основные компоненты

1. **main.cs** - Главный менеджер состояний игры
2. **Lobby.cs** - Система аутентификации и очередей
3. **ui_lobby.cs** - UI интерфейс лобби

### Поток аутентификации

```
Server Connection → Player Registration → Lobby → Queue → Match
```

### Автоматическое обнаружение сервера

Система автоматически пытается подключиться к серверам в следующем порядке:

1. `https://renderfin.com` (Production)
2. `https://localhost:7000` (Local Development)
3. `http://localhost:5000` (Local Development)

### Heartbeat система

- Автоматическая отправка heartbeat каждые 30 секунд
- Поддерживает соединение с сервером
- Предотвращает таймауты

### Валидация статуса игрока

```csharp
// Проверка возможности присоединения к очереди
if (Lobby.Instance.CanJoinQueue()) {
    Lobby.Instance.AddPlayerToQueue();
}

// Проверка возможности покинуть очередь
if (Lobby.Instance.CanLeaveQueue()) {
    Lobby.Instance.RemovePlayerFromQueue();
}
```

### Состояния игрока

- `Unregistered` - Не зарегистрирован
- `Registering` - Регистрируется
- `Authenticating` - Аутентификация
- `Idle` - Готов к игре
- `Searching` - Поиск матча
- `InGame` - В игре
- `Error` - Ошибка

### Автоматическая регистрация

**В Editor:**
- Создается аккаунт разработчика с именем `Developer account {GUID}`
- Данные сохраняются в PlayerPrefs

**В WebGL:**
- Создается гостевой аккаунт с именем `Guest_{GUID}`
- Данные сохраняются в cookies (требует JavaScript плагин)

### API Endpoints

```
POST /api-game-player/                  # Регистрация
POST /api-game-player/heartbeat         # Heartbeat
POST /api-game-queue/{userId}/join      # Войти в очередь
POST /api-game-queue/{userId}/leave     # Покинуть очередь
GET  /api-game-queue/{userId}/status    # Статус очереди
GET  /api-game-statistics/              # Статистика сервера
```

### Тестирование

```bash
# Запуск тестов API
cd Assets/PythonServer
python test_unity_api.py
```

### Отладка

- Все логи начинаются с `[Lobby]`, `[ui_lobby]`, `[main]`
- Используется Debug.Log для информационных сообщений
- Debug.LogError для ошибок
- Debug.LogWarning для предупреждений

### Лучшие практики

1. **Всегда проверяйте статус** перед выполнением действий
2. **Используйте валидацию** для предотвращения некорректных состояний
3. **Обрабатывайте ошибки** gracefully
4. **Мониторьте heartbeat** для стабильного соединения

### Пример использования

```csharp
// Получить текущий статус
var status = Lobby.Instance.GetPlayerStatus();

// Проверить возможность действий
if (Lobby.Instance.CanJoinQueue()) {
    // Игрок может присоединиться к очереди
}

// Получить информацию о пользователе
var user = Lobby.Instance.currentUser;
Debug.Log($"Player: {user.nick_name} (ID: {user.user_id})");
```

### Конфигурация

Для изменения серверов отредактируйте `main.cs`:

```csharp
public static string[] PossibleServers = {
    "https://your-production-server.com",
    "https://localhost:7000",
    "http://localhost:5000"
};
```

### Troubleshooting

**Проблема:** Ошибка сертификата SSL
**Решение:** Используется BypassCertificate для development builds

**Проблема:** Heartbeat не работает
**Решение:** Проверьте user_id и timestamp формат

**Проблема:** Не может присоединиться к очереди
**Решение:** Проверьте статус игрока и валидацию

**Проблема:** UI не обновляется
**Решение:** Используйте RefreshUI() для принудительного обновления

---

## 🎯 Production Ready

Система готова для продакшена с следующими возможностями:

- ✅ Автоматическое обнаружение сервера
- ✅ Heartbeat система
- ✅ Валидация статуса игрока
- ✅ Graceful error handling
- ✅ Автоматическая регистрация
- ✅ UI обратная связь
- ✅ Поддержка WebGL и Editor

**Дата:** 2025-01-10  
**Версия:** 1.0 