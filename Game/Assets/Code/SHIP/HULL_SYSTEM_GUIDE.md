# Система строительства корпуса корабля

## Обзор

Система строительства корпуса корабля позволяет игрокам создавать и редактировать корпус корабля в режиме редактора. Система работает только в состоянии `_ship_state_editor_main_module_0`.

## Основные компоненты

### 1. HULL.cs
Главный компонент для управления корпусом корабля.

**Основные функции:**
- Управление точками, стенами и дверями корпуса
- Сериализация/десериализация данных корпуса
- Интеграция с системой состояний корабля

**Ключевые методы:**
- `AddPoint(Vector3 position)` - добавление точки
- `CreateWallFromLastPoints()` - создание стены между последними точками
- `AddDoor(int startPointId, int endPointId)` - добавление двери
- `SerializeHull()` / `DeserializeHull(string jsonData)` - сохранение/загрузка

### 2. HullNode.cs
Компонент для хранения данных объектов корпуса (точки, стены, двери).

**Типы узлов:**
- `Point` - точка соединения
- `Wall` - стена между точками
- `Door` - дверь между отсеками

### 3. HullBuilder.cs
Система управления строительством корпуса.

**Режимы строительства:**
- `Wall` - строительство стен (по умолчанию)
- `Door` - размещение дверей
- `Point` - размещение отдельных точек

### 4. Префабы
- `HullPointPrefab.cs` - префаб точки
- `HullWallPrefab.cs` - префаб стены
- `HullDoorPrefab.cs` - префаб двери

## Как использовать

### Настройка в Unity

1. **Создайте GameObject с компонентом HULL:**
   ```
   GameObject hullObject = new GameObject("ShipHull");
   HULL hullComponent = hullObject.AddComponent<HULL>();
   ```

2. **Назначьте префабы:**
   - Создайте префабы точек, стен и дверей
   - Назначьте их в компоненте HULL в полях:
     - `pointPrefab`
     - `wallPrefab`
     - `doorPrefab`

3. **Добавьте HullBuilder:**
   ```
   HullBuilder builder = hullObject.AddComponent<HullBuilder>();
   builder.hullComponent = hullComponent;
   ```

### Управление строительством

#### Клавиши управления:
- **ЛКМ (зажать и тянуть)** - строительство стен/линий
- **ПКМ** - отмена текущего строительства
- **1** - режим строительства стен
- **2** - режим размещения дверей
- **3** - режим размещения точек

#### Программное управление:

```csharp
// Переключение в режим строительства
SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);

// Изменение режима строительства
hullBuilder.SetBuildMode(HullBuilder.BuildMode.Wall);

// Добавление точки программно
hullComponent.AddPoint(new Vector3(0, 0, 0));

// Создание стены между точками
hullComponent.CreateWallFromLastPoints();

// Добавление двери
hullComponent.AddDoor(startPointId, endPointId);
```

### События системы

```csharp
// Подписка на события
HULL.OnPointAdded += (point) => Debug.Log($"Добавлена точка {point.id}");
HULL.OnWallAdded += (wall) => Debug.Log($"Добавлена стена {wall.length}m");
HULL.OnDoorAdded += (door) => Debug.Log($"Добавлена дверь");

HullBuilder.OnBuildModeChanged += (mode) => Debug.Log($"Режим: {mode}");
HullBuilder.OnBuildingStateChanged += (active) => Debug.Log($"Строительство: {active}");
```

## Структура данных

### HullPoint
```csharp
public class HullPoint
{
    public Vector3 position;  // Позиция в мировых координатах
    public int id;           // Уникальный идентификатор
}
```

### HullWall
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

### HullDoor
```csharp
public class HullDoor
{
    public int startPointId;     // ID начальной точки
    public int endPointId;       // ID конечной точки
    public Vector3 position;     // Позиция двери
    public Quaternion rotation;  // Поворот двери
}
```

## Сетка и привязка

Система использует сетку 1x1 метр как базовый юнит. Все объекты автоматически привязываются к этой сетке:

```csharp
private Vector3 SnapToGrid(Vector3 worldPosition)
{
    float gridSize = 1f;
    return new Vector3(
        Mathf.Round(worldPosition.x / gridSize) * gridSize,
        Mathf.Round(worldPosition.y / gridSize) * gridSize,
        Mathf.Round(worldPosition.z / gridSize) * gridSize
    );
}
```

## Сериализация

Корпус можно сохранять и загружать:

```csharp
// Сохранение
string hullData = hullComponent.SerializeHull();
PlayerPrefs.SetString("SavedHull", hullData);

// Загрузка
string savedData = PlayerPrefs.GetString("SavedHull", "");
hullComponent.DeserializeHull(savedData);
```

## Тестирование

Используйте компонент `HullTest` для тестирования системы:

1. Добавьте `HullTest` на GameObject в сцене
2. Назначьте ссылки на `HULL` и `HullBuilder`
3. Включите `autoTest` для автоматического тестирования
4. Или используйте кнопки в OnGUI для ручного тестирования

## Интеграция с существующей системой

Система автоматически интегрируется с существующей системой состояний корабля:

- Активируется только в состоянии `_ship_state_editor_main_module_0`
- Подписывается на события `SHIP_UI.ChangeState`
- Автоматически включает/выключает режим строительства

## Рекомендации по использованию

1. **Производительность:** Не создавайте слишком много точек (>1000) для оптимальной производительности
2. **Память:** Регулярно сохраняйте корпус для предотвращения потери данных
3. **UI:** Создайте UI элементы для управления строительством (кнопки режимов, сохранение/загрузка)
4. **Валидация:** Добавьте проверки на корректность создаваемого корпуса

## Примеры использования

### Создание простого прямоугольного корпуса:

```csharp
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

### Добавление двери между отсеками:

```csharp
// Находим точки для двери
int startPointId = 0;
int endPointId = 1;

// Добавляем дверь
hullComponent.AddDoor(startPointId, endPointId);
```

## Устранение неполадок

### Проблема: Система не активируется
**Решение:** Проверьте, что состояние корабля установлено в `_ship_state_editor_main_module_0`

### Проблема: Объекты не создаются
**Решение:** Убедитесь, что назначены префабы в компоненте HULL

### Проблема: Строительство не работает
**Решение:** Проверьте, что камера настроена правильно и есть коллайдеры для raycast

### Проблема: Ошибки компиляции
**Решение:** Убедитесь, что все скрипты находятся в правильных папках и имеют .meta файлы 