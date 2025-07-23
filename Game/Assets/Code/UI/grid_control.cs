using UnityEngine;

public class grid_control : MonoBehaviour
{
    [Header("Grid Shader Settings")]
    [SerializeField] private Material gridMaterial;
    [SerializeField] private bool useMouseFade = true;
    [SerializeField] private float fadeRadius = 5.0f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private bool useFadeCurve = false;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private int curveTextureSize = 256;
    [SerializeField] [Range(0.0f, 1.0f)] private float pulseColor = 0.0f;
    [SerializeField] [Range(0.0f, 1.0f)] private float pulseRadius = 0.0f;
    [SerializeField] [Range(0.1f, 10.0f)] private float pulseColorSpeed = 1.0f;
    [SerializeField] [Range(0.1f, 10.0f)] private float pulseRadiusSpeed = 1.0f;
    [SerializeField] private Color pulseColorAlt = Color.red;
    
    [Header("Mouse Settings")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool clampMouseToScreen = true;
    [SerializeField] private float screenMargin = 0.1f; // Отступ от краев экрана (0.1 = 10%)
    
    private Vector3 mouseWorldPosition;
    private bool isMouseOverGround = false;
    
    // Property names for shader
    private static readonly int UseMouseFadeProperty = Shader.PropertyToID("_UseMouseFade");
    private static readonly int FadeCenterProperty = Shader.PropertyToID("_FadeCenter");
    private static readonly int FadeRadiusProperty = Shader.PropertyToID("_FadeRadius");
    private static readonly int FadeColorProperty = Shader.PropertyToID("_FadeColor");
    private static readonly int UseFadeCurveProperty = Shader.PropertyToID("_UseFadeCurve");
    private static readonly int FadeCurveProperty = Shader.PropertyToID("_FadeCurve");
    private static readonly int PulseColorProperty = Shader.PropertyToID("_PulseColor");
    private static readonly int PulseRadiusProperty = Shader.PropertyToID("_PulseRadius");
    private static readonly int PulseColorSpeedProperty = Shader.PropertyToID("_PulseColorSpeed");
    private static readonly int PulseRadiusSpeedProperty = Shader.PropertyToID("_PulseRadiusSpeed");
    private static readonly int PulseColorAltProperty = Shader.PropertyToID("_PulseColorAlt");
    
    private Texture2D fadeCurveTexture;
    
    void Start()
    {
        // Если материал не назначен, попробуем найти его на этом объекте
        if (gridMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                gridMaterial = renderer.material;
#if UNITY_EDITOR
                Debug.Log("Found material: " + gridMaterial.name);
#endif
            }
        }
        
        // Если камера не назначена, используем основную камеру
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
#if UNITY_EDITOR
            Debug.Log("Using main camera: " + targetCamera.name);
#endif
        }
        
#if UNITY_EDITOR
        // Создаем текстуру для кривой затухания
        CreateFadeCurveTexture();
        
        // Инициализируем параметры шейдера
        UpdateShaderParameters();
        Debug.Log("Grid control initialized");
#endif
    }
    
    void Update()
    {
#if UNITY_EDITOR
        UpdateMousePosition();
        UpdateShaderParameters();
#endif
    }
    
#if UNITY_EDITOR
    void UpdateMousePosition()
    {
        if (!useMouseFade || targetCamera == null) return;
        
        // Получаем текущую позицию мыши
        Vector3 mouseScreenPosition = Input.mousePosition;
        
        // Ограничиваем позицию мыши в пределах экрана
        if (clampMouseToScreen)
        {
            mouseScreenPosition = ClampMouseToScreen(mouseScreenPosition);
        }
        
        // Делаем raycast для определения точки пересечения с землей
        Ray ray = targetCamera.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask))
        {
            mouseWorldPosition = hit.point;
            isMouseOverGround = true;
            
            // Отладочная информация
            if (Time.frameCount % 60 == 0) // Каждую секунду
            {
                Debug.Log($"Mouse world position: {mouseWorldPosition}");
            }
        }
        else
        {
            // Если raycast не попал в землю, используем приблизительную позицию
            mouseScreenPosition.z = targetCamera.transform.position.y;
            mouseWorldPosition = targetCamera.ScreenToWorldPoint(mouseScreenPosition);
            mouseWorldPosition.y = 0; // Устанавливаем Y в 0 для плоскости
            isMouseOverGround = false;
        }
    }
#endif
    
    Vector3 ClampMouseToScreen(Vector3 mousePos)
    {
        // Получаем размеры экрана
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Вычисляем границы с учетом отступа
        float marginX = screenWidth * screenMargin;
        float marginY = screenHeight * screenMargin;
        
        // Ограничиваем позицию мыши
        float clampedX = Mathf.Clamp(mousePos.x, marginX, screenWidth - marginX);
        float clampedY = Mathf.Clamp(mousePos.y, marginY, screenHeight - marginY);
        
        return new Vector3(clampedX, clampedY, mousePos.z);
    }
    
