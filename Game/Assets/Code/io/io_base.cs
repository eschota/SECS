using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;



[RequireComponent(typeof(Transform))]
public class io_base : MonoBehaviour
{ 
    [SerializeField] public List<io_type> stack = new List<io_type>();

    [SerializeField] public io_base_cell_type cell_type; public enum io_base_cell_type
    {
        cell,
        stair,
        space,
        wall,
        door,
        empty_wall,
        module
    } 
    [SerializeField] public Transform target_transform;
    [SerializeField] public Collider target_collider;
    public int floor = 0;
    
    [Header("Direction Settings")]
    [SerializeField] public int direction = 0; // Направление клетки (0-3, где 0=0°, 1=90°, 2=180°, 3=270°)
    
    Vector3 originalScale;
    Vector3 originalPosition;
    quaternion originalRotation;
    [SerializeField] public MeshRenderer[] target_mesh_renderer;

    public Vector3 cell_position_in_grid= new Vector3(0,0,0);
    // Поле для хранения желаемого состояния после анимации
    [HideInInspector] public io_type desiredState = io_type.on;
    
    [HideInInspector][SerializeField] private List<io_animation_SO> stateAnimations = new List<io_animation_SO>();
    
    [ExecuteInEditMode]
    [ContextMenu("Обновить анимации")]
    public void RefreshAnimations()
    {
        stateAnimations.Clear();

        // Загружаем все SO файлы анимаций из папки Resources/Settings
        io_animation_SO[] allSOs = Resources.LoadAll<io_animation_SO>("Settings");
        
        if (allSOs.Length == 0)
        {
            Debug.LogWarning($"Не найдено SO файлов анимаций в папке Resources/Settings");
            return;
        }
        
        // Добавляем все SO в список
        foreach (var so in allSOs)
        {
            if (so != null)
            {
                stateAnimations.Add(so);
            }
        }
        
//        Debug.Log($"Загружено {stateAnimations.Count} SO файлов анимаций для {gameObject.name}");
    }

    void OnValidate()
    {
        RefreshAnimations(); 
            ValidateTransform();
    }
    void ValidateTransform()
    {
        int c = 0;
        Transform t = null;
        Transform[] transforms = GetComponentsInChildren<Transform>();
        foreach (Transform k in transforms)
        {
            if (target_transform == k)
            {
                t = k;
            }
        }
        if (t != null)
        {

        }
        else
        {
            target_transform = transform.GetChild(0);

        }
        target_transform.gameObject.layer = LayerMask.NameToLayer("io_base");
        target_collider.gameObject.layer = LayerMask.NameToLayer("io_base");

    }

    public float localTimer = 0;
    
    // ObservableCollection автоматически уведомляет об изменениях

    
    // Публичное свойство для доступа к списку

    void Awake()
    { 
        stack = new List<io_type>();
        stack.Clear();
        stack.Add(io_type.off);
        // НЕ добавляем в io_system автоматически - это делается вручную в grid_cells
        // io_system.instance.io_list.Add(this);
        InitializeCell();
    }
  
    public void AddStack(io_type type)
    {
        if (stack.Count == 0) return;
        if (stack.Last() == type) return;
        if (stack.Last() == io_type.ToRemove) return;
        
        // Дополнительная проверка - если объект помечен на удаление, не добавляем новые состояния
        if (stack.Contains(io_type.ToRemove)) return;
        
        // Сохраняем предыдущее состояние для проверки изменений
        io_type previousState = stack.Count > 0 ? stack.Last() : io_type.off;
        
        // проверяем если последний это маусовер то надо его выбрать дать. иначе игнорировать добавление копии маусовера.
        if (stack.Last() == io_type.mouseOver)
        {
            if (type != io_type.mouseOver) stack.Add(type);
            return;
        }
        
        stack.Add(type);
        
        // Сбрасываем таймер только если это не повторное добавление mouseOver
        // или если предыдущее состояние не было mouseOver
        if (type != io_type.mouseOver || previousState != io_type.mouseOver)
        {
            localTimer = 0;
        }
        
        // Управляем parent'ом в зависимости от состояния
        ManageParentByState(previousState, type);
    }
    
