using UnityEngine;

public class cam : MonoBehaviour
{
    public Camera _cam;
    public Vector3 target_pivot_position;
    public Quaternion target_pivot_rotation;
    public float speed = 10;
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 2f;
    public float minZoomDistance = 0.3f; // 30% от начального расстояния
    public float maxZoomDistance = 2f;   // 200% от начального расстояния
    
    private GameObject cameraPivot;
    private Vector3 initialCameraLocalPosition;
    private Quaternion initialCameraLocalRotation;
    private Vector3 baseLocalPosition; // Базовая локальная позиция камеры
    private Quaternion baseLocalRotation; // Базовый локальный поворот камеры
    private float initialDistanceToPivot;
    private float currentDistanceToPivot;
    private bool isInitialized = false;
    
    void Awake()
    {
        _cam = Camera.main;
        CreatePivot();
        SetupCameraToFirstCell();
    }

    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraPivot != null)
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
            
            cameraPivot.transform.position = Vector3.Lerp(cameraPivot.transform.position, target_pivot_position, Time.deltaTime * speed);
        }
    }

    void HandleMovement()
    {
        if (!isInitialized) return;
        
        Vector3 input = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) input.z += 1f;
        if (Input.GetKey(KeyCode.S)) input.z -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        
        if (input.magnitude > 0)
        {
            // Нормализуем входной вектор
            input.Normalize();
            
            // Преобразуем в локальные координаты относительно направления камеры
            Vector3 forward = _cam.transform.forward;
            Vector3 right = _cam.transform.right;
            
            // Игнорируем Y компоненту для горизонтального движения
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 movement = (forward * input.z + right * input.x) * movementSpeed * Time.deltaTime;
            target_pivot_position += movement;
        }
    }
    
    void HandleRotation()
    {
        if (!isInitialized) return;
        
        if (Input.GetMouseButton(1)) // Правая кнопка мыши
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
            
            // Поворачиваем камеру вокруг пивота
            _cam.transform.RotateAround(cameraPivot.transform.position, Vector3.up, mouseX);
            _cam.transform.RotateAround(cameraPivot.transform.position, _cam.transform.right, -mouseY);
            
            // Ограничиваем вертикальный поворот
            Vector3 eulerAngles = _cam.transform.eulerAngles;
            if (eulerAngles.x > 180f)
                eulerAngles.x -= 360f;
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -80f, 80f);
            _cam.transform.eulerAngles = eulerAngles;
        }
    }
    
    void HandleZoom()
    {
        if (!isInitialized) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Изменяем локальную позицию камеры в процентах от базовой
            Vector3 zoomOffset = baseLocalPosition * scroll * zoomSpeed;
            Vector3 newLocalPosition = _cam.transform.localPosition + zoomOffset;
            
            // Ограничиваем зум в процентах от базовой позиции
            float distanceMultiplier = 1f + scroll * zoomSpeed;
            distanceMultiplier = Mathf.Clamp(distanceMultiplier, minZoomDistance, maxZoomDistance);
            
            // Применяем зум к базовой позиции
            _cam.transform.localPosition = baseLocalPosition * distanceMultiplier;
            
            Debug.Log($"Zoom: {distanceMultiplier:F2}x, Local position: {_cam.transform.localPosition}");
        }
    }

    void CreatePivot()
    {
        cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.parent = null;
        
        // Сохраняем начальную позицию и поворот камеры относительно мира
        initialCameraLocalPosition = _cam.transform.position;
        initialCameraLocalRotation = _cam.transform.rotation;
    }
    
    void SetupCameraToFirstCell()
    {
        // Находим Creator в сцене
        Creator creator = FindObjectOfType<Creator>();
        if (creator != null && creator.cells.Count > 0)
        {
            // Получаем первую клетку
            io_base firstCell = creator.cells[0];

            // Помещаем пивот в позицию первой клетки
            cameraPivot.transform.position = firstCell.target_world_position;
            
            // Сохраняем мировую позицию камеры перед изменением иерархии
            Vector3 worldPosition = _cam.transform.position;
            Quaternion worldRotation = _cam.transform.rotation;
            
            baseLocalPosition = _cam.transform.position;
            baseLocalRotation = _cam.transform.rotation;
            // Размещаем камеру внутри иерархии пивота
            _cam.transform.SetParent(cameraPivot.transform, true);
            
            // Сохраняем базовые локальные координаты камеры
            
            
            // Устанавливаем камеру в базовую локальную позицию
            _cam.transform.localPosition = baseLocalPosition;
            _cam.transform.localRotation = baseLocalRotation;
            
            // Сохраняем начальное расстояние до пивота
            initialDistanceToPivot = Vector3.Distance(_cam.transform.position, cameraPivot.transform.position);
            currentDistanceToPivot = initialDistanceToPivot;
            
            // Устанавливаем целевую позицию пивота
            target_pivot_position = firstCell.target_world_position;
            
            isInitialized = true;
            
            Debug.Log($"Camera initialized - Base local position: {baseLocalPosition}, Base local rotation: {baseLocalRotation}");
        }
    }
}
