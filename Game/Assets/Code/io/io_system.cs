using System.Collections.Generic;
using System.Linq; 
using Unity.VisualScripting;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class io_system : MonoBehaviour
{
    private static io_system _instance;
    public static io_system instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<io_system>();
            }
            return _instance;
        }
    }
    float base_delay = 1;
    [SerializeField] public List<io_base> io_list = new List<io_base>();
    Camera main_camera
    {
        get
        {
            return Camera.main;
        }
    }
    public List<io_base> io_stack;
    public enum mode
    {
        create = 0,
        edit = 1,
        play = 2
    }
    public mode current_mode = mode.edit;

    [SerializeField] public List<io_base> cells_prefabs = new List<io_base>();


    [Header("Настройки управления этажами")]
    [SerializeField] private float floorChangeCooldown = 0.5f; // Задержка между сменой этажей
    [SerializeField] private int minFloor = -2; // Минимальный этаж
    [SerializeField] private int maxFloor = 2;  // Максимальный этаж
    [SerializeField] private float floorLerpSpeed = 5f; // Скорость плавного перемещения этажей

    private float lastFloorChangeTime = 0f; // Время последней смены этажа
    private Dictionary<io_base, Vector3> targetPositions = new Dictionary<io_base, Vector3>(); // Целевые позиции для lerp

    // Переменная для оптимизации рейкастинга
    private Vector3 lastMousePosition = Vector3.zero; // Предыдущие координаты мыши
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            current_mode = mode.create;
            create_cell.AddStack(io_base.io_type.on);
            clear_mouse_over();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            current_mode = mode.edit;
            create_cell.AddStack(io_base.io_type.off);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            current_mode = mode.play;
        }
        if (change_cell_type()) return;
        HandleFloorChange();

        // Применение плавного перемещения этажей
        ApplyFloorLerp();

        // Очищаем список от уничтоженных объектов


        if (Input.GetKeyDown(KeyCode.R))
        {
            //restart scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        base_delay -= Time.deltaTime;
        if (base_delay > 0) return;
        if (current_mode == mode.create)
        {
            create_mode();
        }
        else
        {

            
            ModifyMode();
        }
    }

    // Метод для очистки списка от уничтоженных объектов