    private void ManageParentByState(io_type previousState, io_type newState)
    {
        // Если переходим в clicked - убираем из общего пула
        if (newState == io_type.clicked && previousState != io_type.clicked)
        {
            if (transform.parent != null)
            {
                transform.parent = null;
//                Debug.Log($"Клетка {gameObject.name} выведена из общего пула (статус: clicked)");
            }
        }
        // Если выходим из clicked в обычное состояние - возвращаем в общий пул
        else if (previousState == io_type.clicked && newState != io_type.clicked)
        {
            if (transform.parent == null)
            {
                // Ищем grid_cells как родительский объект для клеток
                grid_cells gridCellsParent = FindObjectOfType<grid_cells>();
                if (gridCellsParent != null)
                {
                    transform.parent = gridCellsParent.transform;
             //       Debug.Log($"Клетка {gameObject.name} возвращена в общий пул (статус: {newState})");
                }
            }
        }
    }
    
    // Метод для проверки, можно ли взаимодействовать с объектом
    public bool CanInteract()
    {
        return stack != null && stack.Count > 0 && stack.Last() != io_type.ToRemove;
    }
    public void RemoveStack(io_type type)
    {
        if (stack.Count == 0) return;

        if (type == io_type.mouseOver)
        {
            if(stack.Last() == io_type.mouseOver) stack.RemoveAt(stack.Count - 1);
        localTimer = 0;
            return;
        } 
    
    }
    
    
    // Метод инициализации клетки при добавлении первого элемента
    private void InitializeCell()
    {
 
        
        // Сохраняем оригинальные значения
        originalScale = target_transform.localScale;
        originalPosition = target_transform.localPosition;
        originalRotation = target_transform.localRotation;
        
        // Сбрасываем таймер
        localTimer = 0f;
        
        // Инициализируем направление
        direction = 0; // По умолчанию направление 0 (0 градусов)
        
        // Инициализируем анимацию если есть
        if (stateAnimations.Count > 0)
        {
            var firstAnimation = stateAnimations[0];
            if (firstAnimation != null)
            {
                target_transform.localScale = firstAnimation.targetScale;
                target_transform.localPosition = firstAnimation.targetPosition;
                target_transform.localRotation = firstAnimation.targetRotation;
            }
        }
        
    }

    // Методы для процесса самоуничтожения




    public enum io_type
    {
        off = 0,
        on = 1,
        toggle = 2,
        mouseOver = 3,
        selected = 4,
        clicked = 5,
        deselected = 6, // Новый статус для деселекта
        drag = 7,
        floor_up = 8,
        floor_down = 9,
        ToRemove = 10,
        hidden = 11
    }
 

    public virtual void Init(Transform parent)
    {
        transform.parent = parent;
        // Для каждого target_mesh_renderer создаём новый экземпляр материала, чтобы не использовать sharedMaterial
        if (target_mesh_renderer != null)
        {
            for (int i = 0; i < target_mesh_renderer.Length; i++)
            {
                if (target_mesh_renderer[i] != null && target_mesh_renderer[i].sharedMaterial != null)
                {
                    // Создаём копию материала и присваиваем её renderer'у
                    target_mesh_renderer[i].material = new Material(target_mesh_renderer[i].sharedMaterial);
                }
            }
        }
        // Инициализируем target_transform только если есть анимации
        if (stateAnimations != null && stateAnimations.Count > 0)
        {
            target_transform.localScale = stateAnimations[0].targetScale;
            target_transform.localPosition = stateAnimations[0].targetPosition;
        } 
        SnapToGrid();
        transform.SetAsFirstSibling();
    }
        public virtual void ChangeCellType( io_base_cell_type _cell_type)
    { 
        // Проверяем, не помечен ли объект уже на удаление
        if (stack != null && stack.Count > 0 && stack.Last() == io_type.ToRemove)
        {
            Debug.LogWarning($"Попытка изменить тип клетки {gameObject.name}, которая уже помечена на удаление");
            return;
        }
        
        if (cell_type != _cell_type)  
        {
            // Создаем новую клетку
            io_base base_cell = Instantiate(io_system.instance.cells_prefabs.FirstOrDefault(c => c.cell_type == _cell_type), transform.position, Quaternion.identity).GetComponent<io_base>();
            
            // Правильно устанавливаем parent и инициализируем
            base_cell.transform.parent = transform.parent;
            base_cell.Init(transform.parent);
            
            // Принудительно обновляем коллайдер и другие компоненты
            if (base_cell.target_collider != null)
            {
                base_cell.target_collider.enabled = false;
                base_cell.target_collider.enabled = true;
            }
            
            // Обновляем материалы для корректной работы
            if (base_cell.target_mesh_renderer != null)
            {
                for (int i = 0; i < base_cell.target_mesh_renderer.Length; i++)
                {
                    if (base_cell.target_mesh_renderer[i] != null && base_cell.target_mesh_renderer[i].sharedMaterial != null)
                    {
                        base_cell.target_mesh_renderer[i].material = new Material(base_cell.target_mesh_renderer[i].sharedMaterial);
                    }
                }
            }
            // Автоматическая ориентация лестницы по клеткам на верхнем этаже
            int newDirection = direction; // По умолчанию используем текущее направление
            
            if (_cell_type == io_base_cell_type.stair)
            {
                newDirection = find_direction_for_stair();
            }
            
            // Устанавливаем параметры новой клетки
            base_cell.floor = floor;
            base_cell.direction = newDirection; // Используем новое направление
            
            // Применяем направление к transform новой клетки
            base_cell.direction = newDirection;
            
            base_cell.AddStack(io_base.io_type.clicked);
            
            // Добавляем новую клетку в список
            io_system.instance.io_list.Add(base_cell);
            
            // Помечаем старую клетку на удаление и убираем из списка
            this.stack.Clear();
            this.stack.Add(io_type.ToRemove);
            this.transform.parent = null;
            this.name = base_cell.name + "_toRemove";
            io_system.instance.io_list.Remove(this);
            
            // Принудительно сохраняем корпус при изменении типа клетки
            SaveHullOnCellChange();
        }
        ;
    }
    public void SnapToGrid()
    {
        transform.position = SnapToGrid(transform.position);
    }
  public Vector3 SnapToGrid(Vector3 _position)
{
    // Получаем текущую позицию объекта
    

    // Приводим позицию по осям X и Z к ближайшему кратному 0.5
    _position.x = Mathf.Round(_position.x * 2) / 2f; // Округляем к ближайшему 0.5
    _position.z = Mathf.Round(_position.z * 2) / 2f; // Округляем к ближайшему 0.5

    // Приводим позицию по оси Y к ближайшему целому числу
    _position.y = Mathf.Round(_position.y); // Округление до ближайшего целого

    // Устанавливаем обновленную позицию
    return _position;
}
    // Метод для принудительного сохранения при изменении клетки
    private void SaveHullOnCellChange()
    {
        io_hull hull = FindObjectOfType<io_hull>();
        if (hull != null)
        {
            hull.SaveHullToFile();
        }
    }
     
