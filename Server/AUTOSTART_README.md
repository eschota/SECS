# RenderFin Server - Система Автозапуска

## Описание

Система автозапуска обеспечивает **24/7 работу сервера** с автоматическим запуском при старте Windows и самовосстановлением при сбоях.

### Архитектура системы

```
┌─────────────────────────────────────────────────────────────┐
│                    WINDOWS SCHEDULER                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              MASTER WATCHDOG                        │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │            PRIMARY WATCHDOG                 │    │    │
│  │  │  ┌─────────────────────────────────────┐    │    │    │
│  │  │  │           .NET SERVER               │    │    │    │
│  │  │  │     (dotnet run)                    │    │    │    │
│  │  │  └─────────────────────────────────────┘    │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### Уровни защиты

1. **Windows Scheduler** - Автозапуск при старте системы
2. **Master Watchdog** - Контролирует Primary Watchdog
3. **Primary Watchdog** - Контролирует .NET Server
4. **Health Checks** - Проверка доступности через HTTPS

---

## Быстрый старт

### 1. Установка автозапуска

```cmd
# Запустить как Администратор
install-autostart.bat
```

### 2. Управление системой

```cmd
# Интерактивное управление
control-system.bat
```

### 3. Удаление автозапуска

```cmd
# Запустить как Администратор  
remove-autostart.bat
```

---

## Подробная инструкция

### Установка

#### Требования
- Windows 10/11
- .NET 8.0 или выше
- PowerShell 5.0 или выше
- Права администратора

#### Шаг 1: Подготовка
1. Убедитесь, что все файлы находятся в папке `C:\SECS\Server`
2. Проверьте наличие файла `Server.csproj`
3. Убедитесь, что сервер запускается через `dotnet run`

#### Шаг 2: Установка
1. Откройте PowerShell **как Администратор**
2. Перейдите в папку проекта:
   ```cmd
   cd C:\SECS\Server
   ```
3. Запустите установку:
   ```cmd
   .\install-autostart.bat
   ```

#### Шаг 3: Проверка
Система автоматически проверит установку и запустит тест сервера.

---

## Управление системой

### Файлы управления

| Файл | Описание |
|------|----------|
| `install-autostart.bat` | Установка автозапуска |
| `remove-autostart.bat` | Удаление автозапуска |
| `control-system.bat` | Интерактивное управление |
| `master-watchdog.ps1` | Главный контроллер |
| `server-watchdog-fixed.ps1` | Основной вотчдог |

### Интерактивное управление

Запустите `control-system.bat` для доступа к меню:

```
[1] Start System        - Запустить систему вотчдога
[2] Stop System         - Остановить систему вотчдога
[3] Check Status        - Проверить состояние системы
[4] View Logs           - Просмотреть логи
[5] Restart System      - Перезапустить систему
[6] Enable Autostart    - Включить автозапуск
[7] Disable Autostart   - Отключить автозапуск
[8] Test Server         - Проверить работу сервера
[9] Exit                - Выход
```

### Ручное управление через PowerShell

```powershell
# Запуск системы
Start-ScheduledTask -TaskName "RenderFin-Server-Watchdog"

# Остановка системы
Stop-ScheduledTask -TaskName "RenderFin-Server-Watchdog"

# Проверка состояния
Get-ScheduledTask -TaskName "RenderFin-Server-Watchdog"
Get-ScheduledTaskInfo -TaskName "RenderFin-Server-Watchdog"
```

---

## Мониторинг и логи

### Расположение логов

```
C:\SECS\Code\
├── master-watchdog.log      # Главный контроллер
├── watchdog.log            # Основной вотчдог
├── server-activity.log     # Активность сервера
├── matchmaking-activity.log # Матчмейкинг
└── unity-activity.log      # Unity клиент
```

### Мониторинг в реальном времени

```powershell
# Просмотр логов в реальном времени
Get-Content C:\SECS\Code\master-watchdog.log -Wait -Tail 10

