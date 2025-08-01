using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using JetBrains.Annotations;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class matrix : MonoBehaviour
{
    
#region Variables
    public static matrix instance;
    public float timer_to_show_grid_cells = 3;
    [SerializeField] public int size = 25; 
    [SerializeField] public TextAsset target_hull; // Файл сохранения для ручной загрузки 
    // Трёхмерный массив для хранения клеток по индексам
    [SerializeField] public io_base[,,] cells; 
    private const float GRID_STEP = 0.5f; // Шаг сетки для основных клеток
    private const float WALL_GRID_STEP = 0.5f; // Шаг сетки для стен и дверей
    private const int MATRIX_SIZE = 25; // Размер матрицы
    
    // Переменные для анимации
    private float max_distance_from_center = 0f;
    [SerializeField] public bool useCircularGrid = true;
    [SerializeField] public float circleRadius = 5f;
    [SerializeField] public int grid_size = 25;
    [SerializeField] public float grid_step = 0.5f;
    public int center_floor => MATRIX_SIZE/2;
    
    // Список уже активированных этажей
    private List<int> ActivatedFloors = new List<int>();
    
    // Для отслеживания изменений в редакторе
    private int lastEditorHash = 0; 
    float local_timer = 0;
    
    // Переменная для отображения дебага
    private bool showDebug = false;
    #endregion

#region AWAKE_UPDATE_START
    void Awake()
    {
        instance = this;
        create_parents_byType();
        gameObject.AddComponent<io_hull>();
        cells = new io_base[MATRIX_SIZE, MATRIX_SIZE, MATRIX_SIZE];
        io_system.instance.current_floor= center_floor;
        createFloor(io_system.instance.current_floor);
        
        // Инициализация дебага из PlayerPrefs
        showDebug = PlayerPrefs.GetInt("Debug", 0) == 1;
        
        return;
        // Сначала проверяем ручной файл из инспектора
        if (target_hull != null && !string.IsNullOrEmpty(target_hull.text))
        {
            try
            {
                HullData hullData = JsonUtility.FromJson<HullData>(target_hull.text);
                if (hullData != null && hullData.cells != null && hullData.cells.Count > 0)
                {
                    io_hull.instance.LoadHullDataToMatrix(hullData, this);
                    Debug.Log("matrix: Загружены данные из ручного файла (target_hull)");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"matrix: Ошибка загрузки ручного файла: {e.Message}");
            }
        }
        void Update()
    {
        // Обработка клавиши F5 для сохранения в target_hull
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveToTargetHullFile();
        }
        
        // Обработка клавиши F1 для переключения дебага
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebug = !showDebug;
            PlayerPrefs.SetInt("Debug", showDebug ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"matrix: Debug mode {(showDebug ? "enabled" : "disabled")}");
        }
        
        // Обновляем таймер для анимации активации этажа
        local_timer += Time.deltaTime;

         
    }
        // Если ручного файла нет или загрузка не удалась - проверяем автозагрузочный файл
        if (io_hull.instance.LoadHullFromFileToMatrix(this))
        {
            Debug.Log("matrix: Загружены данные из автозагрузочного файла");
            return;
        }
        
       
        
        // Финальный сброс состояний для всех клеток 
    }
    #endregion

    #region functions
    private void deactivateFloors()
    {
        foreach (int floor in ActivatedFloors)
        {
            if (floor == io_system.instance.current_floor) continue;

            // Проверяем, что этаж находится в пределах матрицы
            int floorIndex = floor;
            if (floorIndex < 0 || floorIndex >= MATRIX_SIZE) continue;

            for (int x = 0; x < MATRIX_SIZE; x += 2)
            {
                // Проверяем, что клетка существует и не скрыта
                if (cells[x, floorIndex, 0] == null ||
                    cells[x, floorIndex, 0].stack.Count == 0 ||
                    cells[x, floorIndex, 0].stack.Last() == io_base.io_type.hidden) continue;

                for (int z = 0; z < MATRIX_SIZE; z += 2)
                {
                    if (cells[x, floorIndex, z] != null)
                    {
                        cells[x, floorIndex, z].stack.Add(io_base.io_type.hidden);
                        cells[x, floorIndex, z].target_collider.enabled = false;
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        // Отображаем дебаг только если включен
        //if (!showDebug) return;

        // Увеличиваем размер шрифта для лучшей читаемости
        int fontSize = 14;

        // Создаем стили с явным указанием цветов
        GUIStyle headerStyle = new GUIStyle();
        headerStyle.fontSize = fontSize + 12;
        headerStyle.normal.textColor = Color.yellow;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.UpperLeft;

        GUIStyle normalStyle = new GUIStyle();
        normalStyle.fontSize = fontSize;
        normalStyle.normal.textColor = Color.white;
        normalStyle.fontStyle = FontStyle.Bold;
        normalStyle.alignment = TextAnchor.UpperLeft;

        GUIStyle statsStyle = new GUIStyle();
        statsStyle.fontSize = fontSize;
        statsStyle.normal.textColor = Color.cyan;
        statsStyle.fontStyle = FontStyle.Bold;
        statsStyle.alignment = TextAnchor.UpperLeft;

        // Позиционирование
        float x = 20;
        float y = 20;
        float lineHeight = fontSize + 10;

        // Заголовок
        GUI.Label(new Rect(x, y, 1800, lineHeight * 2.5f), "MATRIX DEBUG", headerStyle);
        y += lineHeight * 1.8f;

        // Текущий этаж
        GUI.Label(new Rect(x, y, 1800, lineHeight * 4f), "FLOOR: " + io_system.instance.current_floor, headerStyle);
        y += lineHeight * 2.5f;

        // Подсчет всех типов клеток
        int hidden = 0, on = 0, clicked = 0, mouseOver = 0, off = 0, other = 0;

        // Отладочная информация
        string debugInfo = $"Активные этажи: {string.Join(", ", ActivatedFloors)}";
        GUI.Label(new Rect(x, y, 600, lineHeight), debugInfo, normalStyle);
        y += lineHeight;

        foreach (int floor in ActivatedFloors)
        {
            int floorIndex = floor;
            for (int i = 0; i < MATRIX_SIZE; i += 2)
            {
                for (int j = 0; j < MATRIX_SIZE; j += 2)
                {
                    if (cells[i, floorIndex, j] != null && cells[i, floorIndex, j].stack.Count > 0)
                    {
                        var lastState = cells[i, floorIndex, j].stack.Last();
                        switch (lastState)
                        {
                            case io_base.io_type.hidden: hidden++; break;
                            case io_base.io_type.on: on++; break;
                            case io_base.io_type.clicked: clicked++; break;
                            case io_base.io_type.mouseOver: mouseOver++; break;
                            case io_base.io_type.off: off++; break;
                            default: other++; break;
                        }
                    }
                }
            }
        }

        // Статистика в две колонки
        float colWidth = 300;
        float statHeight = lineHeight * 1.2f;

        // Первая строка
        GUI.Label(new Rect(x, y, colWidth, statHeight), "ON: " + on, statsStyle);
        GUI.Label(new Rect(x + colWidth - 20, y, colWidth, statHeight), "HIDDEN: " + hidden, statsStyle);
        y += statHeight + 10;

        // Вторая строка
        GUI.Label(new Rect(x, y, colWidth, statHeight), "CLICKED: " + clicked, statsStyle);
        GUI.Label(new Rect(x + colWidth - 20, y, colWidth*2, statHeight), "MOUSE: " + mouseOver, statsStyle);
        y += statHeight + 10;

        // Третья строка
        GUI.Label(new Rect(x, y, colWidth, statHeight), "OFF: " + off, statsStyle);
        GUI.Label(new Rect(x + colWidth - 20, y, colWidth, statHeight), "OTHER: " + other, statsStyle);
        y += lineHeight + 20;

        // Информация о матрице
        GUI.Label(new Rect((Screen.width / 2) - 100, 100, 600, 3 * lineHeight),
                 "Floors: " + ActivatedFloors.Count + " | Matrix: " + MATRIX_SIZE + "³ | Center: " + center_floor, normalStyle);
             
        
        // Дополнительная проверка на null для каждого элемента
        int cellCount = cells.Cast<io_base>().Count(io => io != null && io.cell_type == io_base.io_base_cell_type.cell);
        int stairCount = cells.Cast<io_base>().Count(io => io != null && io.cell_type == io_base.io_base_cell_type.stair);
        int spaceCount = cells.Cast<io_base>().Count(io => io != null && io.cell_type == io_base.io_base_cell_type.space);
        int totalCount = cells.Cast<io_base>().Count(io => io != null);
        
        string stats = $"Всего: {totalCount} (Пол: {cellCount}, Лестница: {stairCount}, Космос: {spaceCount})";
        
        //GUI.Label(new Rect(10, 10, 1200, 100), "СТАТИСТИКА КЛЕТОК:", headerStyle);
        GUI.Label(new Rect(10, 80, 1200, 100), stats, normalStyle);
    }

    public void createFloor(int floor)
    {
        // Проверяем, что этаж находится в пределах матрицы
        int floorIndex = floor;
        if (floorIndex < 0 || floorIndex >= MATRIX_SIZE)
        {
            Debug.LogWarning($"matrix: Попытка создать этаж {floor} за пределами матрицы (индекс: {floorIndex})");
            return;
        }

        if (ActivatedFloors.Contains(floor))
        {
            for (int x = 0; x < MATRIX_SIZE; x += 2)
            {
                for (int z = 0; z < MATRIX_SIZE; z += 2)
                {
                    if (cells[x, floorIndex, z] != null)
                    {
                        float duration = cells[x, floorIndex, z].transform.position.magnitude / size;
                        cells[x, floorIndex, z].StartCoroutine(cells[x, floorIndex, z].ActivateCell(duration));
                        cells[x, floorIndex, z].target_collider.enabled = true;
                    }
                }
            }
            deactivateFloors();
            return;
        }

        for (int x = 0; x < MATRIX_SIZE; x += 2)
        {
            for (int z = 0; z < MATRIX_SIZE; z += 2)
            {
                cells[x, floorIndex, z] = CreateGridCell(x, floorIndex, z, io_base.io_base_cell_type.space);
                if (cells[x, floorIndex, z] != null)
                {
                    float duration = cells[x, floorIndex, z].transform.position.magnitude / size;
                    cells[x, floorIndex, z].StartCoroutine(cells[x, floorIndex, z].ActivateCell(duration));
                }
            }
        }
        ActivatedFloors.Add(floor);
        deactivateFloors();
    }
    public List<Transform> parents_by_type;
    public void create_parents_byType()
    {
        List<GameObject> parents_by_type_go = new List<GameObject>();
        for (int i = 0; i < 12; i++)
        {
            parents_by_type_go.Add(new GameObject("parent_by_type_" + i));            
            parents_by_type_go[i].transform.parent = null;
        }
        for (int i = 0; i < 12; i++)
        {
            parents_by_type.Add(parents_by_type_go[i].transform);
          
        }
    }

    public Vector3Int WorldToGridIndex(Vector3 worldPosition)
    {
        // Преобразуем мировые координаты в индексы матрицы
        // Центр матрицы соответствует позиции (0,0,0) в мире
        int x = Mathf.RoundToInt(worldPosition.x / GRID_STEP) + MATRIX_SIZE / 2;
        int y = Mathf.RoundToInt(worldPosition.y / GRID_STEP) + MATRIX_SIZE / 2;
        int z = Mathf.RoundToInt(worldPosition.z / GRID_STEP) + MATRIX_SIZE / 2;

        // Ограничиваем индексы в пределах матрицы
        x = Mathf.Clamp(x, 0, MATRIX_SIZE - 1);
        y = Mathf.Clamp(y, 0, MATRIX_SIZE - 1);
        z = Mathf.Clamp(z, 0, MATRIX_SIZE - 1);

        return new Vector3Int(x, y, z);
    }
    
    // Перегрузка для клеток с учетом их типа
    public Vector3Int WorldToGridIndex(Vector3 worldPosition, io_base cell)
    {
        float gridStep = GetGridStepForCell(cell);
        
        // Преобразуем мировые координаты в индексы матрицы
        int x = Mathf.RoundToInt(worldPosition.x / gridStep) + MATRIX_SIZE / 2;
        int y = Mathf.RoundToInt(worldPosition.y / gridStep) + MATRIX_SIZE / 2;
        int z = Mathf.RoundToInt(worldPosition.z / gridStep) + MATRIX_SIZE / 2;
        
        // Ограничиваем индексы в пределах матрицы
        x = Mathf.Clamp(x, 0, MATRIX_SIZE - 1);
        y = Mathf.Clamp(y, 0, MATRIX_SIZE - 1);
        z = Mathf.Clamp(z, 0, MATRIX_SIZE - 1);
        
        return new Vector3Int(x, y, z);
    }
    
    public Vector3 GridIndexToWorld(Vector3Int gridIndex)
    {
        // Преобразуем индексы матрицы в мировые координаты
        // Центр матрицы (12,12,12) соответствует мировой позиции (0,0,0)
        float x = (gridIndex.x - MATRIX_SIZE / 2) * GRID_STEP;
        float y = (gridIndex.y - MATRIX_SIZE / 2) * GRID_STEP;
        float z = (gridIndex.z - MATRIX_SIZE / 2) * GRID_STEP;
        
        return new Vector3(x, y, z);
    }
    
    // Метод для получения координат клетки по индексу с учетом типа клетки
    public Vector3 GetCellPositionByIndex(Vector3Int gridIndex, io_base cell = null)
    {
        float gridStep = cell != null ? GetGridStepForCell(cell) : GRID_STEP;
        
        // Преобразуем индексы матрицы в мировые координаты
        float x = (gridIndex.x - MATRIX_SIZE / 2) * gridStep;
        float y = (gridIndex.y - MATRIX_SIZE / 2) * gridStep;
        float z = (gridIndex.z - MATRIX_SIZE / 2) * gridStep;
        
        return new Vector3(x, y, z);
    }
    
    // Метод для получения центрального индекса матрицы
    
  
    
    
    
    public void SetCellAtGridIndex(Vector3Int gridIndex, io_base cell)
    {
        if (IsValidGridIndex(gridIndex))
        {
            cells[gridIndex.x, gridIndex.y, gridIndex.z] = cell;
        }
    }
    
    public io_base GetCellAtGridIndex(Vector3Int gridIndex)
    {
        if (IsValidGridIndex(gridIndex))
        {
            return cells[gridIndex.x, gridIndex.y, gridIndex.z];
        }
        return null;
    }
    
     
    
    // Методы для определения типа клетки и соответствующего шага сетки
    private bool IsWallType(io_base cell)
    {
        if (cell == null) return false;
        return cell.cell_type == io_base.io_base_cell_type.wall || 
               cell.cell_type == io_base.io_base_cell_type.door;
    }
    
    public float GetGridStepForCell(io_base cell)
    {
        if (IsWallType(cell))
        {
            return WALL_GRID_STEP; // Стены и двери могут занимать любые позиции
        }
        else
        {
            return GRID_STEP; // Основные клетки используют стандартный шаг
        }
    }
    
    private float GetGridStepForPosition(Vector3 worldPosition)
    {
        // По умолчанию используем стандартный шаг
        // Для точного определения нужно проверить, есть ли уже клетка в этой позиции
        return GRID_STEP;
    }
    
    private bool IsValidGridIndex(Vector3Int gridIndex)
    {
        return gridIndex.x >= 0 && gridIndex.x < MATRIX_SIZE &&
               gridIndex.y >= 0 && gridIndex.y < MATRIX_SIZE &&
               gridIndex.z >= 0 && gridIndex.z < MATRIX_SIZE;
    }
     
    
 
    public void SaveToTargetHullFile()
    {
        io_hull.instance.SaveHullToFile();
    }
    
    // Методы для работы с клетками
    
 
    
    
    
    public void ClearMatrix()
    {
        if (cells != null)
        {
            for (int x = 0; x < MATRIX_SIZE; x++)
            {
                for (int y = 0; y < MATRIX_SIZE; y++)
                {
                    for (int z = 0; z < MATRIX_SIZE; z++)
                    {
                        cells[x, y, z] = null;
                    }
                }
            }
        }
    }
    

    
   
    
      
    
    // Метод для проверки, активирован ли этаж
  
    
    // Метод для получения списка активированных этажей 
    
    // Метод для очистки списка активированных этажей
    
    
    // Метод для получения информации об активированных этажах
    
    
 
     
    
    // Метод для получения всех клеток на указанном этаже
    public List<io_base> GetFloorCells(int floor)
    {
        List<io_base> floorCells = new List<io_base>();
        
        if (cells == null) return floorCells;
        
        for (int x = 0; x < MATRIX_SIZE; x++)
        {
            for (int z = 0; z < MATRIX_SIZE; z++)
            {
                if (cells[x, floor, z] != null)
                {
                    floorCells.Add(cells[x, floor, z]);
                }
            }
        }
        
        return floorCells;
    }
    
    // Метод для получения выделенных клеток на указанном этаже
    public List<io_base> GetSelectedCellsOnFloor(int floor)
    {
        List<io_base> selectedCells = new List<io_base>();
        
        if (cells == null) return selectedCells;
        
        for (int x = 0; x < MATRIX_SIZE; x++)
        {
            for (int z = 0; z < MATRIX_SIZE; z++)
            {
                if (cells[x, floor, z] != null && 
                    cells[x, floor, z].stack.Contains(io_base.io_type.clicked))
                {
                    selectedCells.Add(cells[x, floor, z]);
                }
            }
        }
        
        return selectedCells;
    }
    
    // Метод для проверки, занята ли позиция клеткой
    public io_base GetCellAtPosition(Vector3 worldPosition)
    {
        if (cells == null) return null;
        
        // Преобразуем мировые координаты в индексы сетки
        Vector3Int gridIndex = WorldToGridIndex(worldPosition);
        
        // Проверяем, что индексы в пределах матрицы
        if (!IsValidGridIndex(gridIndex))
        {
            return null;
        }
        
        // Возвращаем клетку по индексам (если есть)
        return cells[gridIndex.x, gridIndex.y, gridIndex.z];
    }
    
    // Метод для добавления клетки в матрицу
    public void AddCell(io_base cell)
    {
        if (cell == null || cells == null) return;
        
        // Получаем индексы сетки из позиции клетки
        Vector3Int gridIndex = WorldToGridIndex(cell.transform.position);
        
        // Проверяем, что индексы в пределах матрицы
        if (!IsValidGridIndex(gridIndex))
        {
            Debug.LogWarning($"matrix: Попытка добавить клетку за пределами матрицы: {cell.transform.position}");
            return;
        }
        
        // Проверяем, не занята ли уже эта позиция
        if (cells[gridIndex.x, gridIndex.y, gridIndex.z] != null)
        {
            Debug.LogWarning($"matrix: Позиция уже занята: {cell.transform.position}");
            return;
        }
        
        // Добавляем клетку в матрицу
        cells[gridIndex.x, gridIndex.y, gridIndex.z] = cell;
        
        // Обновляем позицию клетки в сетке
        cell.SetGridPosition(cell.transform.position);
        cell.cell_position_in_grid = cell.transform.position;
        cell.floor = gridIndex.y;
        
        Debug.Log($"matrix: Клетка добавлена в позицию {gridIndex} (мировая позиция: {cell.transform.position})");
    }
    
    private io_base CreateGridCell(int x, int y, int z, io_base.io_base_cell_type celltype)
    {
        // Находим префаб клетки
        io_base cellPrefab = io_system.instance.cells_prefabs.Find(x => x.cell_type == celltype);
        if (cellPrefab == null)
        {
            Debug.LogWarning("matrix: Не найден префаб клетки");
            return null;
        }
        
        // Создаем клетку
        io_base cell = Instantiate(cellPrefab, new Vector3(x, y, z), Quaternion.identity);
        cell.floor = io_system.instance.current_floor;
        cell.Init(transform);       
        cell.AddStack(io_base.io_type.hidden);
        cell.target_collider.enabled = false;
        return cell;
    }
    
   
    #endregion
}


