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
    [SerializeField] private float worldSizePerFloor = 20f; // Размер мира на этаж
    [SerializeField] private float moveInertia = 0.9f; // Инерция движения (0-1)
    [SerializeField] private float moveDeceleration = 0.95f; // Замедление движения
    
    [Header("Настройки высоты")]
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 30f;
    [SerializeField] private float heightChangeSpeed = 5f;
    
    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float minRotationY = -30f;
    [SerializeField] private float maxRotationY = 60f;
    [SerializeField] private float rotationInertia = 0.95f; // Инерция вращения (0-1)
    [SerializeField] private float rotationDeceleration = 0.98f; // Замедление вращения
    
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
    private float currentRotationVelocity = 0f; // Текущая скорость вращения
    private float targetRotationVelocity = 0f; // Целевая скорость вращения
    private Vector3 currentMoveVelocity = Vector3.zero; // Текущая скорость движения
    private Vector3 targetMoveVelocity = Vector3.zero; // Целевая скорость движения
    
    // Флаг для сохранения исходного угла наклона
    private bool isInitialized = false;
    
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
        
        // Сохраняем исходные трансформы камеры (позиция 0,0,0 и поворот 70,45,0)
        Vector3 initialCameraPosition = transform.position;
        Quaternion initialCameraRotation = transform.rotation;
        
        Debug.Log($"SHIP_CAMERA: Исходные трансформы камеры - позиция: {initialCameraPosition}, поворот: {initialCameraRotation.eulerAngles}");
        
        // Создание пивот-точки с сохранением исходных трансформов камеры
        CreateCameraPivot();
        
        // Вычисляем центр матрицы на текущем этаже
        Vector3 centerPosition = Vector3.zero;
        if (io_system.instance != null)
        {
            centerPosition.y = io_system.instance.current_floor;
        }
        else
        {
            centerPosition.y = 0; // По умолчанию на нулевом этаже
        }
        
        // Перемещаем пивот в центр матрицы, камера автоматически следует за ним
        cameraPivot.transform.position = centerPosition;
        
        // Инициализация начальных значений
        targetPivotPosition = cameraPivot.transform.position;
        targetHeight = transform.localPosition.y;
        targetRotationY = cameraPivot.transform.eulerAngles.y;
        
        // Сохраняем исходный угол наклона камеры (45 градусов)
        targetAngleX = initialCameraRotation.eulerAngles.x;
        startRotationX = initialCameraRotation.eulerAngles.x;
        
        Debug.Log($"SHIP_CAMERA: Сохранен исходный угол наклона X: {targetAngleX} градусов");
        
        // Устанавливаем флаг инициализации
        isInitialized = true;
        
        // Установка начального FOV
        cameraComponent.fieldOfView = startFOV;
        
        Debug.Log($"SHIP_CAMERA: Пивот перемещен в центр матрицы на этаже {centerPosition.y}");
        Debug.Log($"SHIP_CAMERA: Финальная позиция камеры: {transform.position}, поворот: {transform.rotation.eulerAngles}");
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
        else
        {
            // Применяем замедление когда нет ввода
            ApplyMoveDeceleration();
        }

        ApplySmoothMovement();
        UpdatePivotRotation();
        UpdateFloor();
    }
    private void UpdateFloor()
    {
        // Проверяем, есть ли доступ к io_system
        if (io_system.instance == null) return;
        
        // Получаем текущий этаж
        int currentFloor = io_system.instance.current_floor;
        
        // Вычисляем размер мира для текущего этажа
        float currentWorldSizeX = worldSizeX + (currentFloor * worldSizePerFloor);
        float currentWorldSizeZ = worldSizeZ + (currentFloor * worldSizePerFloor);
        
        // Обновляем позицию камеры по Y
        Vector3 targetPosition = new Vector3(
            cameraPivot.transform.position.x, 
            currentFloor, 
            cameraPivot.transform.position.z
        );
        
        cameraPivot.transform.position = Vector3.Lerp(
            cameraPivot.transform.position, 
            targetPosition, 
            Time.deltaTime * 10
        );
        
        // Ограничиваем движение камеры в пределах мира для текущего этажа
        Vector3 clampedPosition = cameraPivot.transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -currentWorldSizeX / 2f, currentWorldSizeX / 2f);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -currentWorldSizeZ / 2f, currentWorldSizeZ / 2f);
        cameraPivot.transform.position = clampedPosition;
        
        // Обновляем targetPivotPosition чтобы избежать конфликтов
        targetPivotPosition = cameraPivot.transform.position;
    }
    private void CreateCameraPivot()
    {
        // Создание пивот-объекта
        cameraPivot = new GameObject("CameraPivot");
        
        // Сохраняем исходные трансформы камеры
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;
        
        // Делаем камеру дочерним объектом пивота
        transform.SetParent(cameraPivot.transform);
        
        // Восстанавливаем исходные трансформы камеры
        transform.position = worldPosition;
        transform.rotation = worldRotation;
        
        Debug.Log($"SHIP_CAMERA: Создан пивот, камера сохранила исходные трансформы - позиция: {worldPosition}, поворот: {worldRotation.eulerAngles}");
        
        // Позиция пивота будет установлена в Start()
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
        
        // Вычисляем целевую скорость движения
        Vector3 movement = input * moveSpeed;
        Vector3 worldMovement = transform.TransformDirection(movement);
        
        // Обнуляем Y-составляющую для движения только в горизонтальной плоскости
        worldMovement.y = 0f;
        
        // Устанавливаем целевую скорость движения
        targetMoveVelocity = worldMovement;
        
        // Применяем инерцию к скорости движения
        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetMoveVelocity, 1f - moveInertia);
        
        // Применение движения к позиции пивота
        targetPivotPosition += currentMoveVelocity * Time.deltaTime;
        
        // Получаем текущие границы мира для этажа
        float currentWorldSizeX = worldSizeX;
        float currentWorldSizeZ = worldSizeZ;
        if (io_system.instance != null)
        {
            int currentFloor = io_system.instance.current_floor;
            currentWorldSizeX = worldSizeX + (currentFloor * worldSizePerFloor);
            currentWorldSizeZ = worldSizeZ + (currentFloor * worldSizePerFloor);
        }
        
        // Ограничение движения в пределах мира
        targetPivotPosition.x = Mathf.Clamp(targetPivotPosition.x, -currentWorldSizeX / 2f, currentWorldSizeX / 2f);
        targetPivotPosition.z = Mathf.Clamp(targetPivotPosition.z, -currentWorldSizeZ / 2f, currentWorldSizeZ / 2f);
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
            Vector3 movement = edgeMovement * moveSpeed;
            
            // Преобразование локального движения в мировые координаты относительно камеры
            Vector3 worldMovement = transform.TransformDirection(movement);
            
            // Обнуляем Y-составляющую для движения только в горизонтальной плоскости
            worldMovement.y = 0f;
            
            // Добавляем к целевой скорости движения
            targetMoveVelocity += worldMovement;
            
            // Применяем инерцию к скорости движения
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetMoveVelocity, 1f - moveInertia);
            
            targetPivotPosition += currentMoveVelocity * Time.deltaTime;
            
            // Получаем текущие границы мира для этажа
            float currentWorldSizeX = worldSizeX;
            float currentWorldSizeZ = worldSizeZ;
            if (io_system.instance != null)
            {
                int currentFloor = io_system.instance.current_floor;
                currentWorldSizeX = worldSizeX + (currentFloor * worldSizePerFloor);
                currentWorldSizeZ = worldSizeZ + (currentFloor * worldSizePerFloor);
            }
            
            // Ограничение движения в пределах мира
            targetPivotPosition.x = Mathf.Clamp(targetPivotPosition.x, -currentWorldSizeX / 2f, currentWorldSizeX / 2f);
            targetPivotPosition.z = Mathf.Clamp(targetPivotPosition.z, -currentWorldSizeZ / 2f, currentWorldSizeZ / 2f);
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
    
    // Метод для применения замедления движения
    private void ApplyMoveDeceleration()
    {
        // Применяем замедление к скорости движения
        currentMoveVelocity *= moveDeceleration;
        targetMoveVelocity *= moveDeceleration;
        
        // Применяем оставшуюся скорость к позиции
        targetPivotPosition += currentMoveVelocity * Time.deltaTime;
        
        // Останавливаем движение если скорость стала очень маленькой
        if (currentMoveVelocity.magnitude < 0.1f)
        {
            currentMoveVelocity = Vector3.zero;
            targetMoveVelocity = Vector3.zero;
        }
    }
    

    
    // Метод для вращения камеры при зажатой средней кнопке мыши
    private void HandleMouseRotation()
    {
        // Отключаем вращение в режиме freeze
        if (IsInFreezeMode()) return;
        
        // Начало вращения при зажатии средней кнопки мыши
        if (Input.GetMouseButtonDown(2))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
            // Сбрасываем скорость при начале вращения
            currentRotationVelocity = 0f;
            targetRotationVelocity = 0f;
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
            
            // Вычисляем скорость вращения на основе движения мыши
            float mouseRotationSpeed = mouseDelta.x * rotationSpeed;
            targetRotationVelocity = mouseRotationSpeed;
            
            // Применяем инерцию к скорости вращения
            currentRotationVelocity = Mathf.Lerp(currentRotationVelocity, targetRotationVelocity, 1f - rotationInertia);
            
            // Применяем вращение к целевому углу
            targetRotationY += currentRotationVelocity * Time.deltaTime;
            
            lastMousePosition = Input.mousePosition;
        }
        else
        {
            // Применяем замедление когда не вращаем
            currentRotationVelocity *= rotationDeceleration;
            targetRotationY += currentRotationVelocity * Time.deltaTime;
            
            // Останавливаем вращение если скорость стала очень маленькой
            if (Mathf.Abs(currentRotationVelocity) < 0.1f)
            {
                currentRotationVelocity = 0f;
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
            
            // Плавное вращение пивота с учетом инерции
            Quaternion targetPivotRotation = Quaternion.Euler(0f, targetRotationY, 0f);
            cameraPivot.transform.rotation = Quaternion.Lerp(cameraPivot.transform.rotation, targetPivotRotation, smoothness * Time.deltaTime);
            
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
        
        // Вычисление целевого угла наклона через AnimationCurve только после инициализации
        if (isInitialized)
        {
            float newTargetAngleX = heightToAngleCurve.Evaluate(heightNormalized);
            targetAngleX = newTargetAngleX;
        }
        
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
    
    // Метод для получения текущих границ мира
    public Vector2 GetCurrentWorldBounds()
    {
        float currentWorldSizeX = worldSizeX;
        float currentWorldSizeZ = worldSizeZ;
        
        if (io_system.instance != null)
        {
            int currentFloor = io_system.instance.current_floor;
            currentWorldSizeX = worldSizeX + (currentFloor * worldSizePerFloor);
            currentWorldSizeZ = worldSizeZ + (currentFloor * worldSizePerFloor);
        }
        
        return new Vector2(currentWorldSizeX, currentWorldSizeZ);
    }
    
    // Метод для настройки параметров инерции вращения
    public void SetRotationInertia(float inertia, float deceleration)
    {
        rotationInertia = Mathf.Clamp01(inertia);
        rotationDeceleration = Mathf.Clamp01(deceleration);
    }
    
    // Метод для остановки вращения
    public void StopRotation()
    {
        currentRotationVelocity = 0f;
        targetRotationVelocity = 0f;
    }
    
    // Метод для настройки параметров инерции движения
    public void SetMoveInertia(float inertia, float deceleration)
    {
        moveInertia = Mathf.Clamp01(inertia);
        moveDeceleration = Mathf.Clamp01(deceleration);
    }
    
    // Метод для остановки движения
    public void StopMovement()
    {
        currentMoveVelocity = Vector3.zero;
        targetMoveVelocity = Vector3.zero;
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
//        Debug.Log("SHIP_CAMERA: Переключение в стратегический режим");
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
