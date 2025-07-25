using UnityEngine;
using System.Collections.ObjectModel;
using System.Linq;

public class SHIP_CAMERA : MonoBehaviour
{
    // Статический Instance для доступа из других скриптов
    public static SHIP_CAMERA Instance { get; private set; }
    
    // Enum для типов камеры
    public enum camType
    {
        strategy,    // Стратегический вид (по умолчанию)
        firstPerson, // От первого лица
        free,        // Свободная камера
        freeze       // Замороженная камера
    }
    
    [Header("Настройки состояний камеры")]
    [SerializeField] private ObservableCollection<camType> stackTypes;
    
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float edgeScrollThreshold = 10f;
    [SerializeField] private float worldSizeX = 100f;
    [SerializeField] private float worldSizeZ = 100f;
    
    [Header("Настройки высоты")]
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 30f;
    [SerializeField] private float heightChangeSpeed = 5f;
    
    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float minRotationY = -30f;
    [SerializeField] private float maxRotationY = 60f;
    
    [Header("Настройки плавности")]
    [SerializeField] private float smoothness = 5f;
    
    [Header("Настройки угла наклона")]
    [SerializeField] private AnimationCurve heightToAngleCurve = AnimationCurve.Linear(10f, 45f, 30f, 90f);
    [SerializeField] private float startHeightAngle = 45f;
    [SerializeField] private float endHeightAngle = 90f;
    
    [Header("Настройки FOV")]
    [SerializeField] private AnimationCurve heightToFOVCurve = AnimationCurve.Linear(10f, 60f, 30f, 90f);
    [SerializeField] private float startFOV = 60f;
    [SerializeField] private float endFOV = 90f;
    
    private GameObject cameraPivot;
    private Vector3 targetPivotPosition;
    private float targetHeight;
    private float targetRotationY;
    private float targetAngleX;
    private float startRotationX;
    private bool isRotating = false;
    private Vector3 lastMousePosition;
    private Camera cameraComponent;
    
    // Переменные для режима freeze
    private Vector3 frozenPivotPosition;
    private float frozenHeight;
    private float frozenRotationY;
    private float frozenAngleX;
    
    void Awake()
    {
        // Установка статического Instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Инициализация ObservableCollection с обработчиком изменений
        stackTypes = new ObservableCollection<camType>();
        stackTypes.CollectionChanged += (sender, e) => 
        {
            // Обработка изменений состояний камеры
            OnCameraStateChanged();
        };
        
        // Добавляем базовое состояние
        stackTypes.Add(camType.strategy);
    }
    
    void Start()
    {
        // Получение компонента камеры
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("SHIP_CAMERA: Компонент Camera не найден!");
            return;
        }
        
        // Создание пивот-точки в координатах (0,0,0)
        CreateCameraPivot();
        
        // Инициализация начальных значений
        targetPivotPosition = cameraPivot.transform.position;
        targetHeight = transform.position.y;
        targetRotationY = transform.eulerAngles.y;
        targetAngleX = transform.eulerAngles.x;
        startRotationX = transform.eulerAngles.x;
        