# Проверка работы сервера
Invoke-WebRequest -Uri "https://renderfin.com/api-game-queue/stats"
```

### Ключевые индикаторы

| Лог | Значение |
|-----|----------|
| `[MASTER] ===== MASTER WATCHDOG SERVICE STARTED =====` | Система запущена |
| `[OK] System healthy. Watchdog PID: XXXX` | Все работает нормально |
| `[RESTART] Attempting to restart primary watchdog...` | Перезапуск вотчдога |
| `[ERROR] Primary watchdog is not healthy!` | Проблема с системой |

---

## Настройка и конфигурация

### Параметры Master Watchdog

```powershell
# Запуск с кастомными параметрами
.\master-watchdog.ps1 -CheckInterval 60 -MaxRestarts 20 -RestartCooldown 600
```

| Параметр | Описание | По умолчанию |
|----------|----------|--------------|
| `CheckInterval` | Интервал проверки (сек) | 30 |
| `MaxRestarts` | Максимум перезапусков | 10 |
| `RestartCooldown` | Пауза между перезапусками (сек) | 300 |

### Параметры Primary Watchdog

```powershell
# Запуск с мониторингом файлов
.\server-watchdog-fixed.ps1 -CheckInterval 5 -MonitorFiles
```

| Параметр | Описание | По умолчанию |
|----------|----------|--------------|
| `CheckInterval` | Интервал проверки (сек) | 10 |
| `MonitorFiles` | Мониторинг изменений файлов | false |

---

## Устранение неполадок

### Проблема: Система не запускается

**Решение:**
1. Проверьте права администратора
2. Убедитесь, что .NET установлен
3. Проверьте пути к файлам
4. Посмотрите логи

```powershell
# Проверка задачи
Get-ScheduledTask -TaskName "RenderFin-Server-Watchdog"

# Проверка .NET
dotnet --version

# Проверка файлов
Test-Path "C:\SECS\Server\Server.csproj"
Test-Path "C:\SECS\Server\master-watchdog.ps1"
```

### Проблема: Сервер не отвечает

**Решение:**
1. Проверьте интернет соединение
2. Убедитесь, что порты не заблокированы
3. Проверьте сертификаты SSL
4. Перезапустите систему

```powershell
# Тест соединения
Test-NetConnection -ComputerName renderfin.com -Port 443

# Перезапуск
Stop-ScheduledTask -TaskName "RenderFin-Server-Watchdog"
Start-ScheduledTask -TaskName "RenderFin-Server-Watchdog"
```

### Проблема: Много перезапусков

**Решение:**
1. Проверьте стабильность системы
2. Увеличьте `RestartCooldown`
3. Проверьте логи на ошибки
4. Рассмотрите аппаратные проблемы

```powershell
# Анализ логов
Get-Content C:\SECS\Code\master-watchdog.log | Select-String "ERROR"
Get-Content C:\SECS\Code\watchdog.log | Select-String "ERROR"
```

---

## Безопасность

### Права доступа

- **Задача Windows**: Запускается от имени SYSTEM
- **Файлы**: Доступ только для администраторов
- **Логи**: Защищены от изменения обычными пользователями

### Сетевая безопасность

- **HTTPS**: Все проверки через защищенное соединение
- **Сертификаты**: Валидация SSL сертификатов
- **Таймауты**: Ограничение времени запросов

### Рекомендации

1. Регулярно проверяйте логи
2. Обновляйте .NET и PowerShell
3. Мониторьте использование ресурсов
4. Делайте резервные копии конфигурации

---

## Расширенные возможности

### Кастомная конфигурация

Создайте файл `watchdog-config.json`:

```json
{
  "ServerPath": "C:\\SECS\\Server",
  "CheckInterval": 30,
  "MaxRestarts": 15,
  "RestartCooldown": 300,
  "HealthCheckUrl": "https://renderfin.com/api-game-queue/stats",
  "MonitorFiles": true,
  "LogLevel": "INFO"
}
```

### Интеграция с мониторингом

```powershell
# Экспорт метрик для мониторинга
function Export-WatchdogMetrics {
    $task = Get-ScheduledTask -TaskName "RenderFin-Server-Watchdog"
    $taskInfo = Get-ScheduledTaskInfo -TaskName "RenderFin-Server-Watchdog"
    
    @{
        TaskState = $task.State
        LastRunTime = $taskInfo.LastRunTime
        LastResult = $taskInfo.LastTaskResult
        NextRunTime = $taskInfo.NextRunTime
    } | ConvertTo-Json
}
```

---

## FAQ

**Q: Можно ли изменить URL для проверки здоровья?**
A: Да, отредактируйте переменную в скриптах или создайте конфигурационный файл.

**Q: Как увеличить интервал проверки?**
A: Используйте параметр `-CheckInterval` при запуске или измените значение по умолчанию.

**Q: Что делать, если система потребляет много ресурсов?**
A: Увеличьте интервалы проверки и отключите мониторинг файлов если он не нужен.

**Q: Можно ли запустить несколько серверов?**
A: Да, но потребуется модификация скриптов для разных путей и портов.

**Q: Как добавить уведомления?**
A: Можете добавить отправку email или push-уведомлений в функции логирования.

---

## Поддержка

Если у вас возникли проблемы:

1. Проверьте логи в `C:\SECS\Code\`
2. Используйте `control-system.bat` для диагностики
3. Запустите тест сервера через меню
4. Проверьте права доступа и системные требования

**Удачной работы с RenderFin Server! 🚀** 