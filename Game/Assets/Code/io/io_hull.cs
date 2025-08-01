using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class HullCellData
{
    public int x;
    public int z;
    public int floor;
    public io_base.io_base_cell_type cellType;
    public io_base.io_type currentState;
    public int direction; // Направление поворота: 0=север, 1=восток, 2=юг, 3=запад
    
    public HullCellData(int x, int z, int floor, io_base.io_base_cell_type cellType, int direction = 0)
    {
        this.x = x;
        this.z = z;
        this.floor = floor;
        this.cellType = cellType;
        this.direction = direction;
    }
}

[System.Serializable]
public class HullData
{ 
   
    // Основная информация о корпусе
    public string hull_name = "default hull 01";
    public HullType hull_type = HullType.default_type;
    public string user_id = "0";
    
    // Динамический подсчет клеток по типам
    public Dictionary<string, int> cells_type_count = new Dictionary<string, int>();
    
    // Данные клеток
    public List<HullCellData> cells = new List<HullCellData>();
    public int gridSize;
    public string saveDate;
    
    public HullData(int gridSize)
    {
        this.gridSize = gridSize;
        this.saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// Enum для типов корпуса
public enum HullType
{
    default_type,
    module_captain,
    module,
    module_gate,
    module_engine,
    module_shield,
    module_weapon
}

public class io_hull : MonoBehaviour
{   public static io_hull instance;
    private const string HULL_SAVE_FILE = "last_hull.json";
    private const string HULL_SAVE_PATH = "HullSaves";
    private string saveFilePath => Path.Combine(Application.persistentDataPath, HULL_SAVE_PATH, HULL_SAVE_FILE);
    [Header("Настройки сохранения")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private bool debugMode = false;
     
    private int lastHash = 0;
    
    void Awake()
    {
        instance = this;
        InitializeSavePath();
        // Загрузка теперь происходит в grid_cells.Awake()
        // StartCoroutine(LoadHullWithDelay()); // Убираем эту строку
    }
    
    void Start()
    {
        // Подписываемся на события изменения клеток
        if (io_system.instance != null)
        {
            StartCoroutine(MonitorCellChanges());
        }
    }
    
    private void InitializeSavePath()
    {
        string persistentDataPath = Application.persistentDataPath;
        string hullSavesPath = Path.Combine(persistentDataPath, HULL_SAVE_PATH);
        
        if (!Directory.Exists(hullSavesPath))
        {
            Directory.CreateDirectory(hullSavesPath);
        }
        
     
        
        if (debugMode)
        {
            Debug.Log($"io_hull: Путь сохранения: {saveFilePath}");
        }
    }
    
    float timerThreshold = 1.2f;
    private System.Collections.IEnumerator MonitorCellChanges()
    {
        while (true)
        {
            yield return new WaitForSeconds(timerThreshold); // Проверяем каждые 100мс

            if (autoSave && io_system.instance != null)
            {
                int currentHash = CalculateCellsHash();
                if (currentHash != lastHash)
                {
                    lastHash = currentHash;
                    SaveHullToFile();
                }
            }
        }
    }
    
    private int CalculateCellsHash()
    {
        if (io_system.instance == null)
        {
            if (debugMode)
                Debug.Log("io_hull: io_system.instance == null");
            return 0;
        }
        
        if (io_system.instance.io_list == null)
        {
            if (debugMode)
                Debug.Log("io_hull: io_system.instance.io_list == null");
            return 0;
        }
        
        int hash = 0;
        int validCells = 0;
        
        foreach (var cell in io_system.instance.io_list)
        {
            if (cell == null)
            {
                if (debugMode)
                    Debug.Log("io_hull: Найдена null клетка в списке");
                continue;
            }
            
            if (cell.stack == null || cell.stack.Count == 0)
            {
                if (debugMode)
                    Debug.Log($"io_hull: Клетка {cell.name} не имеет состояния");
                continue;
            }
            
            // Хешируем только типы и этажность
            hash = hash * 31 + (int)cell.cell_type;
            hash = hash * 31 + cell.floor;
            hash = hash * 31 + cell.direction; // Добавляем направление в хеш
            validCells++;
        }
        
        if (debugMode && validCells > 0)
        {
            Debug.Log($"io_hull: Хеш рассчитан для {validCells} клеток: {hash}");
        }
        
        return hash;
    }
    
    public void SaveHullToFile()
    {
        if (io_system.instance == null)
        {
            Debug.LogWarning("io_hull: Не удалось сохранить - io_system недоступен");
            return;
        }
        
        if (io_system.instance.io_list == null)
        {
            Debug.LogWarning("io_hull: Не удалось сохранить - список клеток пуст");
            return;
        }
        
        try
        {
            HullData hullData = new HullData(25); // Используем фиксированный размер матрицы
            
            int savedCells = 0;
           
            foreach (var cell in io_system.instance.io_list)
            {
                if (hullData.cells_type_count.ContainsKey(cell.cell_type.ToString()))
                {
                    hullData.cells_type_count[cell.cell_type.ToString()]++;
                }
                else
                {
                    hullData.cells_type_count.Add(cell.cell_type.ToString(), 1);
                }

                if (cell == null)
                {
                    Debug.LogWarning("io_hull: Найдена null клетка в списке, пропускаем");
                    continue;
                }

                if (cell.stack == null || cell.stack.Count == 0)
                {
                    Debug.LogWarning($"io_hull: Клетка {cell.name} не имеет состояния, пропускаем");
                    continue;
                }

                // Пропускаем клетки помеченные на удаление
                if (cell.stack.Last() == io_base.io_type.ToRemove)
                    continue;


                Vector3 position = cell.transform.position;
                int x = Mathf.RoundToInt(position.x);
                int z = Mathf.RoundToInt(position.z);

                HullCellData cellData = new HullCellData(x, z, cell.floor, cell.cell_type, cell.direction);

                hullData.cells.Add(cellData);
                savedCells++;
            }
            
            if (savedCells == 0)
            {
                Debug.LogWarning("io_hull: Нет клеток для сохранения");
                return;
            }
            
            string json = JsonUtility.ToJson(hullData, true);
            File.WriteAllText(saveFilePath, json);
            
            if (debugMode)
            {
                Debug.Log($"io_hull: Сохранено {savedCells} клеток в {saveFilePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"io_hull: Ошибка сохранения: {e.Message}");
        }
    }
    
    public void LoadHullFromFile()
    {
     
    }
    
    // Методы для работы с matrix
    public void LoadHullDataToMatrix(HullData hullData, matrix matrixSystem)
    {
        if (hullData == null || hullData.cells == null)
        {
            Debug.LogWarning("io_hull: HullData пуст или null");
            return;
        }
        
        if (matrixSystem == null)
        {
            Debug.LogError("io_hull: matrixSystem равен null");
            return;
        }
        
        // Очищаем матрицу
        matrixSystem.ClearMatrix();
        
        // Загружаем клетки
        foreach (var cellData in hullData.cells)
        {
            // Находим префаб для типа клетки
            io_base prefabToUse = io_system.instance.cells_prefabs.Find(x => x.cell_type == cellData.cellType);
            if (prefabToUse == null)
            {
                Debug.LogWarning($"io_hull: Не найден префаб для типа {cellData.cellType}");
                continue;
            }
            
            // Вычисляем позицию с учетом шага сетки
            float gridStep = matrixSystem.GetGridStepForCell(prefabToUse);
            Vector3 position = new Vector3(cellData.x * gridStep, cellData.floor * gridStep, cellData.z * gridStep);
            
            // Создаем клетку
            io_base io = Instantiate(prefabToUse, position, Quaternion.identity);
            io.floor = cellData.floor;
            io.direction = cellData.direction;
            io.target_transform.localRotation = Quaternion.Euler(0, cellData.direction * 90, 0);
            io.Init(matrixSystem.transform);
            
            // Добавляем в матрицу
            matrixSystem.AddCell(io);
        }
        
        Debug.Log($"io_hull: Загружено {hullData.cells.Count} клеток в matrix");
    }
    
    public bool LoadHullFromFileToMatrix(matrix matrixSystem)
    {
        if (matrixSystem == null)
        {
            Debug.LogError("io_hull: matrixSystem равен null");
            return false;
        }
        
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("io_hull: Файл сохранения не найден");
            return false;
        }
        
        try
        {
            string json = File.ReadAllText(saveFilePath);
            HullData hullData = JsonUtility.FromJson<HullData>(json);
            if (hullData != null && hullData.cells != null && hullData.cells.Count > 0)
            {
                LoadHullDataToMatrix(hullData, matrixSystem);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"io_hull: Ошибка загрузки файла: {e.Message}");
        }
        
        return false;
    }
    
   
    
    [ContextMenu("Сохранить корпус")]
    public void SaveHull()
    {
        SaveHullToFile();
    }
    
    [ContextMenu("Загрузить корпус")]
    public void LoadHull()
    {
        LoadHullFromFile();
    }
    
    [ContextMenu("Удалить файл сохранения")]
    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"io_hull: Файл сохранения удален: {saveFilePath}");
        }
    }
}
