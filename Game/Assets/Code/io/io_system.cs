using System.Collections.Generic;
using System.Linq;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class io_system : MonoBehaviour
{
    float base_delay = 1;
    [SerializeField] io_base[] io_list;
    Camera main_camera
    {
        get
        {
            return Camera.main;
        }
    }
    public List<io_base> io_stack;
    
    [Header("Настройки управления этажами")]
    [SerializeField] private float floorChangeCooldown = 0.5f; // Задержка между сменой этажей
    [SerializeField] private int minFloor = -2; // Минимальный этаж
    [SerializeField] private int maxFloor = 2;  // Максимальный этаж
    [SerializeField] private float floorLerpSpeed = 5f; // Скорость плавного перемещения этажей
    
    private float lastFloorChangeTime = 0f; // Время последней смены этажа
    private Dictionary<io_base, Vector3> targetPositions = new Dictionary<io_base, Vector3>(); // Целевые позиции для lerp
     void Update()
    {
        // Обработка колеса мыши для изменения этажа
        HandleFloorChange();
        
        // Применение плавного перемещения этажей
        ApplyFloorLerp();

        if (Input.GetKeyDown(KeyCode.R))
        {
            //restart scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        base_delay -= Time.deltaTime;
        if (base_delay >0) return;
        RaycasteveryFrame();
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
                    if (io.target_collider.gameObject == hit.collider.gameObject && 
                        io.io_type_stack.Contains(io_base.io_type.clicked))
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
                DeselectSpecific(hitSelected);
            }
            else
            {
                // Если кликнули мимо выделенных объектов - снимаем выделение со всех
                DeselectAll();
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
            
            // Проверяем, достигли ли мы цели (с небольшой погрешностью)
            if (Vector3.Distance(newPosition, targetPosition) < 0.01f)
            {
                // Устанавливаем точную позицию и помечаем для удаления
                cell.transform.position = targetPosition;
                cellsToRemove.Add(cell);
            }
        }
        
        // Удаляем клетки, которые достигли цели
        foreach (var cell in cellsToRemove)
        {
            targetPositions.Remove(cell);
        }
    }
    void Start()
    {
        io_stack = new List<io_base>();
        io_list = FindObjectsOfType<io_base>();
    }
    void DeselectAll()
    {
        // Очищаем целевые позиции для всех клеток
        targetPositions.Clear();
        
        foreach (var io in io_list)
        {
            io.io_type_stack.Remove(io_base.io_type.clicked);
        }
        
        Debug.Log("SHIP_CAMERA: Деселект всех клеток.");
    }
    
    void DeselectSpecific(io_base target)
    {
        // Очищаем целевую позицию для конкретной клетки
        if (targetPositions.ContainsKey(target))
        {
            targetPositions.Remove(target);
        }
        
        target.io_type_stack.Remove(io_base.io_type.clicked);
        
        Debug.Log($"SHIP_CAMERA: Деселект клетки {target.name}.");
    }
    
    // Метод для проверки наличия выбранных клеток
    private bool HasSelectedCells()
    {
        foreach (var io in io_list)
        {
            if (io.io_type_stack.Contains(io_base.io_type.clicked))
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
            if (io.io_type_stack.Contains(io_base.io_type.clicked))
            {
                selectedCells.Add(io);
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
    void RaycasteveryFrame()
    {
        if(main_camera == null) return;
        
        // Проверяем, находится ли мышь в пределах экрана
        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition.x < 0 || mousePosition.x > Screen.width || 
            mousePosition.y < 0 || mousePosition.y > Screen.height)
        {
            // Мышь за пределами экрана - убираем mouseOver со всех объектов
            foreach (var io in io_list)
            {
                if (io.io_type_stack.Contains(io_base.io_type.mouseOver))
                {
                    io.io_type_stack.Remove(io_base.io_type.mouseOver);
                }
            }
            return;
        }
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
                    if (io.target_collider.gameObject == hit.collider.gameObject && 
                        io.io_type_stack.Contains(io_base.io_type.clicked))
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
                DeselectSpecific(hitSelected);
            }
            else
            {
                // Если кликнули мимо выделенных объектов - снимаем выделение со всех
                DeselectAll();
            }
        }
        
        // only io_base layermask
        if (raycastHit)
        {
           Debug.Log(hit.collider.gameObject.name  );
           if(Input.GetMouseButton(0)||Input.GetMouseButtonDown(0))
           {
                foreach (var io in io_list)
                {
                    if(io.target_collider.gameObject == hit.collider.gameObject)
                    {   
                        
                    if (!io.io_type_stack.Contains(io_base.io_type.clicked))
                        {
                            io.io_type_stack.Add(io_base.io_type.clicked);
                        }
                        else
                        {
                           // io.io_type_stack.Remove(io_base.io_type.clicked);
                        }    
                    }
                    
                
            }
           }

           if(io_stack.Count == 0)
            foreach (var io in io_list)
            {
                                 if (io.target_collider.gameObject == hit.collider.gameObject)
                 {
                     if (!io.io_type_stack.Contains(io_base.io_type.mouseOver))
                     {
                         io.io_type_stack.Add(io_base.io_type.mouseOver);
                         Debug.Log(hit.collider.gameObject.name + " over " + io.name);
                     }
                 }
                 else
                 {
                     if (io.io_type_stack.Contains(io_base.io_type.mouseOver))
                     {
                         Debug.Log(hit.collider.gameObject.name + " NO over " + io.name);
                         io.io_type_stack.Remove(io_base.io_type.mouseOver);
                     }
                 }
            }
        }
        else
        {
            // Луч не попал ни в один объект - убираем mouseOver со всех объектов
            foreach (var io in io_list)
            {
                if (io.io_type_stack.Contains(io_base.io_type.mouseOver))
                {
                    io.io_type_stack.Remove(io_base.io_type.mouseOver);
                }
            }
        }
    }
    // Update is called once per frame
   
}
