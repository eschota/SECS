# 🚀 Система строительства корпуса корабля

## 📋 Описание

Полнофункциональная система строительства корпуса корабля для Unity, которая позволяет игрокам создавать и редактировать корпус корабля в режиме редактора. Система работает только в состоянии `_ship_state_editor_main_module_0`.

## 🎯 Основные возможности

- ✅ **Строительство стен** - зажатие ЛКМ и перетаскивание для создания линий стен
- ✅ **Размещение дверей** - создание проходов между отсеками
- ✅ **Привязка к сетке** - все объекты автоматически привязываются к сетке 1x1 метр
- ✅ **Сериализация** - сохранение и загрузка корпуса
- ✅ **Интеграция с UI** - автоматическое переключение режимов
- ✅ **События** - система событий для интеграции с другими системами

## 🏗️ Архитектура системы

```
HULL.cs (главный компонент)
├── HullNode.cs (данные объектов)
├── HullBuilder.cs (управление строительством)
├── HullPointPrefab.cs (префаб точки)
├── HullWallPrefab.cs (префаб стены)
├── HullDoorPrefab.cs (префаб двери)
├── HullTest.cs (тестирование)
└── HullQuickTest.cs (быстрое тестирование)
```

## 🚀 Быстрый старт

### 1. Настройка в Unity

1. **Добавьте компонент HULL на GameObject:**
   ```csharp
   GameObject hullObject = new GameObject("ShipHull");
   HULL hullComponent = hullObject.AddComponent<HULL>();
   ```

2. **Добавьте HullBuilder:**
   ```csharp
   HullBuilder builder = hullObject.AddComponent<HullBuilder>();
   builder.hullComponent = hullComponent;
   ```

3. **Создайте префабы:**
   - Создайте GameObject с компонентом `HullPointPrefab`
   - Создайте GameObject с компонентом `HullWallPrefab`
   - Создайте GameObject с компонентом `HullDoorPrefab`
   - Назначьте их в компоненте HULL

### 2. Переключение в режим строительства

```csharp
SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
```

### 3. Управление

- **ЛКМ (зажать и тянуть)** - строительство стен
- **ПКМ** - отмена строительства
- **1** - режим стен
- **2** - режим дверей
- **3** - режим точек

## 📁 Структура файлов

| Файл | Описание |
|------|----------|
| `HULL.cs` | Главный компонент управления корпусом |
| `HullNode.cs` | Компонент для хранения данных объектов |
| `HullBuilder.cs` | Система управления строительством |
| `HullPointPrefab.cs` | Префаб точки соединения |
| `HullWallPrefab.cs` | Префаб стены с автоматическим изменением размера |
| `HullDoorPrefab.cs` | Префаб двери между отсеками |
| `HullTest.cs` | Полное тестирование системы |
| `HullQuickTest.cs` | Быстрое тестирование |
| `HULL_SYSTEM_GUIDE.md` | Подробная документация |
| `README_HULL_SYSTEM.md` | Этот файл |

## 🔧 API системы

### Основные классы

#### HullPoint
```csharp
public class HullPoint
{
    public Vector3 position;  // Позиция в мировых координатах
    public int id;           // Уникальный идентификатор
}
```

#### HullWall
```csharp
public class HullWall
{
    public int startPointId;     // ID начальной точки
    public int endPointId;       // ID конечной точки
    public Vector3 startPosition; // Позиция начала
    public Vector3 endPosition;   // Позиция конца
    public float length;         // Длина стены
    public Quaternion rotation;  // Поворот стены
}
```

#### HullDoor
```csharp
public class HullDoor
{
    public int startPointId;     // ID начальной точки
    public int endPointId;       // ID конечной точки
    public Vector3 position;     // Позиция двери
    public Quaternion rotation;  // Поворот двери
}
```

### Основные методы HULL

```csharp
// Добавление точки
public void AddPoint(Vector3 position)

// Создание стены между последними точками
public void CreateWallFromLastPoints()

// Добавление двери
public void AddDoor(int startPointId, int endPointId)

// Сериализация
public string SerializeHull()
public void DeserializeHull(string jsonData)
```

### События системы

```csharp
// События HULL
public static event Action<HullPoint> OnPointAdded;
public static event Action<HullWall> OnWallAdded;
public static event Action<HullDoor> OnDoorAdded;

// События HullBuilder
public static event Action<BuildMode> OnBuildModeChanged;
public static event Action<bool> OnBuildingStateChanged;
```

## 🧪 Тестирование

