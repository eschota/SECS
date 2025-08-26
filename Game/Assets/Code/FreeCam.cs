using UnityEngine;

public class FreeCam : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera _cam;
    
    [Header("Movement Settings")]
    public float movementSpeed = 10f;
    public float fastMovementSpeed = 20f;
    public float mouseSensitivity = 2f;
    public float smoothness = 5f;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minFov = 10f;
    public float maxFov = 120f;
    
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFov;
    private Vector3 currentVelocity;
    private Vector2 mouseInput;
    private bool isInitialized = false;
    
    // Переключение между камерами
    private cam mainCam;
    private bool isActive = false;

    void Awake()
    {
        if (_cam == null)
            _cam = Camera.main;
            
        // Находим основной скрипт камеры
        mainCam = FindObjectOfType<cam>();
        
        // Инициализируем позицию и поворот
        targetPosition = _cam.transform.position;
        targetRotation = _cam.transform.rotation;
        targetFov = _cam.fieldOfView;
        
        // Отключаемся по умолчанию
        enabled = false;
        isActive = false;
    }

    void Update()
    {
        if (!isActive || !isInitialized) return;
        
        HandleInput();
        UpdateCamera();
    }

    void HandleInput()
    {
        // Движение
        Vector3 input = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) input.z += 1f;
        if (Input.GetKey(KeyCode.S)) input.z -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        if (Input.GetKey(KeyCode.E)) input.y += 1f;
        if (Input.GetKey(KeyCode.Q)) input.y -= 1f;
        
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMovementSpeed : movementSpeed;
        
        if (input.magnitude > 0)
        {
            input.Normalize();
            Vector3 forward = _cam.transform.forward;
            Vector3 right = _cam.transform.right;
            Vector3 up = _cam.transform.up;
            
            // Убираем Y компоненту для горизонтального движения
            forward.y = 0; 
            right.y = 0;
            forward.Normalize(); 
            right.Normalize();
            
            Vector3 movement = (forward * input.z + right * input.x + up * input.y) * currentSpeed * Time.deltaTime;
            targetPosition += movement;
        }
        
        // Поворот камеры
        if (Input.GetMouseButton(1)) // Правая кнопка мыши
        {
            mouseInput.x = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseInput.y = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            Vector3 eulerAngles = targetRotation.eulerAngles;
            float currentX = eulerAngles.x;
            if (currentX > 180f) currentX -= 360f;
            
            float newX = Mathf.Clamp(currentX - mouseInput.y, -89f, 89f);
            float newY = eulerAngles.y + mouseInput.x;
            
            targetRotation = Quaternion.Euler(newX, newY, 0);
        }
        
        // Зум
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetFov -= scroll * zoomSpeed * 30f;
            targetFov = Mathf.Clamp(targetFov, minFov, maxFov);
        }
    }

    void UpdateCamera()
    {
        // Плавное движение к целевой позиции
        _cam.transform.position = Vector3.SmoothDamp(_cam.transform.position, targetPosition, ref currentVelocity, 1f / smoothness);
        
        // Плавный поворот
        _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, targetRotation, Time.deltaTime * smoothness);
        
        // Плавный зум
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * smoothness);
    }

    public void Activate()
    {
        if (mainCam != null)
        {
            // Отключаем основной скрипт камеры
            mainCam.enabled = false;
        }
        
        // Отключаем камеру от pivot если она прикреплена
        if (_cam.transform.parent != null)
        {
            _cam.transform.SetParent(null, true);
        }
        
        // Инициализируем позицию и поворот
        targetPosition = _cam.transform.position;
        targetRotation = _cam.transform.rotation;
        targetFov = _cam.fieldOfView;
        
        enabled = true;
        isActive = true;
        isInitialized = true;
        
        Debug.Log("<color=#4DA3FF>[FreeCam]</color> Activated");
    }

    public void Deactivate()
    {
        enabled = false;
        isActive = false;
        
        if (mainCam != null)
        {
            // Включаем основной скрипт камеры
            mainCam.enabled = true;
        }
        
        Debug.Log("<color=#4DA3FF>[FreeCam]</color> Deactivated");
    }

    public bool IsActive()
    {
        return isActive;
    }
}
