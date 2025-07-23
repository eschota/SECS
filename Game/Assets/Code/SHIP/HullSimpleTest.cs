using UnityEngine;

public class HullSimpleTest : MonoBehaviour
{
    [Header("Simple Test")]
    [SerializeField] private bool runOnStart = true;
    
    void Start()
    {
        if (runOnStart)
        {
            Invoke("RunSimpleTest", 0.5f);
        }
    }
    
    void RunSimpleTest()
    {
        Debug.Log("=== HULL SYSTEM SIMPLE TEST ===");
        
        // Тест 1: Проверяем создание базовых классов
        TestBasicClasses();
        
        // Тест 2: Проверяем создание компонентов
        TestComponents();
        
        // Тест 3: Проверяем сериализацию
        TestBasicSerialization();
        
        Debug.Log("=== SIMPLE TEST COMPLETED ===");
    }
    
    void TestBasicClasses()
    {
        Debug.Log("Тест 1: Базовые классы");
        
        try
        {
            // Создаем простые объекты
            HullPoint point = new HullPoint(Vector3.zero, 0);
            HullWall wall = new HullWall(0, 1, Vector3.zero, Vector3.one);
            HullDoor door = new HullDoor(0, 1, Vector3.zero, Quaternion.identity);
            
            Debug.Log($"✓ HullPoint: ID={point.id}, Pos={point.position}");
            Debug.Log($"✓ HullWall: Length={wall.length}, Start={wall.startPointId}, End={wall.endPointId}");
            Debug.Log($"✓ HullDoor: Start={door.startPointId}, End={door.endPointId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка базовых классов: {e.Message}");
        }
    }
    
    void TestComponents()
    {
        Debug.Log("Тест 2: Компоненты");
        
        try
        {
            // Создаем тестовый объект
            GameObject testObject = new GameObject("SimpleTest");
            
            // Добавляем компоненты
            HULL hull = testObject.AddComponent<HULL>();
            HullBuilder builder = testObject.AddComponent<HullBuilder>();
            HullNode node = testObject.AddComponent<HullNode>();
            
            Debug.Log($"✓ HULL: {hull != null}");
            Debug.Log($"✓ HullBuilder: {builder != null}");
            Debug.Log($"✓ HullNode: {node != null}");
            
            // Очищаем
            DestroyImmediate(testObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка компонентов: {e.Message}");
        }
    }
    
    void TestBasicSerialization()
    {
        Debug.Log("Тест 3: Базовая сериализация");
        
        try
        {
            // Создаем тестовые данные
            HullPoint point = new HullPoint(new Vector3(1, 2, 3), 42);
            
            // Сериализуем
            string json = JsonUtility.ToJson(point);
            Debug.Log($"✓ JSON: {json}");
            
            // Десериализуем
            HullPoint point2 = JsonUtility.FromJson<HullPoint>(json);
            Debug.Log($"✓ Десериализация: ID={point2.id}, Pos={point2.position}");
            
            // Проверяем
            if (point.id == point2.id && point.position == point2.position)
            {
                Debug.Log("✓ Сериализация работает корректно");
            }
            else
            {
                Debug.LogWarning("⚠ Сериализация работает, но данные не совпадают");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Ошибка сериализации: {e.Message}");
        }
    }
    
    // UI для ручного запуска
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        
        GUILayout.Label("Hull Simple Test", GUI.skin.box);
        
        if (GUILayout.Button("Run Simple Test"))
        {
            RunSimpleTest();
        }
        
        GUILayout.EndArea();
    }
} 