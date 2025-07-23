using UnityEngine;

public class HullQuickTest : MonoBehaviour
{
    [Header("Quick Test")]
    [SerializeField] private bool runTestOnStart = true;
    
    void Start()
    {
        if (runTestOnStart)
        {
            Invoke("RunQuickTest", 1f); // Запускаем тест через 1 секунду
        }
    }
    
    void RunQuickTest()
    {
        Debug.Log("=== HULL SYSTEM QUICK TEST ===");
        
        // Тест 1: Проверяем наличие компонентов
        TestComponents();
        
        // Тест 2: Переключаем в режим строительства
        TestStateSwitch();
        
        // Тест 3: Проверяем события
        TestEvents();
        
        Debug.Log("=== QUICK TEST COMPLETED ===");
    }
    
    void TestComponents()
    {
        Debug.Log("Тест 1: Проверка компонентов");
        
        // Проверяем SHIP_UI
        if (SHIP_UI.Instance != null)
        {
            Debug.Log("✓ SHIP_UI найден");
        }
        else
        {
            Debug.LogError("✗ SHIP_UI не найден!");
        }
        
        // Проверяем HULL
        HULL hull = FindObjectOfType<HULL>();
        if (hull != null)
        {
            Debug.Log("✓ HULL найден");
        }
        else
        {
            Debug.LogWarning("⚠ HULL не найден - создаем тестовый");
            CreateTestHull();
        }
        
        // Проверяем HullBuilder
        HullBuilder builder = FindObjectOfType<HullBuilder>();
        if (builder != null)
        {
            Debug.Log("✓ HullBuilder найден");
        }
        else
        {
            Debug.LogWarning("⚠ HullBuilder не найден - создаем тестовый");
            CreateTestBuilder();
        }
    }
    
    void TestStateSwitch()
    {
        Debug.Log("Тест 2: Переключение состояний");
        
        if (SHIP_UI.Instance != null)
        {
            // Переключаем в режим строительства
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
            Debug.Log("✓ Переключились в режим строительства");
            
            // Ждем немного и переключаем обратно
            Invoke("SwitchBackToSpace", 2f);
        }
        else
        {
            Debug.LogError("✗ Не удалось переключить состояние - SHIP_UI не найден");
        }
    }
    
    void SwitchBackToSpace()
    {
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_space);
            Debug.Log("✓ Переключились в режим космоса");
        }
    }
    
    void TestEvents()
    {
        Debug.Log("Тест 3: Проверка событий");
        
        // Подписываемся на события
        HULL.OnPointAdded += OnTestPointAdded;
        HULL.OnWallAdded += OnTestWallAdded;
        HULL.OnDoorAdded += OnTestDoorAdded;
        
        HullBuilder.OnBuildModeChanged += OnTestBuildModeChanged;
        HullBuilder.OnBuildingStateChanged += OnTestBuildingStateChanged;
        
        Debug.Log("✓ Подписались на события");
        
        // Отписываемся через 5 секунд
        Invoke("UnsubscribeFromEvents", 5f);
    }
    
    void CreateTestHull()
    {
        GameObject hullObject = new GameObject("TestHull");
        HULL hull = hullObject.AddComponent<HULL>();
        
        // Создаем простые префабы для тестирования
        CreateTestPrefabs();
        
        Debug.Log("✓ Создан тестовый HULL");
    }
    
    void CreateTestBuilder()
    {
        GameObject builderObject = new GameObject("TestHullBuilder");
        HullBuilder builder = builderObject.AddComponent<HullBuilder>();
        
        // Находим HULL и назначаем его
        HULL hull = FindObjectOfType<HULL>();
        if (hull != null)
        {
            builder.hullComponent = hull;
        }
        
        Debug.Log("✓ Создан тестовый HullBuilder");
    }
    
    void CreateTestPrefabs()
    {
        // Создаем простые префабы для тестирования
        GameObject pointPrefab = new GameObject("TestPointPrefab");
        pointPrefab.AddComponent<HullPointPrefab>();
        
        GameObject wallPrefab = new GameObject("TestWallPrefab");
        wallPrefab.AddComponent<HullWallPrefab>();
        
        GameObject doorPrefab = new GameObject("TestDoorPrefab");
        doorPrefab.AddComponent<HullDoorPrefab>();
        
        // Назначаем префабы в HULL
        HULL hull = FindObjectOfType<HULL>();
        if (hull != null)
        {
            // Используем reflection для назначения префабов
            var pointField = typeof(HULL).GetField("pointPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wallField = typeof(HULL).GetField("wallPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var doorField = typeof(HULL).GetField("doorPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (pointField != null) pointField.SetValue(hull, pointPrefab);
            if (wallField != null) wallField.SetValue(hull, wallPrefab);
            if (doorField != null) doorField.SetValue(hull, doorPrefab);
        }
        
        Debug.Log("✓ Созданы тестовые префабы");
    }
    
    void UnsubscribeFromEvents()
    {
        HULL.OnPointAdded -= OnTestPointAdded;
        HULL.OnWallAdded -= OnTestWallAdded;
        HULL.OnDoorAdded -= OnTestDoorAdded;
        
        HullBuilder.OnBuildModeChanged -= OnTestBuildModeChanged;
        HullBuilder.OnBuildingStateChanged -= OnTestBuildingStateChanged;
        
        Debug.Log("✓ Отписались от событий");
    }
    
    // Event handlers для тестирования
    void OnTestPointAdded(HullPoint point)
    {
        Debug.Log($"🎯 Тестовое событие: Добавлена точка {point.id} в {point.position}");
    }
    
    void OnTestWallAdded(HullWall wall)
    {
        Debug.Log($"🧱 Тестовое событие: Добавлена стена {wall.length}m от {wall.startPointId} к {wall.endPointId}");
    }
    
    void OnTestDoorAdded(HullDoor door)
    {
        Debug.Log($"🚪 Тестовое событие: Добавлена дверь между {door.startPointId} и {door.endPointId}");
    }
    
    void OnTestBuildModeChanged(HullBuilder.BuildMode mode)
    {
        Debug.Log($"🔧 Тестовое событие: Режим строительства изменен на {mode}");
    }
    
    void OnTestBuildingStateChanged(bool isActive)
    {
        Debug.Log($"⚡ Тестовое событие: Строительство {(isActive ? "включено" : "выключено")}");
    }
    
    // UI для ручного тестирования
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 300));
        GUILayout.Label("Hull Quick Test", GUI.skin.box);
        
        if (GUILayout.Button("Run Quick Test"))
        {
            RunQuickTest();
        }
        
        if (GUILayout.Button("Switch to Build Mode"))
        {
            if (SHIP_UI.Instance != null)
            {
                SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
            }
        }
        
        if (GUILayout.Button("Switch to Space Mode"))
        {
            if (SHIP_UI.Instance != null)
            {
                SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_space);
            }
        }
        
        if (GUILayout.Button("Create Test Hull"))
        {
            CreateTestHull();
        }
        
        if (GUILayout.Button("Create Test Builder"))
        {
            CreateTestBuilder();
        }
        
        // Показываем статус
        GUILayout.Label("Status:", GUI.skin.box);
        GUILayout.Label($"SHIP_UI: {(SHIP_UI.Instance != null ? "✓" : "✗")}");
        GUILayout.Label($"HULL: {(FindObjectOfType<HULL>() != null ? "✓" : "✗")}");
        GUILayout.Label($"HullBuilder: {(FindObjectOfType<HullBuilder>() != null ? "✓" : "✗")}");
        
        GUILayout.EndArea();
    }
} 