    private io_animation_SO GetCurrentAnimationSO()
    {
        if (stack == null || stack.Count == 0)
            return null;
        if (stateAnimations == null || stateAnimations.Count == 0)
            return null;

        var lastType = stack.Last();
        
        // Ищем SO в загруженном списке
        var so = stateAnimations.FirstOrDefault(s => s.animation_type_current == lastType);
        
        if (so == null)
        {
            Debug.LogWarning($"Не найден SO файл для анимации {lastType} в списке stateAnimations");
        }
        
        return so;
    }
    public io_base find_cell_by_direction(int direction, int floor)
    {
        float x = transform.position.x;
        float z = transform.position.z;
        
        // Упрощенный поиск: 4 клетки во все стороны
        // direction 0 = вперед (0°), 1 = вправо (90°), 2 = назад (180°), 3 = влево (270°)
        int direction_x = 0;
        int direction_z = 0;
        
        switch (direction)
        {
            case 0: // вперед (0°)
                direction_z = 1;
                break;
            case 1: // вправо (90°)
                direction_x = 1;
                break;
            case 2: // назад (180°)
                direction_z = -1;
                break;
            case 3: // влево (270°)
                direction_x = -1;
                break;
        }
        
        float new_x = x + direction_x;
        float new_z = z + direction_z;

        int target_x = Mathf.RoundToInt(new_x);
        int target_z = Mathf.RoundToInt(new_z);

        return io_system.instance.io_list.FirstOrDefault(c =>
            c.floor == floor &&
            Mathf.RoundToInt(c.transform.position.x) == target_x &&
            Mathf.RoundToInt(c.transform.position.z) == target_z
        );
    }
    
    public int getDirection(io_base A, io_base B)
    {
        // Вычисляем вектор от A к B
        Vector3 directionVector = B.transform.position - A.transform.position;
        
        // Определяем направление по наибольшему смещению
        float deltaX = directionVector.x;
        float deltaZ = directionVector.z;
        
        // Используем абсолютные значения для определения основного направления
        float absX = Mathf.Abs(deltaX);
        float absZ = Mathf.Abs(deltaZ);
        
        int direction;
        
        if (absX > absZ)
        {
            // Горизонтальное движение (влево/вправо)
            if (deltaX > 0)
            {
                direction = 1; // Вправо (90°)
            }
            else
            {
                direction = 3; // Влево (270°)
            }
        }
        else
        {
            // Вертикальное движение (вперед/назад)
            if (deltaZ > 0)
            {
                direction = 0; // Вперед (0°)
            }
            else
            {
                direction = 2; // Назад (180°)
            }
        }
        
        Debug.Log($"Направление от {A.name} к {B.name}: deltaX={deltaX:F2}, deltaZ={deltaZ:F2}, направление={direction}");
        
        return direction;
    }
    