public void clear_mouse_over(){
    foreach(var io in io_list){
        io.RemoveStack(io_base.io_type.mouseOver);
    }
}

    // Метод для деселектирования всех объектов
    private void DeselectAllObjects()
    {
        foreach (var io in io_list)
        {
            if (io.stack.Contains(io_base.io_type.clicked))
            {
                io.AddStack(io_base.io_type.on);
            }
        }
        Debug.Log("Все объекты деселектированы");
    }

    bool change_cell_type()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // Создаем копию списка для безопасной итерации
            var ioListCopy = GetSelectedCells();
            foreach (var io in ioListCopy)
            {
                io.ChangeCellType(io_base.io_base_cell_type.stair);
            }
            // Деселектируем все объекты после смены типа
            DeselectAllObjects();
            return true;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            // Создаем копию списка для безопасной итерации
            var ioListCopy = GetSelectedCells();
            foreach (var io in ioListCopy)
            {
                io.ChangeCellType(io_base.io_base_cell_type.space);
            }
            // Деселектируем все объекты после смены типа
            DeselectAllObjects();
            return true;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Создаем копию списка для безопасной итерации
            var ioListCopy = GetSelectedCells();
            foreach (var io in ioListCopy)
            {
                io.ChangeCellType(io_base.io_base_cell_type.cell);
            }
            // Деселектируем все объекты после смены типа
            DeselectAllObjects();
            return true;
        }

        // Обработка изменения направления выделенных объектов
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Клавиша Q нажата!");
            // Поворот против часовой стрелки (-1)
            var ioListCopy = GetSelectedCells();
            Debug.Log($"Найдено выделенных объектов: {ioListCopy.Count}");
            foreach (var io in ioListCopy)
            {
                Debug.Log($"Применяю поворот -1 к объекту: {io.name}");
                io.direction--;
                io.localTimer = 0.75f;
            }
            return true;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Клавиша E нажата!");
            // Поворот по часовой стрелке (+1)
            var ioListCopy = GetSelectedCells();
            Debug.Log($"Найдено выделенных объектов: {ioListCopy.Count}");
            foreach (var io in ioListCopy)
            {
                Debug.Log($"Применяю поворот +1 к объекту: {io.name}");
                io.direction++;
                io.localTimer = 0.75f;
            }
            return true;
        }

        return false;
    }
    // Метод для обработки изменения этажа обычным колесом мыши
    // (не зажатым, в отличие от вращения камеры)
    private void HandleFloorChange()
    {
        // Проверяем нажатие средней кнопки мыши для деселекта
        if (Input.GetMouseButtonDown(2))
        {
            // Проверяем, попал ли клик по какому-либо объекту
            bool hitSelectedObject = false;
            io_base hitSelected = null;

            RaycastHit hit;
            if (Physics.Raycast(main_camera.ScreenPointToRay(Input.mousePosition), out hit, 1000, LayerMask.GetMask("io_base")))
            {
                foreach (var io in io_list)
                {
                    if (io.target_collider == hit.collider &&
                        io.stack.Last() == io_base.io_type.clicked)
                    {
                        hitSelectedObject = true;
                        hitSelected = io;
                        break;
                    }
                }
            }

            if (hitSelectedObject)
            {
                hitSelected.AddStack(io_base.io_type.on);
            }
            else
            {
                // Если кликнули мимо выделенных объектов - снимаем выделение со всех
                foreach (var io in io_list)
                {
                    io.AddStack(io_base.io_type.on);
                }
            }
        }

        // Обработка колеса мыши для изменения этажа (только если не зажата средняя кнопка)
        if (!Input.GetMouseButton(2))
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput > 0f)
            {
                // Колесо вверх - поднимаем этаж
                ChangeFloor(1);
            }
            else if (scrollInput < 0f)
            {
                // Колесо вниз - опускаем этаж
                ChangeFloor(-1);
            }
        }
    }

    // Метод для применения плавного перемещения этажей
    private void ApplyFloorLerp()
    {
        // Список клеток для удаления из словаря (которые достигли цели)
        List<io_base> cellsToRemove = new List<io_base>();

        foreach (var kvp in targetPositions)
        {
            io_base cell = kvp.Key;
            Vector3 targetPosition = kvp.Value;

            // Плавное перемещение к целевой позиции
            Vector3 currentPosition = cell.transform.position;
            Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, floorLerpSpeed * Time.deltaTime);
            cell.transform.position = newPosition;

            // Обновляем поле floor в io_base при изменении позиции
            int newFloor = Mathf.RoundToInt(newPosition.y);
            if (cell.floor != newFloor)
            {
                cell.floor = newFloor;
            }

            // Проверяем, достигли ли мы цели (с небольшой погрешностью)
            if (Vector3.Distance(newPosition, targetPosition) < 0.01f)
            {
                // Устанавливаем точную позицию и помечаем для удаления
                cell.transform.position = targetPosition;
                cell.floor = Mathf.RoundToInt(targetPosition.y);
                cellsToRemove.Add(cell);
            }
        }

        // Удаляем клетки, которые достигли цели
        foreach (var cell in cellsToRemove)
        {
            targetPositions.Remove(cell);
        }

        // Если есть клетки, которые достигли цели, сохраняем корпус
        if (cellsToRemove.Count > 0)
        {
            SaveHullOnFloorChange();
        }
    }
    void Awake()
    {

    }




    // Метод для проверки наличия выбранных клеток
    private bool HasSelectedCells()
    {
        foreach (var io in io_list)
        {
            if (io.stack.Contains(io_base.io_type.clicked))
            {
                return true;
            }
        }
        return false;
    }

    // Метод для получения списка выбранных клеток
    private List<io_base> GetSelectedCells()
    {
        List<io_base> selectedCells = new List<io_base>();
        foreach (var io in io_list)
        {
            if (io.stack.Last() == io_base.io_type.clicked)
            {
                selectedCells.Add(io);
            }
            else
             if (io.stack.Last() == io_base.io_type.mouseOver)
            {
                if (io.stack.Count > 1)
                {
                    if (io.stack[io.stack.Count - 2] == io_base.io_type.clicked)
                    {
                        selectedCells.Add(io);
                    }
                }
            }
        }
        return selectedCells;
    }

    // Метод для изменения этажа выбранных клеток
    private void ChangeFloor(int direction)
    {
        // Проверяем, есть ли выбранные клетки
        if (!HasSelectedCells())
        {
            return;
        }

        // Проверяем задержку между сменами этажей
        if (Time.time - lastFloorChangeTime < floorChangeCooldown)
        {
            return;
        }

        // Получаем выбранные клетки
        List<io_base> selectedCells = GetSelectedCells();

        // Проверяем, можно ли изменить этаж для всех клеток
        bool canChange = true;
        foreach (var cell in selectedCells)
        {
            int currentCellFloor = Mathf.RoundToInt(cell.transform.position.y);
            int newFloor = currentCellFloor + direction;

            if (newFloor < minFloor || newFloor > maxFloor)
            {
                canChange = false;
                break;
            }
        }

        if (!canChange)
        {
            return;
        }

        lastFloorChangeTime = Time.time;

        // Устанавливаем целевые позиции для плавного перемещения
        foreach (var cell in selectedCells)
        {
            Vector3 currentPosition = cell.transform.position;
            int currentCellFloor = Mathf.RoundToInt(currentPosition.y);
            Vector3 targetPosition = currentPosition;
            targetPosition.y = currentCellFloor + direction; // Устанавливаем целевую высоту

            targetPositions[cell] = targetPosition;
        }

        Debug.Log($"SHIP_CAMERA: Этаж изменен на {direction}. Затронуто клеток: {selectedCells.Count}");

        // Принудительно сохраняем корпус при изменении этажа
        SaveHullOnFloorChange();
    }

    // Метод для принудительного сохранения при изменении этажа
    private void SaveHullOnFloorChange()
    {
        io_hull hull = FindObjectOfType<io_hull>();
        if (hull != null)
        {
            hull.SaveHullToFile();
        }
    }

    // Публичные методы для работы с этажами
    public int GetCurrentFloor()
    {
        // Возвращаем средний этаж выбранных клеток
        List<io_base> selectedCells = GetSelectedCells();
        if (selectedCells.Count == 0)
        {
            return 0;
        }

        float totalFloor = 0f;
        foreach (var cell in selectedCells)
        {
            totalFloor += cell.transform.position.y;
        }

        return Mathf.RoundToInt(totalFloor / selectedCells.Count);
    }

    public void SetFloor(int floor)
    {
        if (floor >= minFloor && floor <= maxFloor)
        {
            // Устанавливаем целевые позиции для всех выбранных клеток
            List<io_base> selectedCells = GetSelectedCells();
            foreach (var cell in selectedCells)
            {
                Vector3 currentPosition = cell.transform.position;
                Vector3 targetPosition = currentPosition;
                targetPosition.y = floor;
                targetPositions[cell] = targetPosition;
            }
        }
    }

    public bool HasSelectedCellsPublic()
    {
        return HasSelectedCells();
    }

    public int GetSelectedCellsCount()
    {
        return GetSelectedCells().Count;
    }

    // Метод для получения информации о этажах выбранных клеток
    public string GetSelectedCellsFloorInfo()
    {
        List<io_base> selectedCells = GetSelectedCells();
        if (selectedCells.Count == 0)
        {
            return "Нет выбранных клеток";
        }

        var floorGroups = selectedCells.GroupBy(cell => Mathf.RoundToInt(cell.transform.position.y));
        string info = $"Выбрано клеток: {selectedCells.Count}. Этажи: ";

        foreach (var group in floorGroups.OrderBy(g => g.Key))
        {
            info += $"этаж {group.Key} ({group.Count()} клеток), ";
        }

        return info.TrimEnd(',', ' ');
    }
    void ModifyMode()
    {
        if (main_camera == null) return;

        // Проверяем, находится ли мышь в пределах экрана
        Vector3 mousePosition = Input.mousePosition;

        // Проверяем нажатия кнопок мыши
        bool mouseButtonPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) ||
                                 Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);

        // Проверяем, изменились ли координаты мыши или были нажатия кнопок
        if (mousePosition == lastMousePosition && !mouseButtonPressed)
        {
            return; // Если координаты не изменились И не было нажатий, завершаем функцию
        }

        // Обновляем предыдущие координаты
        lastMousePosition = mousePosition;

        RaycastHit hit;
        bool raycastHit = Physics.Raycast(main_camera.ScreenPointToRay(mousePosition), out hit, 1000, LayerMask.GetMask("io_base"));

        if (Input.GetMouseButtonDown(1))
        {
            // Проверяем, попал ли правый клик по какому-либо объекту
            bool hitSelectedObject = false;
            io_base hitSelected = null;

            if (raycastHit)
            {
                foreach (var io in io_list)
                {
                    if (io.target_collider == hit.collider &&
                        io.stack[io.stack.Count - 2] == io_base.io_type.clicked)
                    {
                        hitSelectedObject = true;
                        hitSelected = io;
                        break;
                    }
                }
            }

            if (hitSelectedObject)
            {
                // Если кликнули по выделенному объекту - снимаем выделение только с него
                hitSelected.stack.Add(io_base.io_type.on);
            }
            else
            {
                // Если кликнули мимо выделенных объектов - снимаем выделение со всех
                foreach (var io in io_list)
                {

                    io.AddStack(io_base.io_type.on);
                }
            }
            return;
        }

        // only io_base layermask
        if (raycastHit)
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            {
                foreach (var io in io_list)
                {
                    if (io.target_collider == hit.collider)
                    {

                        if (io.stack.Last() == io_base.io_type.mouseOver || io.stack.Last() == io_base.io_type.on)
                        {
                            io.AddStack(io_base.io_type.clicked);

                            return;
                        }

                    }
                }


            }
            else
            {
                foreach (var io in io_list)
                {
                    if (io.target_collider == hit.collider)
                    {
                        io.AddStack(io_base.io_type.mouseOver);

                    }
                    else
                    {
                        io.RemoveStack(io_base.io_type.mouseOver);
                    }
                }
                return;
            }
        }
        else
        {
            foreach (var io in io_list)
            {
                if (io.stack.Count == 0) continue;
                if (io.stack.Last() == io_base.io_type.mouseOver) io.RemoveStack(io_base.io_type.mouseOver);
            }
            ;

        }
    }
    // Update is called once per frame

    #region create_mode
    private io_base _create_cell;
    public io_base create_cell
    {
        get
        {
            if (_create_cell == null)
            {
                _create_cell = Instantiate(cells_prefabs.Find(x => x.cell_type == io_base.io_base_cell_type.wall));
                _create_cell.target_collider.enabled = false;
            }
            return _create_cell;
        }
        set
        {
            if(value==null) return;
            if (_create_cell != null)
            {
                Destroy(_create_cell.gameObject);
            }
            _create_cell = Instantiate(value);
            _create_cell.target_collider.enabled = false;
        }
    }

    void UpdateCreatePrefab()
    {
        if (create_cell != null)
        { 
            if (Physics.Raycast(main_camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100, LayerMask.GetMask("io_base")))
            {
                RaycastHit realhit=hit;
                foreach (var hits in Physics.RaycastAll(main_camera.ScreenPointToRay(Input.mousePosition), 100, LayerMask.GetMask("io_base")))
                {
                    foreach (var cell in io_list)
                    {
                        if (cell.target_collider == hits.collider)
                        {
                            if (cell.cell_type == io_base.io_base_cell_type.cell)
                            {
                                realhit = hits;
                                break;
                            }
                        }
                    }
                }
                    Vector3 hitCellPos = realhit.transform.position;
                    Vector3 delta = realhit.point - hitCellPos;

                    // Определяем, по какой оси смещение больше
                    float absX = Mathf.Abs(delta.x);
                    float absZ = Mathf.Abs(delta.z);

                    // Обнуляем меньшую координату
                    if (absX > absZ)
                    {
                        delta.z = 0;
                    }
                    else
                    {
                        delta.x = 0;
                    }

                    if (delta.x > 0.01f) delta.x = 0.5f;
                    if (delta.z > 0.01f) delta.z = 0.5f;
                    if (delta.x < -0.01f) delta.x = -0.5f;
                    if (delta.z < -0.01f) delta.z = -0.5f;
                    Vector3 pos = hitCellPos + delta;

                    if (check_free_position_for_cell(pos) == null)
                    {
                        create_cell.transform.position = pos;

                        // Определяем направление для create_cell на основе дельты
                        int dir = 0;
                        if (absX > absZ)
                        {
                            dir = (delta.x > 0) ? 1 : 3; // 1 = вправо (90°), 3 = влево (270°)
                        }
                        else
                        {
                            dir = (delta.z > 0) ? 0 : 2; // 0 = вперед (0°), 2 = назад (180°)
                        }
                        create_cell.direction = dir;
                        ClickCreateCell();
                    }

                }
        }
    }
    float treshold_for_click=0.25f;
    public void ClickCreateCell()
    {
        if(treshold_for_click>0) return;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {

            io_base target_cell = check_free_position_for_cell(create_cell.transform.position);
            if (target_cell == null)// create new cell if space is free
            {

                io_base new_cell = io_system.createNewCell(create_cell);
                treshold_for_click = 0.25f;

            }
            else
            {
                treshold_for_click = 0.25f;
                Debug.Log("Space is not free" + target_cell.transform.position);
            }
        }
    }

    public io_base check_free_position_for_cell(Vector3 targetPositionCheck)        

    {        
        foreach (var cell in io_list)
        {
            if ( Mathf.Abs(cell.transform.position.x- targetPositionCheck.x) < 0.1f && Mathf.Abs(cell.transform.position.z- targetPositionCheck.z) < 0.1f)
            {
                return cell;
            }
        }
        return null;
    }


    
    // Статический метод для создания новой клетки
    public static io_base createNewCell(io_base templateCell)
    {

        if (templateCell == null)
        {
            Debug.LogError("io_system: templateCell равен null");
            return null;
        }

        if (instance == null)
        {
            Debug.LogError("io_system: instance равен null");
            return null;
        }

        try
        {
            // Создаем новую клетку на основе шаблона
            io_base newCell = Instantiate(templateCell, templateCell.transform.position, templateCell.transform.rotation);

            // Копируем важные параметры
            newCell.floor = templateCell.floor;

            newCell.direction = templateCell.direction;
            newCell.target_transform.localRotation = Quaternion.Euler(0, templateCell.direction * 90, 0);

            newCell.target_collider.enabled = true;
            // Инициализируем клетку
            newCell.Init(templateCell.transform.parent);
            // Добавляем в список
            if (instance.io_list != null)
            {
                instance.io_list.Add(newCell);
            }

            newCell.AddStack(io_base.io_type.on);
            Debug.Log($"io_system: Создана новая клетка типа {newCell.cell_type} в позиции {newCell.cell_position_in_grid} с направлением {newCell.direction}");

            return newCell;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"io_system: Ошибка создания новой клетки: {e.Message}");
            return null;
        }
    }


    void create_mode()
    {
        treshold_for_click-=Time.deltaTime;
        
        UpdateCreatePrefab(); 
        
    }
   #endregion
}
