# Документация базы данных игры

## Обзор

База данных игры использует SQLite для хранения всех данных о пользователях, лобби, очереди, играх и статистике. Модуль `database.py` предоставляет полный API для работы с базой данных.

## Структура базы данных

### Таблица `users`
Основная таблица пользователей системы.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| user_id | TEXT UNIQUE | Уникальный идентификатор пользователя |
| username | TEXT | Имя пользователя |
| email | TEXT | Email пользователя |
| status | TEXT | Статус пользователя (active/inactive) |
| created_at | TIMESTAMP | Дата создания |
| last_login | TIMESTAMP | Последний вход |
| profile_data | TEXT | JSON данные профиля |

### Таблица `lobby_users`
Пользователи, находящиеся в лобби.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| user_id | TEXT UNIQUE | Ссылка на пользователя |
| username | TEXT | Имя пользователя |
| status | TEXT | Статус в лобби |
| created_at | TIMESTAMP | Время входа в лобби |
| last_seen | TIMESTAMP | Последняя активность |

### Таблица `queue_users`
Пользователи в очереди на игру.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| user_id | TEXT UNIQUE | Ссылка на пользователя |
| username | TEXT | Имя пользователя |
| joined_at | TIMESTAMP | Время входа в очередь |
| priority | INTEGER | Приоритет в очереди |
| status | TEXT | Статус (waiting/matched) |

### Таблица `games`
Информация об играх.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| game_id | TEXT UNIQUE | Уникальный ID игры |
| name | TEXT | Название игры |
| status | TEXT | Статус игры (waiting/active/ended) |
| max_players | INTEGER | Максимум игроков |
| current_players | INTEGER | Текущее количество игроков |
| created_at | TIMESTAMP | Время создания |
| started_at | TIMESTAMP | Время начала |
| ended_at | TIMESTAMP | Время окончания |
| players | TEXT | JSON массив игроков |

### Таблица `game_sessions`
Сессии игроков в играх.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| game_id | TEXT | Ссылка на игру |
| user_id | TEXT | Ссылка на пользователя |
| joined_at | TIMESTAMP | Время присоединения |
| left_at | TIMESTAMP | Время выхода |
| status | TEXT | Статус сессии |

### Таблица `game_stats`
Статистика игроков.

| Поле | Тип | Описание |
|------|-----|----------|
| id | INTEGER PRIMARY KEY | Автоинкрементный ID |
| user_id | TEXT | Ссылка на пользователя |
| games_played | INTEGER | Количество сыгранных игр |
| games_won | INTEGER | Количество выигранных игр |
| total_score | INTEGER | Общий счет |
| last_updated | TIMESTAMP | Последнее обновление |

## API базы данных

### Класс GameDatabase

#### Инициализация
```python
from database import db

# Использование глобального экземпляра
user = db.get_user("user123")

# Или создание нового экземпляра
custom_db = GameDatabase("custom_database.db")
```

#### Методы для работы с пользователями

**create_user(user_data)**
- Создает нового пользователя
- Параметры: `user_data` - словарь с данными пользователя
- Возвращает: словарь с данными созданного пользователя
- Исключения: `ValueError` если пользователь уже существует

**get_user(user_id)**
- Получает пользователя по ID
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными пользователя или None

**update_user(user_data)**
- Обновляет данные пользователя
- Параметры: `user_data` - словарь с обновляемыми данными
- Возвращает: словарь с обновленными данными или None

**delete_user(user_id)**
- Удаляет пользователя и все связанные данные
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными удаленного пользователя или None

#### Методы для работы с лобби

**create_lobby_user(user_data)**
- Добавляет пользователя в лобби
- Параметры: `user_data` - словарь с данными пользователя
- Возвращает: словарь с данными пользователя в лобби

**get_lobby_user(user_id)**
- Получает пользователя из лобби
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными или None

