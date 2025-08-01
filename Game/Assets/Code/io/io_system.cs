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
    [SerializeField] public List<io_base> io_list = new List<io_base>(); // Оставляем для совместимости, но будем использовать matrix
    [SerializeField] private matrix matrixSystem; // Ссылка на matrix для работы с матрицей
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
    public int current_floor=0;

    [Header("Настройки управления этажами")]
    [SerializeField] private float floorChangeCooldown = 0.5f; // Задержка между сменой этажей
    [SerializeField] private int minFloor = -2; // Минимальный этаж
    [SerializeField] private int maxFloor = 2;  // Максимальный этаж
    [SerializeField] private float floorLerpSpeed = 5f; // Скорость плавного перемещения этажей

    private float lastFloorChangeTime = 0f; // Время последней смены этажа
    private Dictionary<io_base, Vector3> targetPositions = new Dictionary<io_base, Vector3>(); // Целевые позиции для lerp
    private HashSet<int> floorsWithSpaceCells = new HashSet<int>(); // Этажи, на которых уже созданы клетки space

    // Переменная для оптимизации рейкастинга
    private Vector3 lastMousePosition = Vector3.zero; // Предыдущие координаты мыши
    void Awake()
    { 
        
    }
    void Update()
    {  
        if (change_cell_type()) return;
        HandleFloorChange();
        return;
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
     

        // Применение плавного перемещения этажей
     

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
    var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
    foreach(var io in currentFloorCells){
        io.RemoveStack(io_base.io_type.mouseOver);
    }
}

    // Метод для получения выделенных клеток на текущем этаже
    private List<io_base> GetSelectedCellsOnFloor()
    {
        return matrix.instance.GetSelectedCellsOnFloor(current_floor);
    }
    
    // Метод для получения всех выделенных клеток (для совместимости)
    private List<io_base> GetSelectedCells()
    {
        return GetSelectedCellsOnFloor();
    }
    
    // Метод для деселектирования всех объектов
    private void DeselectAllObjects()
    {
        var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
        foreach (var io in currentFloorCells)
        {
            if (io.stack.Contains(io_base.io_type.clicked))
            {
                io.AddStack(io_base.io_type.on);
            }
        }
        Debug.Log("Все объекты текущего этажа деселектированы");
    }

    bool change_cell_type()
    {
        // Обработка изменения типа клеток
        if (HandleCellTypeChange()) return true;
        
        // Обработка изменения направления
        if (HandleDirectionChange()) return true;
        
        return false;
    }
    
    // Метод для обработки изменения типа клеток
    private bool HandleCellTypeChange()
    {
        var selectedCells = GetSelectedCellsOnFloor();
        if (selectedCells.Count == 0) return false;
        
        bool cellTypeChanged = false;
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ApplyCellTypeChange(selectedCells, io_base.io_base_cell_type.stair);
            cellTypeChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            ApplyCellTypeChange(selectedCells, io_base.io_base_cell_type.space);
            cellTypeChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            ApplyCellTypeChange(selectedCells, io_base.io_base_cell_type.cell);
            cellTypeChanged = true;
        }
        
        if (cellTypeChanged)
        {
            DeselectAllObjects();
            return true;
        }
        
        return false;
    }
    
    // Метод для применения изменения типа клеток
    private void ApplyCellTypeChange(List<io_base> cells, io_base.io_base_cell_type newType)
    {
        foreach (var cell in cells)
        {
            cell.ChangeCellType(newType);
        }
    }
    
    // Метод для обработки изменения направления
    private bool HandleDirectionChange()
    {
        var selectedCells = GetSelectedCellsOnFloor();
        if (selectedCells.Count == 0) return false;
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Клавиша Q нажата!");
            ApplyDirectionChange(selectedCells, -1);
            return true;
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Клавиша E нажата!");
            ApplyDirectionChange(selectedCells, 1);
            return true;
        }
        
        return false;
    }
    
    // Метод для применения изменения направления
    private void ApplyDirectionChange(List<io_base> cells, int directionDelta)
    {
        Debug.Log($"Найдено выделенных объектов: {cells.Count}");
        foreach (var cell in cells)
        {
            Debug.Log($"Применяю поворот {directionDelta} к объекту: {cell.name}");
            cell.direction += directionDelta;
            cell.localTimer = 0.75f;
        }
    }
    
    public float floor_treshold=0.0f;
    private void HandleFloorChange()
    {
        floor_treshold+=Time.deltaTime;

        grid_control.Instance.transform.position=Vector3.Lerp(grid_control.Instance.transform.position,new Vector3(grid_control.Instance.transform.position.x,current_floor,grid_control.Instance.transform.position.z),Time.deltaTime*10);
        
        if (floor_treshold < 1.25f) return;
        
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput > 0f)
        {
            if(current_floor==matrix.instance.size-1) return;
            current_floor++;
            floor_treshold = 0.0f;
           matrix.instance.createFloor(current_floor);
        }
        else if (scrollInput < 0f)
        {
            if(current_floor==0) return;
            current_floor--;
            floor_treshold = 0.0f;
            matrix.instance.createFloor(current_floor);
        }  
    } 
    
    // Метод для создания клеток space на текущем этаже
   
        // Метод для создания одной клетки space
   


    
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
                var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
                foreach (var io in currentFloorCells)
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
                var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
                foreach (var io in currentFloorCells)
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
                var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
                foreach (var io in currentFloorCells)
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
                var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
                foreach (var io in currentFloorCells)
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
            var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
            foreach (var io in currentFloorCells)
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
                    var currentFloorCells = matrix.instance.GetFloorCells(current_floor);
                    foreach (var cell in currentFloorCells)
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
        // Используем матрицу для проверки позиции
        return matrix.instance.GetCellAtPosition(targetPositionCheck);
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
            // Добавляем в матрицу
            if (instance.matrixSystem != null)
            {
                instance.matrixSystem.AddCell(newCell);
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