    // Метод для установки направления клетки A в сторону клетки B
    public void SetDirectionTowards(io_base targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("Целевая клетка равна null");
            return;
        }
        
        int newDirection = getDirection(this, targetCell);
        direction = newDirection;
        
        // Применяем вращение к transform используя правильные углы
        float rotationY = direction * 90f; // 0°=0, 90°=1, 180°=2, 270°=3
        transform.rotation = Quaternion.Euler(0, rotationY, 0);
        
        Debug.Log($"Клетка {this.name} повернута в сторону {targetCell.name}, направление: {direction}, угол: {rotationY}°");
    }
    
    // Метод для изменения направления клетки
    public void ChangeDirection(int directionDelta)
    {
        // Получаем компонент анимации
        var directionAnimation = GetComponent<io_base_transform_animation>();
        if (directionAnimation != null)
        {
            // Используем существующую систему анимации
            directionAnimation.ChangeDirection(directionDelta);
            // Синхронизируем направление с нашим полем
            direction = directionAnimation.currentDirection;
        }
        else
        {
            // Если нет компонента анимации, просто меняем направление напрямую
            int newDirection = (direction + directionDelta + 4) % 4;
            if (newDirection != direction)
            {
                direction = newDirection;
                // Применяем вращение напрямую к transform
                float rotationY = direction * 90f;
                transform.rotation = Quaternion.Euler(0, rotationY, 0);
            }
        }
    }
    
    // Метод для установки направления напрямую
    public void SetDirection(int newDirection)
    {
        newDirection = (newDirection + 4) % 4; // Обеспечиваем диапазон 0-3
        
        var directionAnimation = GetComponent<io_base_transform_animation>();
        if (directionAnimation != null)
        {
            // Устанавливаем направление в компоненте анимации
            directionAnimation.currentDirection = newDirection;
            // Применяем вращение
            float rotationY = newDirection * 90f;
            transform.rotation = Quaternion.Euler(0, rotationY, 0);
        }
        
        direction = newDirection;
    }
    
    // Метод для получения текущего направления
    public int GetDirection()
    {
        return direction;
    }
    
    // Метод для получения угла направления в градусах
    public float GetDirectionAngle()
    {
        return direction * 90f;
    }
    // Ищет клетки типа target_cell_type на (floor + additionfloor) во всех 4 направлениях,
    // если находит, меняет их direction на target_direction, максимум target_cell_count_to_release клеток
    public List<io_base> find_cells_around_floor(io_type target_cell_type, int target_cell_count_to_release, int additionfloor, int target_direction)
    {
        List<io_base> foundCells = new List<io_base>();
        int searchFloor = this.floor + additionfloor;

        for (int dir = 0; dir < 4; dir++)
        {
            io_base cell = find_cell_by_direction(dir, searchFloor);
            if(cell!=null) Debug.Log($"find_cell: {cell.name}");
            if (cell != null &&
                cell.stack != null &&
                cell.stack.Count > 0 &&
                cell.stack.Last() == target_cell_type)
            {
                cell.direction = target_direction;
                foundCells.Add(cell);

                if (foundCells.Count >= target_cell_count_to_release)
                    break;
            }
        }

        return foundCells;
    }
    
    // Ищет клетки типа cell на верхнем этаже и возвращает направление первой найденной
    // Используется для автоматической ориентации лестниц
    public int find_direction_for_stair()
    {
        int searchFloor = this.floor + 1; // Ищем на верхнем этаже
        
        // Проверяем все 4 направления
        for (int dir = 0; dir < 4; dir++)
        {
            io_base cell = find_cell_by_direction(dir, searchFloor);
            if(cell!=null) Debug.Log($"find_cell: {cell.name}");
            if (cell != null &&
                cell.stack != null &&
                cell.stack.Count > 0 &&
                cell.cell_type == io_base_cell_type.cell) // Ищем именно клетки типа cell
            {
                Debug.Log($"Найдена клетка типа cell на верхнем этаже в направлении {dir}, устанавливаю направление лестницы");
                return dir; // Возвращаем направление к найденной клетке
            }
        }
        
        Debug.Log("Клетки типа cell на верхнем этаже не найдены, оставляю направление по умолчанию");
        return 0; // Если ничего не найдено, возвращаем направление по умолчанию
    }
    // Update is called once per frame
    void Update()
    { 

            // if (gameObject.name == "cell_wall_wall(Clone)")
            // {
            //     Debug.Log("cell_wall_wall(Clone)");
            // }




        // Обновляем анимацию вращения направления
        var directionAnimation = GetComponent<io_base_transform_animation>();
        if (directionAnimation != null)
        {
            directionAnimation.UpdateDirectionLerp();
        }
        
        localTimer += Time.deltaTime;
        if(stack.Count == 0 || stack==null) return;

                if (stack.Last() == io_type.ToRemove)
        {
            var animSO = GetCurrentAnimationSO();
            if (animSO != null && localTimer > animSO.duration)
            {
                Destroy(gameObject, (localTimer * localTimer % 1) / 3);
                return;
            }
            else
            {
                 Destroy(gameObject);
                return;
            }
            
            // Если нет анимации ToRemove, уничтожаем сразу
            if (animSO == null)
            {
                if (io_system.instance != null && io_system.instance.io_list != null)
                {
                    io_system.instance.io_list.Remove(this);
                }
                Destroy(gameObject);
                return;
            }
        
            // Выполняем анимацию исчезновения
            float progress = localTimer / animSO.duration;
            localTimer=Mathf.Clamp(localTimer, 0, 1);
            target_transform.localScale = new Vector3(Mathf.Max(target_transform.localScale.x*animSO.curve.Evaluate(localTimer), 0.01f), Mathf.Max(target_transform.localScale.y*animSO.curve.Evaluate(localTimer), 0.01f), Mathf.Max(target_transform.localScale.z*animSO.curve.Evaluate(localTimer), 0.01f));
            target_transform.localPosition = Vector3.Lerp(target_transform.localPosition, animSO.targetPosition, animSO.curve.Evaluate(localTimer));
            target_transform.localRotation = Quaternion.Slerp(target_transform.localRotation, animSO.targetRotation, animSO.curve.Evaluate(localTimer));
            
            // Анимация цвета
            for (int i = 0; i < target_mesh_renderer.Length; i++)
            {
                if (target_mesh_renderer[i] != null && target_mesh_renderer[i].material != null)
                {
                    target_mesh_renderer[i].material.color = Color.Lerp(target_mesh_renderer[i].material.color, animSO.targetColor, animSO.curve.Evaluate(progress));
                    target_mesh_renderer[i].material.SetColor("_EmissionColor", Color32.Lerp(target_mesh_renderer[i].material.GetColor("_EmissionColor"), animSO.targetEmissionColor, animSO.curve.Evaluate(progress)));
                }
            }
            return;
        } 
        var currentAnimSO = GetCurrentAnimationSO();
        if (currentAnimSO == null) return;
        target_transform.localScale = Vector3.Lerp(target_transform.localScale, currentAnimSO.targetScale, currentAnimSO.curve.Evaluate(localTimer / currentAnimSO.duration));
        if(target_transform.localScale.magnitude<0.01f)
        {
            target_transform.localScale=new Vector3(0.01f,0.01f,0.01f);
        }
       
        target_transform.localPosition = Vector3.Lerp(target_transform.localPosition, currentAnimSO.targetPosition, currentAnimSO.curve.Evaluate(localTimer / currentAnimSO.duration));
            // Validate quaternions before interpolation to avoid assertion errors
        Quaternion endRotation = Quaternion.Euler(0, direction * 90f, 0);   
        target_transform.localRotation = Quaternion.Slerp(target_transform.localRotation, endRotation, currentAnimSO.curve.Evaluate(localTimer / currentAnimSO.duration));
        // Анимация цвета
        for (int i = 0; i < target_mesh_renderer.Length; i++)
        {
            if (target_mesh_renderer[i] != null && target_mesh_renderer[i].material != null)
            {
                target_mesh_renderer[i].material.color = Color.Lerp(target_mesh_renderer[i].material.color, currentAnimSO.targetColor, currentAnimSO.curve.Evaluate(localTimer / currentAnimSO.duration));
                target_mesh_renderer[i].material.SetColor("_EmissionColor", Color32.Lerp(target_mesh_renderer[i].material.GetColor("_EmissionColor"), currentAnimSO.targetEmissionColor, currentAnimSO.curve.Evaluate(localTimer / currentAnimSO.duration)));
            }
        }
    }
 
}