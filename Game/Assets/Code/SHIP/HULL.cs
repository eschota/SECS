using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public class HullPoint
{
    public Vector3 position;
    public int id;
    
    public HullPoint(Vector3 pos, int pointId)
    {
        position = pos;
        id = pointId;
    }
}

[Serializable]
public class HullWall
{
    public int startPointId;
    public int endPointId;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public float length;
    public Quaternion rotation;
    
    public HullWall(int startId, int endId, Vector3 startPos, Vector3 endPos)
    {
        startPointId = startId;
        endPointId = endId;
        startPosition = startPos;
        endPosition = endPos;
        length = Vector3.Distance(startPos, endPos);
        rotation = Quaternion.LookRotation(endPos - startPos);
    }
}

[Serializable]
public class HullDoor
{
    public int startPointId;
    public int endPointId;
    public Vector3 position;
    public Quaternion rotation;
    
    public HullDoor(int startId, int endId, Vector3 pos, Quaternion rot)
    {
        startPointId = startId;
        endPointId = endId;
        position = pos;
        rotation = rot;
    }
}

public class HULL : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] public GameObject pointPrefab;
    [SerializeField] public GameObject wallPrefab;
    [SerializeField] public GameObject doorPrefab;
    
    [Header("Building Settings")]
    [SerializeField] private float gridSize = 1f; // Размер сетки 1x1 метр
    [SerializeField] private LayerMask gridLayerMask = 1;
    
    [Header("Hull Data")]
    [SerializeField] public List<HullPoint> points = new List<HullPoint>();
    [SerializeField] public List<HullWall> walls = new List<HullWall>();
    [SerializeField] public List<HullDoor> doors = new List<HullDoor>();
    
    // Runtime objects
    private List<GameObject> pointObjects = new List<GameObject>();
    private List<GameObject> wallObjects = new List<GameObject>();
    private List<GameObject> doorObjects = new List<GameObject>();
    
    [Header("Debug Info")]
    [SerializeField] public bool isBuilding = false;
    [SerializeField] public Vector3 lastMousePosition;
    [SerializeField] public int nextPointId = 0;
    
    // Система предварительного просмотра
    [SerializeField] public GameObject previewPoint;
    [SerializeField] public GameObject previewWall;
    [SerializeField] public Vector3 previewPosition;
    [SerializeField] public bool isDragging = false;
    [SerializeField] public Vector3 dragStartPosition;
    [SerializeField] public List<Vector3> currentDragPath = new List<Vector3>();
    
    // Events
    public static event Action<HullPoint> OnPointAdded;
    public static event Action<HullWall> OnWallAdded;
    public static event Action<HullDoor> OnDoorAdded;
    
    void Start()
    {
        // Подписываемся на изменение состояния корабля
        SHIP_UI.ChangeState += OnShipStateChanged;
    }
    
    void OnDestroy()
    {
        SHIP_UI.ChangeState -= OnShipStateChanged;
    }
    
    private void OnShipStateChanged(SHIP_UI.State newState)
    {
        bool wasBuilding = isBuilding;
        
        Debug.Log($"[HULL] Изменение состояния: {newState}");
        
        // Активируем строительство в любом состоянии редактора
        isBuilding = (newState == SHIP_UI.State._ship_state_editor_main_module_0 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_1 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_2 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_3 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_4 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_5 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_6 ||
                     newState == SHIP_UI.State._ship_state_editor_main_module_7);
        
        if (isBuilding && !wasBuilding)
        {
            Debug.Log("[HULL] Включен режим строительства корпуса");
            Debug.Log($"[HULL] pointPrefab: {(pointPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
            Debug.Log($"[HULL] wallPrefab: {(wallPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
            Debug.Log($"[HULL] doorPrefab: {(doorPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
            
            // Создаем предварительный просмотр точки сразу при входе в режим строительства
            CreatePreviewPoint();
        }
        else if (!isBuilding && wasBuilding)
        {
            Debug.Log("[HULL] Режим строительства отключен");
            // Очищаем предварительный просмотр
            ClearPreview();
        }
    }
    
    void Update()
    {
        if (!isBuilding) 
        {
            // Очищаем предварительный просмотр при выходе из режима строительства
            ClearPreview();
            return;
        }
        
        HandleBuildingInput();
        UpdatePreview();
    }
    
    private void HandleBuildingInput()
    {
        // Начало перетаскивания
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 gridPosition = SnapToGrid(GetMouseWorldPosition());
            StartDragging(gridPosition);
        }
        
        // Перетаскивание
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 gridPosition = SnapToGrid(GetMouseWorldPosition());
            ContinueDragging(gridPosition);
        }
        
        // Завершение перетаскивания
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            FinishDragging();
        }
        
        // Отмена строительства правой кнопкой мыши (только во время перетаскивания)
        if (Input.GetMouseButtonDown(1) && isDragging)
        {
            CancelDragging();
        }
        
        // Удаление ближайшего узла правой кнопкой мыши (когда не перетаскиваем)
        if (Input.GetMouseButtonDown(1) && !isDragging)
        {
            DeleteNearestNode();
        }
    }
    
    private void StartDragging(Vector3 position)
    {
        isDragging = true;
        dragStartPosition = SnapToGrid(position);
        currentDragPath.Clear();
        currentDragPath.Add(dragStartPosition);
        
        // Проверяем, есть ли уже точка в этой позиции
        HullPoint existingPoint = FindPointAtPosition(dragStartPosition);
        
        if (existingPoint != null)
        {
            Debug.Log($"[HULL] Присоединяемся к существующей точке {existingPoint.id} в позиции {dragStartPosition}");
            // Начинаем строительство от существующей точки
        }
        else
        {
            // Создаем новую точку
            AddPoint(dragStartPosition);
            Debug.Log($"[HULL] Начато строительство в позиции {dragStartPosition}");
        }
    }
    
    private void ContinueDragging(Vector3 position)
    {
        // Получаем последнюю позицию в пути
        Vector3 lastPathPosition = currentDragPath.Count > 0 ? currentDragPath[currentDragPath.Count - 1] : dragStartPosition;
        
        // Вычисляем все промежуточные точки на траектории
        List<Vector3> intermediatePoints = CalculateIntermediatePoints(lastPathPosition, position);
        
        Debug.Log($"[HULL] ContinueDragging: найдено {intermediatePoints.Count} промежуточных точек");
        
        // Добавляем все промежуточные точки
        foreach (Vector3 point in intermediatePoints)
        {
            Vector3 snappedPoint = SnapToGrid(point);
            
            // Проверяем, есть ли уже точка в этой позиции
            HullPoint existingPoint = FindPointAtPosition(snappedPoint);
            
            if (existingPoint != null)
            {
                Debug.Log($"[HULL] Присоединяемся к существующей точке {existingPoint.id} в позиции {snappedPoint}");
                currentDragPath.Add(snappedPoint);
                
                // Создаем стену от предыдущей точки к существующей
                if (points.Count >= 1)
                {
                    CreateWallToExistingPoint(existingPoint);
                }
            }
            else if (!IsPositionOccupied(snappedPoint))
            {
                Debug.Log($"[HULL] Добавляем новую точку в позиции {snappedPoint}");
                currentDragPath.Add(snappedPoint);
                AddPoint(snappedPoint);
                
                // Создаем стену от предыдущей точки к текущей
                if (points.Count >= 2)
                {
                    CreateWallFromLastPoints();
                }
            }
            else
            {
                Debug.Log($"[HULL] Позиция {snappedPoint} занята, пропускаем");
            }
        }
        
        // Также создаем стену от последней точки пути к текущей позиции мыши
        if (currentDragPath.Count > 0)
        {
            Vector3 lastPoint = currentDragPath[currentDragPath.Count - 1];
            Vector3 snappedPosition = SnapToGrid(position);
            
            // Создаем временную стену к позиции мыши (если она отличается от последней точки)
            if (Vector3.Distance(lastPoint, snappedPosition) > gridSize * 0.1f)
            {
                CreateTemporaryWall(lastPoint, snappedPosition);
            }
        }
        
        lastMousePosition = position;
    }
    
    private void FinishDragging()
    {
        // Проверяем, можно ли замкнуть линию к начальной точке
        if (currentDragPath.Count > 2)
        {
            Vector3 mousePosition = GetMouseWorldPosition();
            Vector3 snappedMousePosition = SnapToGrid(mousePosition);
            Vector3 firstPosition = currentDragPath[0];
            
            // Если позиция мыши близка к первой точке, замыкаем линию
            if (Vector3.Distance(snappedMousePosition, firstPosition) <= gridSize * 1.5f)
            {
                Debug.Log("[HULL] Замыкаем линию к начальной точке");
                
                // Добавляем точку в позиции мыши (если она не занята)
                if (!IsPositionOccupied(snappedMousePosition))
                {
                    AddPoint(snappedMousePosition);
                    
                    if (points.Count >= 2)
                    {
                        CreateWallFromLastPoints();
                    }
                }
                
                // Создаем финальную стену для замыкания
                if (points.Count >= 2)
                {
                    CreateClosingWall();
                }
            }
        }
        
        // Очищаем временную стену
        if (previewWall != null)
        {
            DestroyImmediate(previewWall);
            previewWall = null;
        }
        
        isDragging = false;
        currentDragPath.Clear();
        
        Debug.Log($"[HULL] Завершено строительство. Создано точек: {points.Count}, стен: {walls.Count}");
    }
    
    public void CancelDragging()
    {
        Debug.Log("[HULL] Отмена строительства");
        
        // Удаляем все точки, созданные в текущем перетаскивании
        int pointsToRemove = currentDragPath.Count;
        for (int i = 0; i < pointsToRemove; i++)
        {
            if (points.Count > 0)
            {
                // Удаляем последнюю точку
                HullPoint lastPoint = points[points.Count - 1];
                points.RemoveAt(points.Count - 1);
                
                // Удаляем визуальный объект точки
                if (pointObjects.Count > 0)
                {
                    GameObject pointObj = pointObjects[pointObjects.Count - 1];
                    pointObjects.RemoveAt(pointObjects.Count - 1);
                    DestroyImmediate(pointObj);
                }
                
                Debug.Log($"[HULL] Удалена точка {lastPoint.id}");
            }
        }
        
        // Удаляем все стены, созданные в текущем перетаскивании
        int wallsToRemove = currentDragPath.Count - 1; // Количество стен = количество точек - 1
        for (int i = 0; i < wallsToRemove; i++)
        {
            if (walls.Count > 0)
            {
                // Удаляем последнюю стену
                HullWall lastWall = walls[walls.Count - 1];
                walls.RemoveAt(walls.Count - 1);
                
                // Удаляем визуальный объект стены
                if (wallObjects.Count > 0)
                {
                    GameObject wallObj = wallObjects[wallObjects.Count - 1];
                    wallObjects.RemoveAt(wallObjects.Count - 1);
                    DestroyImmediate(wallObj);
                }
                
                Debug.Log($"[HULL] Удалена стена {lastWall.startPointId}-{lastWall.endPointId}");
            }
        }
        
        // Очищаем временную стену
        if (previewWall != null)
        {
            DestroyImmediate(previewWall);
            previewWall = null;
        }
        
        isDragging = false;
        currentDragPath.Clear();
        
        Debug.Log($"[HULL] Отмена завершена. Осталось точек: {points.Count}, стен: {walls.Count}");
    }
    
    private void CreateClosingWall()
    {
        if (points.Count < 2) return;
        
        // Создаем стену от последней точки к первой
        HullPoint firstPoint = points[0];
        HullPoint lastPoint = points[points.Count - 1];
        
        // Проверяем, не создана ли уже такая стена
        if (IsWallExists(firstPoint.id, lastPoint.id))
        {
            Debug.Log($"[HULL] Стена замыкания уже существует");
            return;
        }
        
        // Создаем стену замыкания
        Vector3 startPos = firstPoint.position;
        Vector3 endPos = lastPoint.position;
        startPos.y = 0f;
        endPos.y = 0f;
        
        HullWall closingWall = new HullWall(firstPoint.id, lastPoint.id, startPos, endPos);
        walls.Add(closingWall);
        
        Debug.Log($"[HULL] Создана стена замыкания от точки {firstPoint.id} к точке {lastPoint.id}");
        
        // Создаем визуальный объект стены замыкания
        if (wallPrefab != null)
        {
            Vector3 wallCenter = (startPos + endPos) * 0.5f;
            GameObject wallObj = Instantiate(wallPrefab, wallCenter, closingWall.rotation, transform);
            wallObj.name = $"HullWall_Closing_{firstPoint.id}_{lastPoint.id}";
            wallObjects.Add(wallObj);
            
            Debug.Log($"[HULL] Создан GameObject стены замыкания {wallObj.name}");
            
            // Настраиваем размер стены
            HullNode hullNode = wallObj.GetComponent<HullNode>();
            if (hullNode == null)
            {
                hullNode = wallObj.AddComponent<HullNode>();
                Debug.Log($"[HULL] Добавлен компонент HullNode к стене замыкания");
            }
            hullNode.Initialize(closingWall);
        }
        else
        {
            Debug.LogError($"[HULL] wallPrefab не назначен для стены замыкания!");
        }
        
        OnWallAdded?.Invoke(closingWall);
    }
    
    public void CreatePreviewPoint()
    {
        if (previewPoint != null)
        {
            DestroyImmediate(previewPoint);
            previewPoint = null;
        }
        
        if (pointPrefab != null)
        {
            previewPosition = GetMouseWorldPosition();
            previewPosition = SnapToGrid(previewPosition);
            
            Debug.Log($"[HULL] Создаем предварительный просмотр точки в позиции {previewPosition}");
            previewPoint = Instantiate(pointPrefab, previewPosition, Quaternion.identity, transform);
            previewPoint.name = "PreviewPoint";
            
            // Делаем полупрозрачным
            Renderer renderer = previewPoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 0.5f;
                renderer.material.color = color;
            }
            
            Debug.Log("[HULL] Предварительный просмотр точки создан");
        }
        else
        {
            Debug.LogError("[HULL] pointPrefab не назначен!");
        }
    }
    
    public void UpdatePreview()
    {
        // Отладка
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            Debug.Log($"[HULL] UpdatePreview - isBuilding: {isBuilding}, previewPoint: {(previewPoint != null ? "существует" : "НЕ СУЩЕСТВУЕТ")}, pointPrefab: {(pointPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
        }
        
        // Если мы в режиме строительства, но предварительный просмотр не создан - создаем его
        if (isBuilding && previewPoint == null && pointPrefab != null)
        {
            Debug.Log("[HULL] Принудительное создание предварительного просмотра");
            CreatePreviewPoint();
        }
        
        // Обновляем позицию предварительного просмотра
        if (previewPoint != null && isBuilding)
        {
            // Получаем новую позицию мыши и привязываем к сетке
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 targetPosition = SnapToGrid(mousePos);
            
            // Плавная интерполяция к новой позиции
            Vector3 currentPosition = previewPoint.transform.position;
            Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 10f);
            previewPoint.transform.position = newPosition;
            
            // Обновляем previewPosition для других методов
            previewPosition = targetPosition;
        }
        
        // Обновляем предварительный просмотр стены при перетаскивании
        if (isDragging && currentDragPath.Count >= 2)
        {
            UpdatePreviewWall();
        }
    }
    
    private void UpdatePreviewWall()
    {
        // Этот метод теперь используется только для обновления предварительного просмотра стены
        // Основная логика создания стен перенесена в ContinueDragging
        if (isDragging && currentDragPath.Count >= 2 && previewWall != null)
        {
            Vector3 startPos = currentDragPath[currentDragPath.Count - 2];
            Vector3 endPos = previewPosition;
            
            // Принудительно устанавливаем Y=0
            startPos.y = 0f;
            endPos.y = 0f;
            
            Vector3 center = (startPos + endPos) * 0.5f;
            Vector3 direction = (endPos - startPos).normalized;
            
            previewWall.transform.position = center;
            previewWall.transform.rotation = Quaternion.LookRotation(direction);
            
            // Обновляем размер стены
            HullWallPrefab wallPrefabComponent = previewWall.GetComponent<HullWallPrefab>();
            if (wallPrefabComponent != null)
            {
                wallPrefabComponent.UpdateWallVisual(startPos, endPos);
            }
        }
    }
    
    private void ClearPreview()
    {
        // Удаляем предварительный просмотр точки
        if (previewPoint != null)
        {
            DestroyImmediate(previewPoint);
            previewPoint = null;
        }
        
        // Удаляем предварительный просмотр стены
        if (previewWall != null)
        {
            DestroyImmediate(previewWall);
            previewWall = null;
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[HULL] Camera.main не найден!");
            return Vector3.zero;
        }
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayerMask))
        {
            // Принудительно устанавливаем Y=0
            Vector3 point = hit.point;
            point.y = 0f;
            return point;
        }
        
        // Если луч не попал никуда, используем плоскость XZ на высоте 0
        float distance = 0f;
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        
        if (plane.Raycast(ray, out distance))
        {
            Vector3 point = ray.GetPoint(distance);
            point.y = 0f; // Принудительно устанавливаем Y=0
            return point;
        }
        
        // Если ничего не работает, возвращаем позицию камеры + направление мыши на плоскости Y=0
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Расстояние от камеры
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.y = 0f; // Принудительно устанавливаем Y=0
        return worldPos;
    }
    
    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / gridSize) * gridSize,
            0f, // Всегда на высоте 0
            Mathf.Round(worldPosition.z / gridSize) * gridSize
        );
    }
    
    private List<Vector3> CalculateIntermediatePoints(Vector3 startPos, Vector3 endPos)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Привязываем точки к сетке и устанавливаем Y=0
        Vector3 snappedStart = SnapToGrid(startPos);
        Vector3 snappedEnd = SnapToGrid(endPos);
        snappedStart.y = 0f;
        snappedEnd.y = 0f;
        
        // Если точки совпадают, возвращаем пустой список
        if (snappedStart == snappedEnd)
        {
            return points;
        }
        
        // Вычисляем направление и расстояние (только в плоскости XZ)
        Vector3 direction = (snappedEnd - snappedStart).normalized;
        float distance = Vector3.Distance(snappedStart, snappedEnd);
        
        // Вычисляем количество промежуточных точек
        int steps = Mathf.CeilToInt(distance / gridSize);
        
        // Добавляем промежуточные точки
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 intermediatePos = Vector3.Lerp(snappedStart, snappedEnd, t);
            Vector3 snappedIntermediatePos = SnapToGrid(intermediatePos);
            snappedIntermediatePos.y = 0f; // Принудительно устанавливаем Y=0
            
            // Добавляем только если это новая позиция
            if (!points.Contains(snappedIntermediatePos) && snappedIntermediatePos != snappedStart)
            {
                points.Add(snappedIntermediatePos);
            }
        }
        
        return points;
    }
    
    private bool IsPositionOccupied(Vector3 position)
    {
        // Проверяем, есть ли уже точка в этой позиции (только X и Z координаты)
        foreach (var point in points)
        {
            // Сравниваем только X и Z координаты, игнорируем Y
            float distanceXZ = Mathf.Sqrt(
                Mathf.Pow(point.position.x - position.x, 2) + 
                Mathf.Pow(point.position.z - position.z, 2)
            );
            
            if (distanceXZ < gridSize * 0.5f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private HullPoint FindPointAtPosition(Vector3 position)
    {
        // Находим точку в указанной позиции (только X и Z координаты)
        foreach (var point in points)
        {
            // Сравниваем только X и Z координаты, игнорируем Y
            float distanceXZ = Mathf.Sqrt(
                Mathf.Pow(point.position.x - position.x, 2) + 
                Mathf.Pow(point.position.z - position.z, 2)
            );
            
            if (distanceXZ < gridSize * 0.5f)
            {
                return point;
            }
        }
        
        return null;
    }
    
    public void DeleteNearestNode()
    {
        Vector3 mousePosition = GetMouseWorldPosition();
        Vector3 snappedPosition = SnapToGrid(mousePosition);
        
        // Ищем ближайшую точку в радиусе 3 метров
        HullPoint nearestPoint = null;
        float nearestDistance = 3f; // Радиус поиска 3 метра
        
        foreach (var point in points)
        {
            float distance = Vector3.Distance(point.position, snappedPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }
        }
        
        if (nearestPoint != null)
        {
            Debug.Log($"[HULL] Удаляем ближайшую точку {nearestPoint.id} на расстоянии {nearestDistance:F2}м");
            
            // Удаляем все стены, связанные с этой точкой
            List<HullWall> wallsToRemove = new List<HullWall>();
            foreach (var wall in walls)
            {
                if (wall.startPointId == nearestPoint.id || wall.endPointId == nearestPoint.id)
                {
                    wallsToRemove.Add(wall);
                }
            }
            
            // Удаляем стены
            foreach (var wall in wallsToRemove)
            {
                walls.Remove(wall);
                
                // Удаляем визуальный объект стены
                GameObject wallObj = wallObjects.Find(obj => obj.name == $"HullWall_{wall.startPointId}_{wall.endPointId}");
                if (wallObj != null)
                {
                    wallObjects.Remove(wallObj);
                    DestroyImmediate(wallObj);
                }
                
                Debug.Log($"[HULL] Удалена стена {wall.startPointId}-{wall.endPointId}");
            }
            
            // Удаляем точку
            points.Remove(nearestPoint);
            
            // Удаляем визуальный объект точки
            GameObject pointObj = pointObjects.Find(obj => obj.name == $"HullPoint_{nearestPoint.id}");
            if (pointObj != null)
            {
                pointObjects.Remove(pointObj);
                DestroyImmediate(pointObj);
            }
            
            Debug.Log($"[HULL] Точка {nearestPoint.id} удалена");
        }
        else
        {
            Debug.Log($"[HULL] Ближайшая точка не найдена в радиусе 3 метров");
        }
    }
    
    public void AddPoint(Vector3 position)
    {
        // Привязываем позицию к сетке и принудительно устанавливаем Y=0
        Vector3 snappedPosition = SnapToGrid(position);
        snappedPosition.y = 0f; // Дополнительная гарантия
        
        Debug.Log($"[HULL] AddPoint: попытка добавить точку в {snappedPosition}");
        
        // Проверяем, нет ли уже точки в этой позиции
        if (IsPositionOccupied(snappedPosition))
        {
            Debug.Log($"[HULL] Позиция {snappedPosition} уже занята, пропускаем");
            return; // Точка уже существует
        }
        
        // Создаем новую точку
        HullPoint newPoint = new HullPoint(snappedPosition, nextPointId++);
        points.Add(newPoint);
        
        Debug.Log($"[HULL] Создана HullPoint с ID {newPoint.id}");
        
        // Создаем визуальный объект точки
        if (pointPrefab != null)
        {
            GameObject pointObj = Instantiate(pointPrefab, snappedPosition, Quaternion.identity, transform);
            pointObj.name = $"HullPoint_{newPoint.id}";
            pointObjects.Add(pointObj);
            
            Debug.Log($"[HULL] Создан GameObject {pointObj.name}");
            
            // Добавляем компонент для хранения данных
            HullNode hullNode = pointObj.GetComponent<HullNode>();
            if (hullNode == null)
            {
                hullNode = pointObj.AddComponent<HullNode>();
                Debug.Log($"[HULL] Добавлен компонент HullNode");
            }
            hullNode.Initialize(newPoint);
        }
        else
        {
            Debug.LogError($"[HULL] pointPrefab не назначен!");
        }
        
        OnPointAdded?.Invoke(newPoint);
        Debug.Log($"[HULL] Добавлена точка {newPoint.id} в позиции {snappedPosition}");
    }
    
    private void CreateWallFromLastPoints()
    {
        if (points.Count < 2) 
        {
            Debug.Log($"[HULL] CreateWallFromLastPoints: недостаточно точек ({points.Count})");
            return;
        }
        
        HullPoint startPoint = points[points.Count - 2];
        HullPoint endPoint = points[points.Count - 1];
        
        Debug.Log($"[HULL] CreateWallFromLastPoints: создаем стену от точки {startPoint.id} к точке {endPoint.id}");
        
        // Проверяем, не создана ли уже такая стена
        if (IsWallExists(startPoint.id, endPoint.id))
        {
            Debug.Log($"[HULL] Стена от точки {startPoint.id} к точке {endPoint.id} уже существует");
            return;
        }
        
        // Создаем стену (принудительно на высоте Y=0)
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        startPos.y = 0f;
        endPos.y = 0f;
        
        HullWall newWall = new HullWall(startPoint.id, endPoint.id, startPos, endPos);
        walls.Add(newWall);
        
        Debug.Log($"[HULL] Создана HullWall с ID {startPoint.id}-{endPoint.id}");
        
        // Создаем визуальный объект стены
        if (wallPrefab != null)
        {
            Vector3 wallCenter = (startPoint.position + endPoint.position) * 0.5f;
            GameObject wallObj = Instantiate(wallPrefab, wallCenter, newWall.rotation, transform);
            wallObj.name = $"HullWall_{startPoint.id}_{endPoint.id}";
            wallObjects.Add(wallObj);
            
            Debug.Log($"[HULL] Создан GameObject стены {wallObj.name}");
            
            // Настраиваем размер стены
            HullNode hullNode = wallObj.GetComponent<HullNode>();
            if (hullNode == null)
            {
                hullNode = wallObj.AddComponent<HullNode>();
                Debug.Log($"[HULL] Добавлен компонент HullNode к стене");
            }
            hullNode.Initialize(newWall);
        }
        else
        {
            Debug.LogError($"[HULL] wallPrefab не назначен!");
        }
        
        OnWallAdded?.Invoke(newWall);
        Debug.Log($"[HULL] Создана стена от точки {startPoint.id} к точке {endPoint.id}");
    }
    
    private void CreateWallToExistingPoint(HullPoint existingPoint)
    {
        if (points.Count < 1) 
        {
            Debug.Log($"[HULL] CreateWallToExistingPoint: недостаточно точек ({points.Count})");
            return;
        }
        
        HullPoint startPoint = points[points.Count - 1];
        
        Debug.Log($"[HULL] CreateWallToExistingPoint: создаем стену от точки {startPoint.id} к существующей точке {existingPoint.id}");
        
        // Проверяем, не создана ли уже такая стена
        if (IsWallExists(startPoint.id, existingPoint.id))
        {
            Debug.Log($"[HULL] Стена от точки {startPoint.id} к точке {existingPoint.id} уже существует");
            return;
        }
        
        // Создаем стену (принудительно на высоте Y=0)
        Vector3 startPos = startPoint.position;
        Vector3 endPos = existingPoint.position;
        startPos.y = 0f;
        endPos.y = 0f;
        
        HullWall newWall = new HullWall(startPoint.id, existingPoint.id, startPos, endPos);
        walls.Add(newWall);
        
        Debug.Log($"[HULL] Создана HullWall с ID {startPoint.id}-{existingPoint.id}");
        
        // Создаем визуальный объект стены
        if (wallPrefab != null)
        {
            Vector3 wallCenter = (startPos + endPos) * 0.5f;
            GameObject wallObj = Instantiate(wallPrefab, wallCenter, newWall.rotation, transform);
            wallObj.name = $"HullWall_{startPoint.id}_{existingPoint.id}";
            wallObjects.Add(wallObj);
            
            Debug.Log($"[HULL] Создан GameObject стены {wallObj.name}");
            
            // Настраиваем размер стены
            HullNode hullNode = wallObj.GetComponent<HullNode>();
            if (hullNode == null)
            {
                hullNode = wallObj.AddComponent<HullNode>();
                Debug.Log($"[HULL] Добавлен компонент HullNode к стене");
            }
            hullNode.Initialize(newWall);
        }
        else
        {
            Debug.LogError($"[HULL] wallPrefab не назначен!");
        }
        
        OnWallAdded?.Invoke(newWall);
        Debug.Log($"[HULL] Создана стена от точки {startPoint.id} к существующей точке {existingPoint.id}");
    }
    
    private void CreateTemporaryWall(Vector3 startPos, Vector3 endPos)
    {
        // Создаем временную стену для предварительного просмотра
        if (previewWall == null && wallPrefab != null)
        {
            previewWall = Instantiate(wallPrefab, Vector3.zero, Quaternion.identity, transform);
            previewWall.name = "PreviewWall";
            
            // Делаем полупрозрачным
            Renderer renderer = previewWall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 0.5f;
                renderer.material.color = color;
            }
        }
        
        // Обновляем позицию и размер временной стены (принудительно на высоте Y=0)
        if (previewWall != null)
        {
            Vector3 startPosFlat = startPos;
            Vector3 endPosFlat = endPos;
            startPosFlat.y = 0f;
            endPosFlat.y = 0f;
            
            Vector3 center = (startPosFlat + endPosFlat) * 0.5f;
            Vector3 direction = (endPosFlat - startPosFlat).normalized;
            
            previewWall.transform.position = center;
            previewWall.transform.rotation = Quaternion.LookRotation(direction);
            
            // Обновляем размер стены
            HullWallPrefab wallPrefabComponent = previewWall.GetComponent<HullWallPrefab>();
            if (wallPrefabComponent != null)
            {
                wallPrefabComponent.UpdateWallVisual(startPosFlat, endPosFlat);
            }
        }
    }
    
    private bool IsWallExists(int startPointId, int endPointId)
    {
        // Проверяем, существует ли уже стена между этими точками
        foreach (var wall in walls)
        {
            if ((wall.startPointId == startPointId && wall.endPointId == endPointId) ||
                (wall.startPointId == endPointId && wall.endPointId == startPointId))
            {
                return true;
            }
        }
        return false;
    }
    
    public void AddDoor(int startPointId, int endPointId)
    {
        HullPoint startPoint = points.Find(p => p.id == startPointId);
        HullPoint endPoint = points.Find(p => p.id == endPointId);
        
        if (startPoint == null || endPoint == null) return;
        
        Vector3 doorPosition = (startPoint.position + endPoint.position) * 0.5f;
        Quaternion doorRotation = Quaternion.LookRotation(endPoint.position - startPoint.position);
        
        HullDoor newDoor = new HullDoor(startPointId, endPointId, doorPosition, doorRotation);
        doors.Add(newDoor);
        
        // Создаем визуальный объект двери
        if (doorPrefab != null)
        {
            GameObject doorObj = Instantiate(doorPrefab, doorPosition, doorRotation, transform);
            doorObj.name = $"HullDoor_{startPointId}_{endPointId}";
            doorObjects.Add(doorObj);
            
            HullNode hullNode = doorObj.GetComponent<HullNode>();
            if (hullNode == null)
            {
                hullNode = doorObj.AddComponent<HullNode>();
            }
            hullNode.Initialize(newDoor);
        }
        
        OnDoorAdded?.Invoke(newDoor);
        Debug.Log($"[HULL] Добавлена дверь между точками {startPointId} и {endPointId}");
    }
    
    // Методы для сериализации
    public string SerializeHull()
    {
        var hullData = new
        {
            points = points,
            walls = walls,
            doors = doors
        };
        
        return JsonUtility.ToJson(hullData, true);
    }
    
    public void DeserializeHull(string jsonData)
    {
        // Очищаем текущие данные
        ClearHull();
        
        // Десериализуем данные
        var hullData = JsonUtility.FromJson<HullData>(jsonData);
        
        if (hullData != null)
        {
            points = hullData.points ?? new List<HullPoint>();
            walls = hullData.walls ?? new List<HullWall>();
            doors = hullData.doors ?? new List<HullDoor>();
            
            // Воссоздаем визуальные объекты
            RecreateVisualObjects();
        }
    }
    
    public void ClearHull()
    {
        // Удаляем визуальные объекты
        foreach (var obj in pointObjects) DestroyImmediate(obj);
        foreach (var obj in wallObjects) DestroyImmediate(obj);
        foreach (var obj in doorObjects) DestroyImmediate(obj);
        
        pointObjects.Clear();
        wallObjects.Clear();
        doorObjects.Clear();
        
        // Очищаем данные
        points.Clear();
        walls.Clear();
        doors.Clear();
        
        nextPointId = 0;
    }
    
    private void RecreateVisualObjects()
    {
        // Воссоздаем точки
        foreach (var point in points)
        {
            if (pointPrefab != null)
            {
                GameObject pointObj = Instantiate(pointPrefab, point.position, Quaternion.identity, transform);
                pointObj.name = $"HullPoint_{point.id}";
                pointObjects.Add(pointObj);
                
                HullNode hullNode = pointObj.GetComponent<HullNode>();
                if (hullNode == null)
                {
                    hullNode = pointObj.AddComponent<HullNode>();
                }
                hullNode.Initialize(point);
            }
        }
        
        // Воссоздаем стены
        foreach (var wall in walls)
        {
            if (wallPrefab != null)
            {
                Vector3 wallCenter = (wall.startPosition + wall.endPosition) * 0.5f;
                GameObject wallObj = Instantiate(wallPrefab, wallCenter, wall.rotation, transform);
                wallObj.name = $"HullWall_{wall.startPointId}_{wall.endPointId}";
                wallObjects.Add(wallObj);
                
                HullNode hullNode = wallObj.GetComponent<HullNode>();
                if (hullNode == null)
                {
                    hullNode = wallObj.AddComponent<HullNode>();
                }
                hullNode.Initialize(wall);
            }
        }
        
        // Воссоздаем двери
        foreach (var door in doors)
        {
            if (doorPrefab != null)
            {
                GameObject doorObj = Instantiate(doorPrefab, door.position, door.rotation, transform);
                doorObj.name = $"HullDoor_{door.startPointId}_{door.endPointId}";
                doorObjects.Add(doorObj);
                
                HullNode hullNode = doorObj.GetComponent<HullNode>();
                if (hullNode == null)
                {
                    hullNode = doorObj.AddComponent<HullNode>();
                }
                hullNode.Initialize(door);
            }
        }
        
        // Обновляем nextPointId
        if (points.Count > 0)
        {
            nextPointId = points.Max(p => p.id) + 1;
        }
    }
    
    // Вспомогательный класс для сериализации
    [Serializable]
    private class HullData
    {
        public List<HullPoint> points;
        public List<HullWall> walls;
        public List<HullDoor> doors;
    }
}
