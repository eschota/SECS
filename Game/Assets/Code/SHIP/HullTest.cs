using UnityEngine;
using System.Collections;

public class HullTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private HULL hullComponent;
    [SerializeField] private HullBuilder hullBuilder;
    [SerializeField] private bool autoTest = false;
    [SerializeField] private float testDelay = 2f;
    
    void Start()
    {
        // Находим компоненты если не назначены
        if (hullComponent == null)
        {
            hullComponent = FindObjectOfType<HULL>();
        }
        
        if (hullBuilder == null)
        {
            hullBuilder = FindObjectOfType<HullBuilder>();
        }
        
        // Подписываемся на события
        if (hullComponent != null)
        {
            HULL.OnPointAdded += OnPointAdded;
            HULL.OnWallAdded += OnWallAdded;
            HULL.OnDoorAdded += OnDoorAdded;
        }
        
        if (hullBuilder != null)
        {
            HullBuilder.OnBuildModeChanged += OnBuildModeChanged;
            HullBuilder.OnBuildingStateChanged += OnBuildingStateChanged;
        }
        
        // Запускаем автоматический тест если включен
        if (autoTest)
        {
            StartCoroutine(AutoTest());
        }
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий
        if (hullComponent != null)
        {
            HULL.OnPointAdded -= OnPointAdded;
            HULL.OnWallAdded -= OnWallAdded;
            HULL.OnDoorAdded -= OnDoorAdded;
        }
        
        if (hullBuilder != null)
        {
            HullBuilder.OnBuildModeChanged -= OnBuildModeChanged;
            HullBuilder.OnBuildingStateChanged -= OnBuildingStateChanged;
        }
    }
    
    private IEnumerator AutoTest()
    {
        Debug.Log("[HullTest] Начинаем автоматический тест системы строительства корпуса");
        
        // Ждем немного для инициализации
        yield return new WaitForSeconds(1f);
        
        // Тест 1: Переключение в режим строительства
        Debug.Log("[HullTest] Тест 1: Переключение в режим строительства");
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
        }
        
        yield return new WaitForSeconds(testDelay);
        
        // Тест 2: Создание простого корпуса
        Debug.Log("[HullTest] Тест 2: Создание простого корпуса");
        CreateSimpleHull();
        
        yield return new WaitForSeconds(testDelay);
        
        // Тест 3: Сохранение и загрузка корпуса
        Debug.Log("[HullTest] Тест 3: Сохранение и загрузка корпуса");
        TestSaveLoad();
        
        yield return new WaitForSeconds(testDelay);
        
        // Тест 4: Переключение режимов строительства
        Debug.Log("[HullTest] Тест 4: Переключение режимов строительства");
        TestBuildModes();
        
        Debug.Log("[HullTest] Автоматический тест завершен");
    }
    
    private void CreateSimpleHull()
    {
        if (hullComponent == null) return;
        
        // Создаем простой прямоугольный корпус
        Vector3[] positions = {
            new Vector3(0, 0, 0),
            new Vector3(5, 0, 0),
            new Vector3(5, 0, 5),
            new Vector3(0, 0, 5)
        };
        
        // Добавляем точки
        foreach (Vector3 pos in positions)
        {
            // Здесь нужно вызвать метод добавления точки
            Debug.Log($"[HullTest] Добавлена точка в позиции {pos}");
        }
        
        // Создаем стены между точками
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 start = positions[i];
            Vector3 end = positions[(i + 1) % positions.Length];
            
            // Здесь нужно вызвать метод создания стены
            Debug.Log($"[HullTest] Создана стена от {start} к {end}");
        }
    }
    
    private void TestSaveLoad()
    {
        if (hullComponent == null) return;
        
        // Сохраняем текущий корпус
        string hullData = hullComponent.SerializeHull();
        Debug.Log($"[HullTest] Корпус сохранен: {hullData.Length} символов");
        
        // Очищаем корпус
        // hullComponent.ClearHull(); // Нужно добавить этот метод
        
        // Загружаем корпус обратно
        hullComponent.DeserializeHull(hullData);
        Debug.Log("[HullTest] Корпус загружен обратно");
    }
    
    private IEnumerator TestBuildModes()
    {
        if (hullBuilder == null) yield break;
        
        // Тестируем переключение режимов
        hullBuilder.SetBuildMode(HullBuilder.BuildMode.Wall);
        yield return new WaitForSeconds(0.5f);
        
        hullBuilder.SetBuildMode(HullBuilder.BuildMode.Door);
        yield return new WaitForSeconds(0.5f);
        
        hullBuilder.SetBuildMode(HullBuilder.BuildMode.Point);
        yield return new WaitForSeconds(0.5f);
        
        hullBuilder.SetBuildMode(HullBuilder.BuildMode.Wall);
    }
    
    // Event Handlers
    private void OnPointAdded(HullPoint point)
    {
        Debug.Log($"[HullTest] Событие: Добавлена точка {point.id} в позиции {point.position}");
    }
    
    private void OnWallAdded(HullWall wall)
    {
        Debug.Log($"[HullTest] Событие: Добавлена стена от точки {wall.startPointId} к точке {wall.endPointId}, длина: {wall.length}");
    }
    
    private void OnDoorAdded(HullDoor door)
    {
        Debug.Log($"[HullTest] Событие: Добавлена дверь между точками {door.startPointId} и {door.endPointId}");
    }
    
    private void OnBuildModeChanged(HullBuilder.BuildMode mode)
    {
        Debug.Log($"[HullTest] Событие: Режим строительства изменен на {mode}");
    }
    
    private void OnBuildingStateChanged(bool isActive)
    {
        Debug.Log($"[HullTest] Событие: Режим строительства {(isActive ? "включен" : "выключен")}");
    }
    
    // UI методы для ручного тестирования
    [ContextMenu("Test Switch to Build Mode")]
    public void TestSwitchToBuildMode()
    {
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
            Debug.Log("[HullTest] Переключение в режим строительства");
        }
    }
    
    [ContextMenu("Test Switch to Space Mode")]
    public void TestSwitchToSpaceMode()
    {
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_space);
            Debug.Log("[HullTest] Переключение в режим космоса");
        }
    }
    
    [ContextMenu("Test Create Simple Hull")]
    public void TestCreateSimpleHull()
    {
        CreateSimpleHull();
    }
    
    [ContextMenu("Test Save/Load")]
    public void TestSaveLoadManual()
    {
        TestSaveLoad();
    }
    
    [ContextMenu("Test Build Modes")]
    public void TestBuildModesManual()
    {
        StartCoroutine(TestBuildModes());
    }
    
    // Методы для отладки
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Hull Test Controls");
        
        if (GUILayout.Button("Switch to Build Mode"))
        {
            TestSwitchToBuildMode();
        }
        
        if (GUILayout.Button("Switch to Space Mode"))
        {
            TestSwitchToSpaceMode();
        }
        
        if (GUILayout.Button("Create Simple Hull"))
        {
            TestCreateSimpleHull();
        }
        
        if (GUILayout.Button("Test Save/Load"))
        {
            TestSaveLoadManual();
        }
        
        if (GUILayout.Button("Test Build Modes"))
        {
            TestBuildModesManual();
        }
        
        // Показываем текущее состояние
        if (hullBuilder != null)
        {
            GUILayout.Label($"Building Active: {hullBuilder.IsBuildingActive}");
            GUILayout.Label($"Build Mode: {hullBuilder.CurrentBuildMode}");
        }
        
        GUILayout.EndArea();
    }
} 