        // Установка начального FOV
        cameraComponent.fieldOfView = startFOV;
    }

    void Update()
    {
        HandleCameraTypeSwitch();
        HandleMouseRotation();
        
        // Отключаем движение только если не вращаем камеру и не в режиме freeze
        if (!isRotating && !IsInFreezeMode())
        {
            HandleKeyboardInput();
            HandleMouseEdgeScrolling();
        }
        
        ApplySmoothMovement();
        UpdatePivotRotation();
    }
    
    private void CreateCameraPivot()
    {
        // Создание пивот-объекта
        cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.position = Vector3.zero;
        
        // Делаем камеру дочерним объектом пивота
        transform.SetParent(cameraPivot.transform);
        
        // Сохраняем локальную позицию камеры относительно пивота
        Vector3 localPosition = transform.localPosition;
        transform.localPosition = localPosition;
    }
    
    private void HandleKeyboardInput()
    {
        Vector3 input = Vector3.zero;
        
        // WSAD управление
        if (Input.GetKey(KeyCode.W)) input.z += 1f;
        if (Input.GetKey(KeyCode.S)) input.z -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        
        // Нормализация диагонального движения
        if (input.magnitude > 1f)
        {
            input.Normalize();
        }
        
        // Преобразование локального движения в мировые координаты относительно камеры
        Vector3 movement = input * moveSpeed * Time.deltaTime;
        Vector3 worldMovement = transform.TransformDirection(movement);
        
        // Обнуляем Y-составляющую для движения только в горизонтальной плоскости
        worldMovement.y = 0f;
        
        // Применение движения к позиции пивота
        targetPivotPosition += worldMovement;
        
        // Ограничение движения в пределах мира
        targetPivotPosition.x = Mathf.Clamp(targetPivotPosition.x, -worldSizeX / 2f, worldSizeX / 2f);
        targetPivotPosition.z = Mathf.Clamp(targetPivotPosition.z, -worldSizeZ / 2f, worldSizeZ / 2f);
    }
    
    private void HandleMouseEdgeScrolling()
    {
        // Проверка, что мышь находится в пределах окна игры
        if (!IsMouseInGameWindow())
        {
            return;
        }
        
        Vector3 edgeMovement = Vector3.zero;
        
        // Проверка позиции мыши относительно краёв экрана
        if (Input.mousePosition.x <= edgeScrollThreshold)
        {
            edgeMovement.x -= 1f;
        }
        else if (Input.mousePosition.x >= Screen.width - edgeScrollThreshold)
        {
            edgeMovement.x += 1f;
        }
        
        if (Input.mousePosition.y <= edgeScrollThreshold)
        {
            edgeMovement.z -= 1f;
        }
        else if (Input.mousePosition.y >= Screen.height - edgeScrollThreshold)
        {
            edgeMovement.z += 1f;
        }
        
        // Применение краевого скроллинга
        if (edgeMovement.magnitude > 0f)
        {
            edgeMovement.Normalize();
            Vector3 movement = edgeMovement * moveSpeed * Time.deltaTime;
            
            // Преобразование локального движения в мировые координаты относительно камеры
            Vector3 worldMovement = transform.TransformDirection(movement);
            
            // Обнуляем Y-составляющую для движения только в горизонтальной плоскости
            worldMovement.y = 0f;
            
            targetPivotPosition += worldMovement;
            
            // Ограничение движения в пределах мира
            targetPivotPosition.x = Mathf.Clamp(targetPivotPosition.x, -worldSizeX / 2f, worldSizeX / 2f);
            targetPivotPosition.z = Mathf.Clamp(targetPivotPosition.z, -worldSizeZ / 2f, worldSizeZ / 2f);
        }
    }
    
    private bool IsMouseInGameWindow()
    {
        // Проверка, что мышь находится в пределах окна игры
        Vector3 mousePosition = Input.mousePosition;
        
        // Проверяем, что координаты мыши находятся в пределах размеров экрана
        return mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
               mousePosition.y >= 0 && mousePosition.y <= Screen.height;
    }
    

    
    // Метод для вращения камеры и зума при зажатой средней кнопке мыши
    // (в отличие от обычного колеса мыши, которое используется для смены этажей)
    private void HandleMouseRotation()
    {
        // Отключаем вращение в режиме freeze
        if (IsInFreezeMode()) return;
        
        // Начало вращения при зажатии средней кнопки мыши
        if (Input.GetMouseButtonDown(2))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        
        // Окончание вращения при отпускании средней кнопки мыши
        if (Input.GetMouseButtonUp(2))
        {
            isRotating = false;
        }
        
        // Вращение камеры вокруг пивота при зажатой средней кнопке мыши
        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            targetRotationY += mouseDelta.x * rotationSpeed * Time.deltaTime;
            
            // Применяем вращение к пивоту, а не к камере
            cameraPivot.transform.rotation = Quaternion.Euler(0f, targetRotationY, 0f);
            
            lastMousePosition = Input.mousePosition;
        }
        
        // Обработка зума при зажатой средней кнопке мыши
        if (Input.GetMouseButton(2))
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                targetHeight -= scrollInput * heightChangeSpeed;
                targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
            }
        }
    }
    
    private void ApplySmoothMovement()
    {
        if (IsInFreezeMode())
        {
            // В режиме freeze используем замороженные значения
            cameraPivot.transform.position = frozenPivotPosition;
            
            Vector3 localPosition = transform.localPosition;
            localPosition.y = frozenHeight;
            transform.localPosition = localPosition;
            
            cameraPivot.transform.rotation = Quaternion.Euler(0f, frozenRotationY, 0f);
            transform.localRotation = Quaternion.Euler(frozenAngleX, 0f, 0f);
        }
        else
        {
            // Плавное движение пивота к целевой позиции
            cameraPivot.transform.position = Vector3.Lerp(cameraPivot.transform.position, targetPivotPosition, smoothness * Time.deltaTime);
            
            // Плавное изменение высоты камеры
            Vector3 localPosition = transform.localPosition;
            localPosition.y = Mathf.Lerp(localPosition.y, targetHeight, smoothness * Time.deltaTime);
            transform.localPosition = localPosition;
            
            // Обновление угла наклона и FOV в зависимости от высоты
            UpdateCameraAngleAndFOV();
            
            // Плавное вращение пивота (если не вращаем мышью)
            if (!isRotating)
            {
                Quaternion targetPivotRotation = Quaternion.Euler(0f, targetRotationY, 0f);
                cameraPivot.transform.rotation = Quaternion.Lerp(cameraPivot.transform.rotation, targetPivotRotation, smoothness * Time.deltaTime);
            }
            
            // Плавная интерполяция угла наклона камеры
            Vector3 currentLocalRotation = transform.localEulerAngles;
            Vector3 targetLocalRotation = new Vector3(targetAngleX, 0f, 0f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetLocalRotation), smoothness * Time.deltaTime);
        }
    }
    
    private void UpdateCameraAngleAndFOV()
    {
        // Получение текущей высоты камеры
        float currentHeight = transform.position.y;
        
        // Нормализация высоты для AnimationCurve (от 0 до 1)
        float heightNormalized = Mathf.InverseLerp(minHeight, maxHeight, currentHeight);
        
        // Вычисление целевого угла наклона через AnimationCurve
        float newTargetAngleX = heightToAngleCurve.Evaluate(heightNormalized);
        targetAngleX = newTargetAngleX;
        
        // Вычисление FOV через AnimationCurve
        float targetFOV = heightToFOVCurve.Evaluate(heightNormalized);
        
        // Применение FOV к камере
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = targetFOV;
        }
    }
    
    private void UpdatePivotRotation()
    {
        // Автоматический поворот пивота в направлении камеры
        Vector3 cameraDirection = transform.forward;
        cameraDirection.y = 0f; // Игнорируем вертикальную составляющую
        
        if (cameraDirection.magnitude > 0.1f)
        {
            cameraDirection.Normalize();
            Quaternion targetPivotRotation = Quaternion.LookRotation(cameraDirection);
            cameraPivot.transform.rotation = Quaternion.Lerp(cameraPivot.transform.rotation, targetPivotRotation, smoothness * Time.deltaTime);
        }
    }
    
    // Метод для получения текущих координат пивота (для отладки)
    public Vector3 GetPivotPosition()
    {
        return cameraPivot.transform.position;
    }
    
    // Метод для получения текущих координат камеры (для отладки)
    public Vector3 GetCameraPosition()
    {
        return transform.position;
    }
    
    // Метод для получения текущей высоты камеры
    public float GetCameraHeight()
    {
        return transform.position.y;
    }
    
    // Метод для получения текущего угла поворота
    public float GetCameraRotation()
    {
        return transform.eulerAngles.y;
    }
    
    // Метод для получения текущего типа камеры (для отладки)
    public string GetCurrentCameraTypeString()
    {
        return GetCurrentCameraType().ToString();
    }
    
    // Метод для проверки, находится ли камера в определенном режиме
    public bool IsInMode(camType type)
    {
        return GetCurrentCameraType() == type;
    }
    
    // Метод для разморозки камеры (переключение из режима freeze)
    public void UnfreezeCamera()
    {
        if (IsInFreezeMode())
        {
            SetCameraType(camType.strategy);
        }
    }

    // Метод для переключения типов камеры
    private void HandleCameraTypeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchToNextCameraType();
        }
    }
    
    // Метод для переключения на следующий тип камеры
    public void SwitchToNextCameraType()
    {
        camType currentType = GetCurrentCameraType();
        camType nextType = GetNextCameraType(currentType);
        
        // Удаляем текущий тип и добавляем следующий
        stackTypes.Remove(currentType);
        stackTypes.Add(nextType);
        
        Debug.Log($"SHIP_CAMERA: Переключение на режим {nextType}");
    }
    
    // Метод для получения следующего типа камеры
    private camType GetNextCameraType(camType currentType)
    {
        switch (currentType)
        {
            case camType.strategy:
                return camType.firstPerson;
            case camType.firstPerson:
                return camType.free;
            case camType.free:
                return camType.freeze;
            case camType.freeze:
                return camType.strategy;
            default:
                return camType.strategy;
        }
    }
    
    // Метод для получения текущего типа камеры
    public camType GetCurrentCameraType()
    {
        return stackTypes.LastOrDefault();
    }
    
    // Метод для проверки, находимся ли в режиме freeze
    private bool IsInFreezeMode()
    {
        return GetCurrentCameraType() == camType.freeze;
    }
    
    // Метод для обработки изменений состояния камеры
    private void OnCameraStateChanged()
    {
        camType newType = GetCurrentCameraType();
        
        switch (newType)
        {
            case camType.freeze:
                // Замораживаем текущее положение камеры
                FreezeCameraPosition();
                break;
            case camType.firstPerson:
                // Настройки для режима от первого лица
                SetupFirstPersonMode();
                break;
            case camType.free:
                // Настройки для свободной камеры
                SetupFreeMode();
                break;
            case camType.strategy:
                // Настройки для стратегического режима
                SetupStrategyMode();
                break;
        }
    }
    
    // Метод для заморозки текущего положения камеры
    private void FreezeCameraPosition()
    {
        frozenPivotPosition = cameraPivot.transform.position;
        frozenHeight = transform.localPosition.y;
        frozenRotationY = cameraPivot.transform.eulerAngles.y;
        frozenAngleX = transform.localEulerAngles.x;
        
        Debug.Log("SHIP_CAMERA: Камера заморожена в текущем положении");
    }
    
    // Метод для настройки режима от первого лица
    private void SetupFirstPersonMode()
    {
        // Устанавливаем низкую высоту и широкий угол обзора
        targetHeight = 2f;
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = 90f;
        }
        
        Debug.Log("SHIP_CAMERA: Переключение в режим от первого лица");
    }
    
    // Метод для настройки свободного режима
    private void SetupFreeMode()
    {
        // Убираем ограничения движения
        Debug.Log("SHIP_CAMERA: Переключение в свободный режим");
    }
    
    // Метод для настройки стратегического режима
    private void SetupStrategyMode()
    {
        // Возвращаем стандартные настройки
        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        Debug.Log("SHIP_CAMERA: Переключение в стратегический режим");
    }
    
    // Публичные методы для внешнего управления состоянием камеры
    public void SetCameraType(camType type)
    {
        camType currentType = GetCurrentCameraType();
        if (currentType != type)
        {
            stackTypes.Remove(currentType);
            stackTypes.Add(type);
        }
    }
    
    public void AddCameraType(camType type)
    {
        if (!stackTypes.Contains(type))
        {
            stackTypes.Add(type);
        }
    }
    
    public void RemoveCameraType(camType type)
    {
        if (stackTypes.Contains(type))
        {
            stackTypes.Remove(type);
        }
    }
}