### Автоматическое тестирование

1. Добавьте `HullQuickTest` на GameObject в сцене
2. Включите `runTestOnStart = true`
3. Запустите сцену - тест выполнится автоматически

### Ручное тестирование

```csharp
// Подписка на события
HULL.OnPointAdded += (point) => Debug.Log($"Добавлена точка {point.id}");
HULL.OnWallAdded += (wall) => Debug.Log($"Добавлена стена {wall.length}m");

// Переключение режимов
hullBuilder.SetBuildMode(HullBuilder.BuildMode.Wall);
hullBuilder.SetBuildMode(HullBuilder.BuildMode.Door);
```

### UI тестирования

В режиме Play используйте кнопки в правом верхнем углу экрана:
- **Run Quick Test** - запуск быстрого теста
- **Switch to Build Mode** - переключение в режим строительства
- **Switch to Space Mode** - переключение в режим космоса
- **Create Test Hull** - создание тестового корпуса
- **Create Test Builder** - создание тестового строителя

## 💾 Сохранение и загрузка

```csharp
// Сохранение корпуса
string hullData = hullComponent.SerializeHull();
PlayerPrefs.SetString("SavedHull", hullData);
PlayerPrefs.Save();

// Загрузка корпуса
string savedData = PlayerPrefs.GetString("SavedHull", "");
hullComponent.DeserializeHull(savedData);
```

## 🔗 Интеграция с существующей системой

Система автоматически интегрируется с существующей системой состояний корабля:

- ✅ Активируется только в состоянии `_ship_state_editor_main_module_0`
- ✅ Подписывается на события `SHIP_UI.ChangeState`
- ✅ Автоматически включает/выключает режим строительства
- ✅ Не конфликтует с другими системами

## 🎮 Примеры использования

### Создание простого корпуса

```csharp
// Создаем прямоугольный корпус 5x5 метров
Vector3[] corners = {
    new Vector3(0, 0, 0),
    new Vector3(5, 0, 0),
    new Vector3(5, 0, 5),
    new Vector3(0, 0, 5)
};

foreach (Vector3 corner in corners)
{
    hullComponent.AddPoint(corner);
}

// Создаем стены между углами
for (int i = 0; i < corners.Length; i++)
{
    hullComponent.CreateWallFromLastPoints();
}
```

### Добавление двери

```csharp
// Добавляем дверь между точками 0 и 1
hullComponent.AddDoor(0, 1);
```

### Обработка событий

```csharp
void Start()
{
    HULL.OnPointAdded += OnPointAdded;
    HULL.OnWallAdded += OnWallAdded;
    HULL.OnDoorAdded += OnDoorAdded;
}

void OnPointAdded(HullPoint point)
{
    Debug.Log($"Добавлена точка {point.id} в позиции {point.position}");
}

void OnWallAdded(HullWall wall)
{
    Debug.Log($"Создана стена длиной {wall.length} метров");
}

void OnDoorAdded(HullDoor door)
{
    Debug.Log($"Добавлена дверь между точками {door.startPointId} и {door.endPointId}");
}
```

## 🐛 Устранение неполадок

### Проблема: Система не активируется
**Решение:** Проверьте, что состояние корабля установлено в `_ship_state_editor_main_module_0`

### Проблема: Объекты не создаются
**Решение:** Убедитесь, что назначены префабы в компоненте HULL

### Проблема: Строительство не работает
**Решение:** Проверьте, что камера настроена правильно и есть коллайдеры для raycast

### Проблема: Ошибки компиляции
**Решение:** Убедитесь, что все скрипты находятся в правильных папках и имеют .meta файлы

## 📈 Производительность

- ✅ Оптимизировано для работы с корпусами до 1000 точек
- ✅ Автоматическая очистка памяти при переключении режимов
- ✅ Эффективная сериализация данных
- ✅ Минимальное использование ресурсов в неактивном состоянии

## 🔮 Планы развития

- [ ] Добавление поддержки многоэтажных корпусов
- [ ] Система валидации корпуса
- [ ] Интеграция с физикой корабля
- [ ] Система модулей и отсеков
- [ ] Экспорт/импорт корпусов

## 📞 Поддержка

При возникновении проблем:

1. Проверьте консоль Unity на наличие ошибок
2. Убедитесь, что все компоненты правильно настроены
3. Запустите `HullQuickTest` для диагностики
4. Обратитесь к документации в `HULL_SYSTEM_GUIDE.md`

---

**🎉 Система готова к использованию! Удачного строительства кораблей! 🚀** 