#if UNITY_EDITOR
    void UpdateShaderParameters()
    {
        if (gridMaterial == null) return;
        
        // Передаем параметры в шейдер
        gridMaterial.SetFloat(UseMouseFadeProperty, useMouseFade ? 1.0f : 0.0f);
        gridMaterial.SetVector(FadeCenterProperty, mouseWorldPosition);
        gridMaterial.SetFloat(FadeRadiusProperty, fadeRadius);
        gridMaterial.SetColor(FadeColorProperty, fadeColor);
        gridMaterial.SetFloat(UseFadeCurveProperty, useFadeCurve ? 1.0f : 0.0f);
        gridMaterial.SetFloat(PulseColorProperty, pulseColor);
        gridMaterial.SetFloat(PulseRadiusProperty, pulseRadius);
        gridMaterial.SetFloat(PulseColorSpeedProperty, pulseColorSpeed);
        gridMaterial.SetFloat(PulseRadiusSpeedProperty, pulseRadiusSpeed);
        gridMaterial.SetColor(PulseColorAltProperty, pulseColorAlt);
        
        // Обновляем текстуру кривой если нужно
        if (useFadeCurve && fadeCurveTexture != null)
        {
            gridMaterial.SetTexture(FadeCurveProperty, fadeCurveTexture);
        }
        
        // Отладочная информация
        if (Time.frameCount % 60 == 0) // Каждую секунду
        {
            Debug.Log($"Shader params - UseMouseFade: {useMouseFade}, FadeCenter: {mouseWorldPosition}, FadeRadius: {fadeRadius}");
        }
    }
#endif
    
#if UNITY_EDITOR
    void CreateFadeCurveTexture()
    {
        if (fadeCurveTexture != null)
        {
            DestroyImmediate(fadeCurveTexture);
        }
        
        fadeCurveTexture = new Texture2D(curveTextureSize, 1, TextureFormat.R8, false);
        fadeCurveTexture.wrapMode = TextureWrapMode.Clamp;
        fadeCurveTexture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[curveTextureSize];
        for (int i = 0; i < curveTextureSize; i++)
        {
            float t = (float)i / (curveTextureSize - 1);
            float value = fadeCurve.Evaluate(t);
            pixels[i] = new Color(value, value, value, 1);
        }
        
        fadeCurveTexture.SetPixels(pixels);
        fadeCurveTexture.Apply();
        
        Debug.Log("Fade curve texture created");
    }
#endif
    
#if UNITY_EDITOR
    // Публичные методы для изменения параметров во время выполнения
    public void SetUseMouseFade(bool enable)
    {
        useMouseFade = enable;
        Debug.Log($"Mouse fade set to: {enable}");
    }
    
    public void SetFadeRadius(float radius)
    {
        fadeRadius = Mathf.Clamp(radius, 0.1f, 50.0f);
        Debug.Log($"Fade radius set to: {fadeRadius}");
    }
    
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        Debug.Log($"Fade color set to: {color}");
    }
    
    public void SetGridMaterial(Material material)
    {
        gridMaterial = material;
        Debug.Log($"Grid material set to: {material.name}");
    }
    
    public void SetClampMouseToScreen(bool clamp)
    {
        clampMouseToScreen = clamp;
        Debug.Log($"Mouse clamping set to: {clamp}");
    }
    
    public void SetScreenMargin(float margin)
    {
        screenMargin = Mathf.Clamp01(margin);
        Debug.Log($"Screen margin set to: {screenMargin}");
    }
    
    public void SetUseFadeCurve(bool useCurve)
    {
        useFadeCurve = useCurve;
        Debug.Log($"Fade curve set to: {useCurve}");
    }
    
    public void UpdateFadeCurve(AnimationCurve newCurve)
    {
        fadeCurve = newCurve;
        CreateFadeCurveTexture();
        Debug.Log("Fade curve updated");
    }
    
    public void SetPulseColor(float intensity)
    {
        pulseColor = Mathf.Clamp01(intensity);
        Debug.Log($"Pulse color intensity set to: {pulseColor}");
    }
    
    public void SetPulseRadius(float intensity)
    {
        pulseRadius = Mathf.Clamp01(intensity);
        Debug.Log($"Pulse radius intensity set to: {pulseRadius}");
    }
    
    public void SetPulseColorSpeed(float speed)
    {
        pulseColorSpeed = Mathf.Clamp(speed, 0.1f, 10.0f);
        Debug.Log($"Pulse color speed set to: {pulseColorSpeed}");
    }
    
    public void SetPulseRadiusSpeed(float speed)
    {
        pulseRadiusSpeed = Mathf.Clamp(speed, 0.1f, 10.0f);
        Debug.Log($"Pulse radius speed set to: {pulseRadiusSpeed}");
    }
    
    public void SetPulseColorAlt(Color color)
    {
        pulseColorAlt = color;
        Debug.Log($"Pulse color alternative set to: {color}");
    }
#endif
    
#if UNITY_EDITOR
    // Методы для получения текущих значений
    public Vector3 GetMouseWorldPosition()
    {
        return mouseWorldPosition;
    }
    
    public bool IsMouseOverGround()
    {
        return isMouseOverGround;
    }
#endif
    
#if UNITY_EDITOR
    // Метод для отладки - показывает позицию мыши в сцене
    void OnDrawGizmos()
    {
        if (useMouseFade)
        {
            // Показываем позицию мыши
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mouseWorldPosition, 0.1f);
            
            // Показываем радиус затухания
            Gizmos.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0.3f);
            Gizmos.DrawWireSphere(mouseWorldPosition, fadeRadius);
            
            // Показываем луч от камеры к мыши
            if (targetCamera != null)
            {
                Gizmos.color = Color.yellow;
                Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
                Gizmos.DrawRay(ray.origin, ray.direction * 100f);
            }
        }
    }
#endif
} 