**get_lobby_users(page, per_page)**
- Получает список пользователей в лобби с пагинацией
- Параметры: `page` - номер страницы, `per_page` - количество на странице
- Возвращает: словарь с пользователями и метаданными пагинации

**update_lobby_user(user_data)**
- Обновляет данные пользователя в лобби
- Параметры: `user_data` - словарь с обновляемыми данными
- Возвращает: словарь с обновленными данными или None

**delete_lobby_user(user_id)**
- Удаляет пользователя из лобби
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными удаленного пользователя или None

#### Методы для работы с очередью

**add_user_to_queue(user_data)**
- Добавляет пользователя в очередь
- Параметры: `user_data` - словарь с данными пользователя
- Возвращает: словарь с данными пользователя в очереди

**get_queue_user(user_id)**
- Получает пользователя из очереди
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными или None

**get_queue_users()**
- Получает список всех пользователей в очереди
- Возвращает: словарь с пользователями и общим количеством

**remove_user_from_queue(user_id)**
- Удаляет пользователя из очереди
- Параметры: `user_id` - ID пользователя
- Возвращает: словарь с данными удаленного пользователя или None

#### Методы для работы с играми

**create_game(game_data)**
- Создает новую игру
- Параметры: `game_data` - словарь с данными игры
- Возвращает: словарь с данными созданной игры

**get_game(game_id)**
- Получает игру по ID
- Параметры: `game_id` - ID игры
- Возвращает: словарь с данными игры или None

**get_games()**
- Получает список всех игр
- Возвращает: словарь с играми и общим количеством

**update_game(game_data)**
- Обновляет данные игры
- Параметры: `game_data` - словарь с обновляемыми данными
- Возвращает: словарь с обновленными данными или None

**delete_game(game_id)**
- Удаляет игру и все связанные данные
- Параметры: `game_id` - ID игры
- Возвращает: словарь с данными удаленной игры или None

#### Методы для статистики

**get_server_stats()**
- Получает общую статистику сервера
- Возвращает: словарь со статистикой

**cleanup_old_data(days)**
- Очищает старые данные
- Параметры: `days` - количество дней для хранения данных

## Примеры использования

### Создание пользователя
```python
user_data = {
    "user_id": "player123",
    "username": "PlayerName",
    "email": "player@example.com",
    "status": "active",
    "profile_data": '{"level": 1, "experience": 0}'
}

try:
    user = db.create_user(user_data)
    print(f"Создан пользователь: {user['username']}")
except ValueError as e:
    print(f"Ошибка: {e}")
```

### Добавление пользователя в лобби
```python
lobby_data = {
    "user_id": "player123",
    "username": "PlayerName",
    "status": "active"
}

try:
    lobby_user = db.create_lobby_user(lobby_data)
    print(f"Пользователь добавлен в лобби: {lobby_user['username']}")
except ValueError as e:
    print(f"Ошибка: {e}")
```

### Получение статистики сервера
```python
stats = db.get_server_stats()
print(f"Пользователей в лобби: {stats['lobby_users_count']}")
print(f"Пользователей в очереди: {stats['queue_users_count']}")
print(f"Активных игр: {stats['active_games_count']}")
```

## Тестирование

Для тестирования базы данных используйте скрипт `test_database.py`:

```bash
python test_database.py
```

Скрипт тестирует все основные функции базы данных и автоматически очищает тестовые данные.

## Миграции

При изменении структуры базы данных:

1. Создайте резервную копию текущей базы данных
2. Обновите схему в методе `init_database()`
3. Добавьте миграционные скрипты при необходимости
4. Протестируйте на тестовых данных

## Производительность

- Используйте индексы для часто запрашиваемых полей
- Регулярно выполняйте `cleanup_old_data()` для очистки старых данных
- Для больших нагрузок рассмотрите переход на PostgreSQL или MySQL

## Безопасность

- Все SQL запросы используют параметризованные запросы для предотвращения SQL-инъекций
- Валидируйте входные данные перед сохранением в базу
- Регулярно создавайте резервные копии базы данных 