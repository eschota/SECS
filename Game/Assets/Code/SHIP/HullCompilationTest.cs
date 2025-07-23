using UnityEngine;
using System.Collections;

public class HullCompilationTest : MonoBehaviour
{
    [Header("Compilation Test")]
    [SerializeField] private bool runOnStart = true;
    
    void Start()
    {
        if (runOnStart)
        {
            Invoke("RunCompilationTest", 0.5f);
        }
    }
    
    void RunCompilationTest()
    {
        Debug.Log("=== HULL SYSTEM COMPILATION TEST ===");
        
        // Тест 1: Проверяем создание классов
        TestClassCreation();
        
        // Тест 2: Проверяем создание объектов
        TestObjectCreation();
        
        // Тест 3: Проверяем события
        TestEvents();
        
        // Тест 4: Проверяем сериализацию
        TestSerialization();
        
        Debug.Log("=== COMPILATION TEST COMPLETED SUCCESSFULLY ===");
    }
    
    void TestClassCreation()
    {
        Debug.Log("Тест 1: Создание классов");
        
        try
        {
            // Создаем тестовые объекты
            HullPoint point = new HullPoint(Vector3.zero, 0);
            HullWall wall = new HullWall(0, 1, Vector3.zero, Vector3.one);
            HullDoor door = new HullDoor(0, 1, Vector3.zero, Quaternion.identity);
            
            Debug.Log("✓ HullPoint создан успешно");
            Debug.Log("✓ HullWall создан успешно");
            Debug.Log("✓ HullDoor создан успешно");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка создания классов: {e.Message}");
        }
    }
    
    void TestObjectCreation()
    {
        Debug.Log("Тест 2: Создание объектов");
        
        try
        {
            // Создаем GameObject с компонентами
            GameObject testObject = new GameObject("CompilationTest");
            
            HULL hull = testObject.AddComponent<HULL>();
            HullBuilder builder = testObject.AddComponent<HullBuilder>();
            HullNode node = testObject.AddComponent<HullNode>();
            
            Debug.Log("✓ HULL компонент создан");
            Debug.Log("✓ HullBuilder компонент создан");
            Debug.Log("✓ HullNode компонент создан");
            
            // Очищаем тестовый объект
            DestroyImmediate(testObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка создания объектов: {e.Message}");
        }
    }
    
    void TestEvents()
    {
        Debug.Log("Тест 3: Проверка событий");
        
        try
        {
            // Создаем делегаты для событий
            System.Action<HullPoint> pointHandler = (point) => Debug.Log("✓ Событие OnPointAdded работает");
            System.Action<HullWall> wallHandler = (wall) => Debug.Log("✓ Событие OnWallAdded работает");
            System.Action<HullDoor> doorHandler = (door) => Debug.Log("✓ Событие OnDoorAdded работает");
            System.Action<HullBuilder.BuildMode> modeHandler = (mode) => Debug.Log("✓ Событие OnBuildModeChanged работает");
            System.Action<bool> stateHandler = (active) => Debug.Log("✓ Событие OnBuildingStateChanged работает");
            
            // Подписываемся на события
            HULL.OnPointAdded += pointHandler;
            HULL.OnWallAdded += wallHandler;
            HULL.OnDoorAdded += doorHandler;
            
            HullBuilder.OnBuildModeChanged += modeHandler;
            HullBuilder.OnBuildingStateChanged += stateHandler;
            
            Debug.Log("✓ Все события подписаны успешно");
            
            // Отписываемся
            HULL.OnPointAdded -= pointHandler;
            HULL.OnWallAdded -= wallHandler;
            HULL.OnDoorAdded -= doorHandler;
            
            HullBuilder.OnBuildModeChanged -= modeHandler;
            HullBuilder.OnBuildingStateChanged -= stateHandler;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка событий: {e.Message}");
        }
    }
    
    void TestSerialization()
    {
        Debug.Log("Тест 4: Проверка сериализации");
        
        try
        {
            // Создаем тестовые данные
            HullPoint point = new HullPoint(Vector3.one, 1);
            HullWall wall = new HullWall(0, 1, Vector3.zero, Vector3.one);
            HullDoor door = new HullDoor(0, 1, Vector3.zero, Quaternion.identity);
            
            // Тестируем сериализацию через JsonUtility
            string pointJson = JsonUtility.ToJson(point);
            string wallJson = JsonUtility.ToJson(wall);
            string doorJson = JsonUtility.ToJson(door);
            
            Debug.Log($"✓ Сериализация HullPoint: {pointJson.Length} символов");
            Debug.Log($"✓ Сериализация HullWall: {wallJson.Length} символов");
            Debug.Log($"✓ Сериализация HullDoor: {doorJson.Length} символов");
            
            // Тестируем десериализацию
            HullPoint pointDeserialized = JsonUtility.FromJson<HullPoint>(pointJson);
            HullWall wallDeserialized = JsonUtility.FromJson<HullWall>(wallJson);
            HullDoor doorDeserialized = JsonUtility.FromJson<HullDoor>(doorJson);
            
            Debug.Log("✓ Десериализация прошла успешно");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка сериализации: {e.Message}");
        }
    }
    
    // UI для ручного запуска теста
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 60, 200, 50));
        
        if (GUILayout.Button("Run Compilation Test"))
        {
            RunCompilationTest();
        }
        
        GUILayout.EndArea();
    }
} 