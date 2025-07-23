using UnityEngine;

public class SHIP_CAMERA : MonoBehaviour
{
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
        HandleMouseWheel();
        HandleMouseRotation();
        
        // Отключаем движение только если не вращаем камеру
        if (!isRotating)
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
    
    private void HandleMouseWheel()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            targetHeight -= scrollInput * heightChangeSpeed;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        }
    }
    
    private void HandleMouseRotation()
    {
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
        
        // Вращение камеры вокруг пивота (только по горизонтали)
        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            targetRotationY += mouseDelta.x * rotationSpeed * Time.deltaTime;
            
            // Применяем вращение к пивоту, а не к камере
            cameraPivot.transform.rotation = Quaternion.Euler(0f, targetRotationY, 0f);
            
            lastMousePosition = Input.mousePosition;
        }
    }
    
    private void ApplySmoothMovement()
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
}
