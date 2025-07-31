using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class grid_cells : MonoBehaviour
{
    public float timer_to_show_grid_cells = 3;
    [SerializeField] public int grid_size; 
    [SerializeField] public TextAsset target_hull; // Файл сохранения для ручной загрузки

    [Header("Настройки сетки")]
    [SerializeField] private bool useCircularGrid = true; // Использовать круговую сетку вместо квадратной
    [SerializeField] private float circleRadius = 5f; // Радиус окружности для круговой сетки

    [SerializeField]List<io_base> grid_cells_list;
    
    // Для отслеживания изменений в редакторе
    private int lastEditorHash = 0;

float max_distance_from_center;
    float local_timer = 0;
    
    void Awake()
    {
        // Инициализируем список если он null
        if (grid_cells_list == null)
        {
            grid_cells_list = new List<io_base>();
        }
        
        // Сначала проверяем ручной файл из инспектора
        if (target_hull != null && !string.IsNullOrEmpty(target_hull.text))
        {
            try
            {
                HullData hullData = JsonUtility.FromJson<HullData>(target_hull.text);
                if (hullData != null && hullData.cells != null && hullData.cells.Count > 0)
                {
                    LoadHullData(hullData);
                    Debug.Log("grid_cells: Загружены данные из ручного файла (target_hull)");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"grid_cells: Ошибка загрузки ручного файла: {e.Message}");
            }
        }
        
        // Если ручного файла нет или загрузка не удалась - проверяем автозагрузочный файл
        string savePath = GetHullSavePath();
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(savePath);
                HullData hullData = JsonUtility.FromJson<HullData>(json);
                if (hullData != null && hullData.cells != null && hullData.cells.Count > 0)
                {
                    LoadHullData(hullData);
                    Debug.Log("grid_cells: Загружены данные из автозагрузочного файла");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"grid_cells: Ошибка загрузки автозагрузочного файла: {e.Message}");
            }
        }
        
        // Если файлов нет или загрузка не удалась - создаем стандартную сетку
        Debug.Log("grid_cells: Создание стандартной сетки (файлы сохранения не найдены)");
        CreateGridCell();
        
        // Вычисляем максимальное расстояние от центра для анимации
        max_distance_from_center = 0;
        for (int i = 0; i < grid_cells_list.Count; i++)
        {
            float distance = Vector3.Distance(grid_cells_list[i].transform.position, transform.position);
            if (distance > max_distance_from_center)
            {
                max_distance_from_center = distance;
            }
        }
        
        // Финальный сброс состояний для всех клеток
        ResetAllCellStates();
    }
    
    void Update()
    {
        // Обработка клавиши F5 для сохранения в target_hull
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveToTargetHullFile();
        }
        
        if(local_timer > timer_to_show_grid_cells) return;
        local_timer += Time.deltaTime;

        // Вычисляем текущую дистанцию волны от центра
        // Используем max_distance_from_center для нормализации времени
        float wave_distance = (local_timer / timer_to_show_grid_cells) * max_distance_from_center;
        
        // Список клеток для удаления
        List<io_base> cells_to_remove = new List<io_base>();
        
        for (int i = 0; i < grid_cells_list.Count; i++)
        {
            // Вычисляем расстояние от центра до текущей клетки
            float cell_distance = Vector3.Distance(grid_cells_list[i].transform.position, transform.position);
            
            // Если волна дошла до этой клетки и она еще не активирована
            if (wave_distance >= cell_distance)
            { 
                // Активируем клетку
                grid_cells_list[i].AddStack(io_base.io_type.on);
                
                // Применяем желаемое состояние если оно отличается от on
                if (grid_cells_list[i].desiredState != io_base.io_type.on)
                {
                    grid_cells_list[i].AddStack(grid_cells_list[i].desiredState);
                }
                
                // Добавляем клетку в список для удаления
                cells_to_remove.Add(grid_cells_list[i]);
            }
        }

        // Удаляем активированные клетки из основного списка
        foreach (var cell in cells_to_remove)
        {
            grid_cells_list.Remove(cell);
            io_system.instance.io_list.Add(cell);
        }
    }

    private HullData lastLoadedHullData = null;

    [ContextMenu("Create Grid Cell")]
    private void CreateGridCell()
    {
        clear_all_grid_cells();
        
        // Очищаем io_system
        if (io_system.instance != null && io_system.instance.io_list != null)
        {
            io_system.instance.io_list.Clear();
        }
        
        if (useCircularGrid)
        {
            CreateCircularGrid();
        } 
    }
    
    [ContextMenu("Load Target Hull File")]
    private void LoadTargetHullFile()
    {
        if (target_hull == null)
        {
            Debug.LogWarning("grid_cells: target_hull не указан в инспекторе");
            return;
        }
        
        try
        {
            HullData hullData = JsonUtility.FromJson<HullData>(target_hull.text);
            if (hullData != null && hullData.cells != null && hullData.cells.Count > 0)
            {
                LoadHullData(hullData);
                Debug.Log($"grid_cells: Загружены данные из target_hull ({hullData.cells.Count} клеток)");
            }
            else
            {
                Debug.LogWarning("grid_cells: target_hull содержит невалидные данные");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"grid_cells: Ошибка загрузки target_hull: {e.Message}");
        }
    }
    
    [ContextMenu("Save Current State to Target Hull")]
    private void SaveCurrentStateToTargetHull()
    {
        if (target_hull == null)
        {
            Debug.LogWarning("grid_cells: target_hull не указан в инспекторе");
            return;
        }
        
        try
        {
            // Создаем данные текущего состояния
            HullData hullData = new HullData(grid_size);
            
            int savedCells = 0;
            
            // Сохраняем все клетки из io_system
            if (io_system.instance != null && io_system.instance.io_list != null)
            {
                foreach (var cell in io_system.instance.io_list)
                {
                    if (cell != null && cell.stack != null && cell.stack.Count > 0)
                    {
                        Vector3 position = cell.transform.position;
                        
                        // Сохраняем координаты с шагом 1 метр
                        // Используем целые числа для шага 1 метр
                        int x = Mathf.RoundToInt(position.x);
                        int z = Mathf.RoundToInt(position.z);
                        
                        // Получаем direction из поворота объекта
                        int direction = GetDirectionFromRotation(cell.transform.rotation);
                        
                        HullCellData cellData = new HullCellData(
                            x, z, cell.floor, cell.cell_type, direction
                        );
                        
                        hullData.cells.Add(cellData);
                        savedCells++;
                        
                        // Логируем первые несколько клеток для отладки
                        if (savedCells <= 5)
                        {
                            Debug.Log($"grid_cells: Сохранена клетка {savedCells}: позиция ({position.x}, {position.z}) -> координаты ({x}, {z})");
                        }
                    }
                }
            }
            
            // Если в io_system нет клеток, сохраняем из grid_cells_list
            if (savedCells == 0 && grid_cells_list != null)
            {
                Debug.LogWarning("grid_cells: io_system пуст, сохраняем из grid_cells_list");
                foreach (var cell in grid_cells_list)
                {
                    if (cell != null && cell.stack != null && cell.stack.Count > 0)
                    {
                        Vector3 position = cell.transform.position;
                        
                        // Сохраняем координаты с шагом 1 метр
                        // Используем целые числа для шага 1 метр
                        int x = Mathf.RoundToInt(position.x);
                        int z = Mathf.RoundToInt(position.z);
                        
                        // Получаем direction из поворота объекта
                        int direction = GetDirectionFromRotation(cell.transform.rotation);
                        
                        HullCellData cellData = new HullCellData(
                            x, z, cell.floor, cell.cell_type, direction
                        );
                        
                        hullData.cells.Add(cellData);
                        savedCells++;
                    }
                }
            }
            
            // Сохраняем в target_hull
            string json = JsonUtility.ToJson(hullData, true);
            // Примечание: TextAsset нельзя изменить во время выполнения, поэтому выводим JSON в консоль 
            Debug.Log($"grid_cells: Сохранено {savedCells} клеток в target_hull (скопируйте JSON выше в файл)");
            
            // Подсчитываем клетки по типам  
        }
        catch (System.Exception e)
        {
            Debug.LogError($"grid_cells: Ошибка сохранения в target_hull: {e.Message}");
        }
    }
    
    // Метод для сохранения в target_hull файл по F5
    private void SaveToTargetHullFile()
    {
        if (target_hull == null)
        {
            Debug.LogWarning("grid_cells: target_hull не указан в инспекторе");
            return;
        }
        
        try
        {
            // Получаем путь к файлу target_hull
            #if UNITY_EDITOR
            string targetHullPath = UnityEditor.AssetDatabase.GetAssetPath(target_hull);
            #else
            string targetHullPath = "target_hull.json"; // Fallback для build
            #endif
            
            Debug.Log($"grid_cells: Сохранение в файл: {targetHullPath}");
            
            // Создаем данные текущего состояния
            HullData hullData = new HullData(grid_size);
            
            int savedCells = 0;
            
            // Сохраняем все клетки из io_system
            if (io_system.instance != null && io_system.instance.io_list != null)
            {
                foreach (var cell in io_system.instance.io_list)
                {
                    if (cell != null && cell.stack != null && cell.stack.Count > 0)
                    {
                        Vector3 position = cell.transform.position;
                        
                        // Сохраняем координаты с шагом 1 метр
                        // Используем целые числа для шага 1 метр
                        int x = Mathf.RoundToInt(position.x);
                        int z = Mathf.RoundToInt(position.z);
                        
                        // Пропускаем клетки помеченные на удаление
                        if (cell.stack.Last() == io_base.io_type.ToRemove)
                            continue;
                        
                        // Получаем direction из поворота объекта
                        int direction = GetDirectionFromRotation(cell.transform.rotation);
                        
                        HullCellData cellData = new HullCellData(
                            x, z, cell.floor, cell.cell_type, direction
                        );
                        
                        hullData.cells.Add(cellData);
                        savedCells++;
                    }
                }
            }
            
            // Если в io_system нет клеток, сохраняем из grid_cells_list
            if (savedCells == 0 && grid_cells_list != null)
            {
                Debug.LogWarning("grid_cells: io_system пуст, сохраняем из grid_cells_list");
                foreach (var cell in grid_cells_list)
                {
                    if (cell != null && cell.stack != null && cell.stack.Count > 0)
                    {
                        Vector3 position = cell.transform.position;
                        
                        // Сохраняем координаты с шагом 1 метр
                        // Используем целые числа для шага 1 метр
                        int x = Mathf.RoundToInt(position.x);
                        int z = Mathf.RoundToInt(position.z);
                        
                        // Получаем direction из поворота объекта
                        int direction = GetDirectionFromRotation(cell.transform.rotation);
                        
                        HullCellData cellData = new HullCellData(
                            x, z, cell.floor, cell.cell_type, direction
                        );
                        
                        hullData.cells.Add(cellData);
                        savedCells++;
                    }
                }
            }
            
            // Сохраняем в файл
            string json = JsonUtility.ToJson(hullData, true);
            System.IO.File.WriteAllText(targetHullPath, json);
            
            // Обновляем asset в Unity
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
             
            
            Debug.Log($"grid_cells: Сохранено {savedCells} клеток в файл {targetHullPath}");
            Debug.Log($"grid_cells: Файл обновлен и готов к использованию");
            
            // Дополнительная информация для отладки
            if (io_system.instance != null && io_system.instance.io_list != null)
            {
                Debug.Log($"grid_cells: Всего клеток в io_system: {io_system.instance.io_list.Count}");
            }
            if (grid_cells_list != null)
            {
                Debug.Log($"grid_cells: Всего клеток в grid_cells_list: {grid_cells_list.Count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"grid_cells: Ошибка сохранения в target_hull файл: {e.Message}");
        }
    }
    
    // Метод для создания квадратной сетки 
    
    // Метод для создания круговой сетки
    private void CreateCircularGrid()
    {
        // Используем настраиваемый радиус окружности
        float radius = circleRadius;
        
        // Создаем клетки по всей окружности с шагом 1 метр
        for (int i = -Mathf.CeilToInt(radius); i <= Mathf.CeilToInt(radius); i++)
        {
            for (int j = -Mathf.CeilToInt(radius); j <= Mathf.CeilToInt(radius); j++)
            {
                // Вычисляем позицию клетки с шагом 1 метр
                float x = i;
                float z = j;
                
                // Проверяем, находится ли клетка внутри окружности
                float distanceFromCenter = Mathf.Sqrt(x * x + z * z);
                if (distanceFromCenter <= radius)
                {
                    try
                    {
                        Vector3 position = new Vector3(x, 0, z);
                        // Создаем клетку с поворотом на север (direction = 0)
                        Quaternion rotation = GetRotationFromDirection(0);
                        io_base io = Instantiate(io_system.instance.cells_prefabs[0], position, rotation, transform).GetComponent<io_base>();
                    
                    if (io == null)
                    {
                        Debug.LogError($"grid_cells: Не удалось создать клетку для позиции {i}, {j}");
                        continue;
                    }
                    
                        io.name = $"Circle Cell ({x}, {z}) (dir:0)";
                    
                    if (io.target_collider != null)
                    {
                            io.target_collider.gameObject.name = $"Circle_Collider ({x}, {z})";
                        }
                        
                        // Инициализируем клетку в состоянии off для анимации
                        io.AddStack(io_base.io_type.off);
                        io.desiredState = io_base.io_type.on; 
                        // Добавляем клетку в io_system
                        if (io_system.instance != null && !io_system.instance.io_list.Contains(io))
                        {
                            io_system.instance.io_list.Add(io);
                    }
                    
                    grid_cells_list.Add(io);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"grid_cells: Ошибка создания клетки {i}, {j}: {e.Message}");
                    }
                }
            }
        }
        
        Debug.Log($"grid_cells: Создано {grid_cells_list.Count} клеток по окружности с радиусом {radius} (шаг 1 метр)");
        
        // Вычисляем максимальное расстояние от центра для анимации
        max_distance_from_center = radius;
        
        // Сбрасываем все состояния клеток до базовых
        ResetAllCellStates();
    }
    
    // Метод для сброса всех состояний клеток
    private void ResetAllCellStates()
    {
        // Сбрасываем таймер анимации
        local_timer = 0;
        
        foreach (var cell in grid_cells_list)
        {
            if (cell != null)
            {
                // Очищаем все состояния
                cell.stack.Clear();
                
                // Устанавливаем базовое состояние off
                cell.stack.Add(io_base.io_type.off);
                
                // Если это загруженная клетка, сохраняем её желаемое состояние
                // Иначе устанавливаем on как желаемое состояние
                if (cell.desiredState == io_base.io_type.off)
                {
                    cell.desiredState = io_base.io_type.on;
                }
            }
        }
        
        Debug.Log($"grid_cells: Сброшены состояния для {grid_cells_list.Count} клеток, таймер анимации сброшен");
    }
    
    // Метод для загрузки данных корпуса
    public void LoadHullData(HullData hullData)
    {
        if (hullData == null || hullData.cells == null)
        {
            Debug.LogWarning("grid_cells: Получены пустые данные корпуса, создается стандартная сетка");
            CreateGridCell();
            return;
        }
        
        // Очищаем текущую сетку
        clear_all_grid_cells();
        
        // Устанавливаем размер сетки из данных
        grid_size = hullData.gridSize;
        
        // Вычисляем радиус окружности
        float radius = useCircularGrid ? circleRadius : grid_size / 2f;
        
            // Создаем клетки на основе загруженных данных
    foreach (var cellData in hullData.cells)
    {
            // Восстанавливаем координаты с шагом 1 метр
            // Делим на 2 чтобы получить правильные позиции
            Vector3 position = new Vector3(cellData.x * 1f, cellData.floor, cellData.z * 1f);
            
            // Проверяем, находится ли клетка внутри окружности (если используется круговая сетка)
            if (useCircularGrid)
            {
                float distanceFromCenter = Mathf.Sqrt(cellData.x * cellData.x + cellData.z * cellData.z) * 1f;
                if (distanceFromCenter > radius)
                {
                    Debug.LogWarning($"grid_cells: Клетка {cellData.x}, {cellData.z} находится за пределами окружности (расстояние: {distanceFromCenter}, радиус: {radius}), пропускаем");
                    continue;
                }
            }
        
        // Выбираем правильный префаб в зависимости от типа клетки
        io_base prefabToUse = io_system.instance.cells_prefabs.Find(x => x.cell_type == cellData.cellType);
        
        // Проверяем, что у нас есть валидный префаб
        if (prefabToUse == null)
        {
            Debug.LogError($"grid_cells: Префаб для типа клетки {cellData.cellType} не найден, пропускаем клетку {cellData.x}, {cellData.z}");
            continue;
        }
        
        try
        {
                // Создаем клетку с правильным поворотом
                Quaternion rotation = GetRotationFromDirection(cellData.direction);
                io_base io = Instantiate(prefabToUse, position, rotation, transform).GetComponent<io_base>();
            if (io == null)
            {
                Debug.LogError($"grid_cells: Не удалось создать клетку для позиции {cellData.x}, {cellData.z}");
                continue;
            }
            
                io.name = $"Grid Cell {cellData.x} {cellData.z} (dir:{cellData.direction})";

            
            
            io.floor = cellData.floor;
            
                // Устанавливаем направление клетки
                io.direction = cellData.direction;
            
            grid_cells_list.Add(io);
                
        }
        catch (System.Exception e)
        {
            Debug.LogError($"grid_cells: Ошибка создания клетки {cellData.x}, {cellData.z}: {e.Message}");
        }
    }
        
        // Если клеток нет, создаем стандартную сетку
        if (grid_cells_list.Count == 0)
        {
            Debug.LogWarning("grid_cells: Не удалось загрузить клетки, создается стандартная сетка");
            CreateGridCell();
        }
        
        // Вычисляем максимальное расстояние от центра для анимации
        max_distance_from_center = 0;
        for (int i = 0; i < grid_cells_list.Count; i++)
        {
            float distance = Vector3.Distance(grid_cells_list[i].transform.position, transform.position);
            if (distance > max_distance_from_center)
            {
                max_distance_from_center = distance;
            }
        }
        
        lastLoadedHullData = hullData;
        Debug.Log($"grid_cells: Загружено {grid_cells_list.Count} клеток из сохранения (радиус окружности: {radius}, макс. расстояние: {max_distance_from_center})");
        Debug.Log($"grid_cells: Информация о корпусе - имя: {hullData.hull_name}, тип: {hullData.hull_type}, ID: {hullData.user_id}");
        
        // Подсчитываем клетки по типам 
        
        // Сбрасываем все состояния клеток до базовых
        ResetAllCellStates();
    }
    
    // Метод для создания стандартной сетки при ошибках
    private void CreateDefaultGrid()
    {
        Debug.Log("grid_cells: Создание стандартной сетки");
        CreateGridCell();
    }
    
  
    
    // Метод для вычисления поворота на основе direction
    private Quaternion GetRotationFromDirection(int direction)
    {
        // 0=север (0°), 1=восток (90°), 2=юг (180°), 3=запад (270°)
        float yRotation = direction * 90f;
        return Quaternion.Euler(0, yRotation, 0);
    }
    
    // Метод для вычисления direction на основе поворота
    private int GetDirectionFromRotation(Quaternion rotation)
    {
        float yRotation = rotation.eulerAngles.y;
        // Нормализуем к диапазону 0-360
        while (yRotation < 0) yRotation += 360f;
        while (yRotation >= 360f) yRotation -= 360f;
        
        // Округляем до ближайшего направления
        int direction = Mathf.RoundToInt(yRotation / 90f);
        if (direction >= 4) direction = 0; // Защита от выхода за границы
        
        return direction;
    }
    
    // OnValidate для отслеживания изменений в редакторе
    void OnValidate()
    {
        // Закомментированный код OnValidate
    }
    
    private int CalculateEditorHash()
    {
        int hash = grid_size;
        hash = hash * 31 + (grid_cells_list != null ? grid_cells_list.GetHashCode() : 0);
        return hash;
    }
    
    private string GetHullSavePath()
    {
        string persistentDataPath = Application.persistentDataPath;
        string hullSavesPath = System.IO.Path.Combine(persistentDataPath, "HullSaves");
        return System.IO.Path.Combine(hullSavesPath, "last_hull.json");
    }
    
    
    
    private void clear_all_grid_cells()
    {
        // Удаляем клетки из io_system
        if (io_system.instance != null && io_system.instance.io_list != null)
        {
            for (int i = io_system.instance.io_list.Count - 1; i >= 0; i--)
            {
                if (io_system.instance.io_list[i] != null && io_system.instance.io_list[i].transform.parent == transform)
                {
                    io_system.instance.io_list.RemoveAt(i);
                }
            }
        }
        
        // Удаляем дочерние объекты
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        grid_cells_list.Clear();
    }
}


