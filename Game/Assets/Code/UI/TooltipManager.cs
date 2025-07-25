using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    [Header("Tooltip References")]
    public DynamicTooltip tooltip;
    
    [Header("Settings")]
    public bool enableTooltips = true;
    public float showDelay = 0.5f;
    
    private static TooltipManager instance;
    private float hoverTimer = 0f;
    private GameObject currentHoveredObject = null;
    private GameObject currentSelectedObject = null;
    
    public static TooltipManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TooltipManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TooltipManager");
                    instance = go.AddComponent<TooltipManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Создаем тултип сразу в Awake
            CreateTooltip();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Проверяем, что тултип создан успешно
        if (tooltip == null)
        {
            Debug.LogError("Failed to create tooltip!");
        }
        else
        {
            // Валидируем тултип после создания
            bool isValid = tooltip.ValidateTooltip();
            if (isValid)
            {
                Debug.Log("TooltipManager initialized successfully with valid tooltip");
            }
            else
            {
                Debug.LogWarning("TooltipManager initialized with auto-fixed tooltip");
            }
        }
    }
    
    void Update()
    {
        if (!enableTooltips || tooltip == null) return;
        
        // Проверяем наведение мыши на объекты
        CheckMouseHover();
        
        // Обновляем таймер показа тултипа
        if (currentHoveredObject != null)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= showDelay)
            {
                ShowTooltip();
            }
        }
        else
        {
            hoverTimer = 0f;
            HideTooltip();
        }
    }
    
    void CheckMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Проверяем, является ли объект клеткой
            if (hitObject.name.StartsWith("Cell_Grid_"))
            {
                if (currentHoveredObject != hitObject)
                {
                    currentHoveredObject = hitObject;
                    hoverTimer = 0f;
                }
            }
            else
            {
                currentHoveredObject = null;
            }
        }
        else
        {
            currentHoveredObject = null;
        }
    }
    
    void ShowTooltip()
    {
        if (tooltip != null && currentHoveredObject != null)
        {
            // Проверяем валидность тултипа перед показом
            if (!tooltip.IsValid())
            {
                Debug.LogWarning("Tooltip is not valid, attempting to fix...");
                tooltip.ValidateTooltip();
            }
            
            string hoveredName = currentHoveredObject.name;
            string selectedName = currentSelectedObject != null ? currentSelectedObject.name : "";
            
            tooltip.Show(hoveredName, selectedName);
        }
    }
    
    void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
    
    public void SetSelectedObject(GameObject selectedObject)
    {
        currentSelectedObject = selectedObject;
        
        if (tooltip != null && tooltip.IsVisible)
        {
            string hoveredName = currentHoveredObject != null ? currentHoveredObject.name : "";
            string selectedName = selectedObject != null ? selectedObject.name : "";
            
            tooltip.UpdateSelectedObject(selectedName);
        }
    }
    
    public void ClearSelectedObject()
    {
        currentSelectedObject = null;
        
        if (tooltip != null && tooltip.IsVisible)
        {
            string hoveredName = currentHoveredObject != null ? currentHoveredObject.name : "";
            tooltip.UpdateSelectedObject("");
        }
    }
    
    /// <summary>
    /// Принудительно валидирует тултип и исправляет проблемы
    /// </summary>
    /// <returns>true если тултип валиден после исправлений</returns>
    public bool ForceValidateTooltip()
    {
        if (tooltip == null)
        {
            Debug.LogWarning("Tooltip is null, attempting to create...");
            CreateTooltip();
            if (tooltip == null)
            {
                Debug.LogError("Failed to create tooltip for validation");
                return false;
            }
        }
        
        bool isValid = tooltip.ValidateTooltip();
        if (isValid)
        {
            Debug.Log("Tooltip validation passed successfully");
        }
        else
        {
            Debug.LogWarning("Tooltip validation completed with auto-fixes");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Проверяет, валиден ли тултип
    /// </summary>
    /// <returns>true если тултип валиден</returns>
    public bool IsTooltipValid()
    {
        return tooltip != null && tooltip.IsValid();
    }
    
    void CreateTooltip()
    {
        // Пытаемся загрузить префаб из ресурсов
        GameObject tooltipPrefab = Resources.Load<GameObject>("UI/ToolTipBase");
        
        if (tooltipPrefab != null)
        {
            // Создаем тултип из префаба
            GameObject tooltipInstance = Instantiate(tooltipPrefab);
            tooltip = tooltipInstance.GetComponentInChildren<DynamicTooltip>();
            
            if (tooltip != null)
            {
                Debug.Log("Tooltip created from prefab successfully");
            }
            else
            {
                Debug.LogError("Failed to find DynamicTooltip component in prefab");
                CreateTooltipManually();
            }
        }
        else
        {
            Debug.LogWarning("Tooltip prefab not found in Resources/UI/Base_Tooltip_Prefab, creating manually");
            CreateTooltipManually();
        }
    }
    
    void CreateTooltipManually()
    {
        // Создаем Canvas для тултипа
        GameObject canvasGO = new GameObject("TooltipCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Высокий приоритет
        
        // Добавляем CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Добавляем GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Создаем тултип
        GameObject tooltipGO = new GameObject("DynamicTooltip");
        tooltipGO.transform.SetParent(canvasGO.transform, false);
        
        // Добавляем RectTransform (обязательно для UI элементов)
        RectTransform rectTransform = tooltipGO.AddComponent<RectTransform>();
        
        // Добавляем компоненты тултипа
        tooltip = tooltipGO.AddComponent<DynamicTooltip>();
        
        // Создаем UI элементы
        CreateTooltipUI(tooltipGO);
        
        // Валидируем созданный тултип
        bool isValid = tooltip.ValidateTooltip();
        if (isValid)
        {
            Debug.Log("Tooltip created manually successfully");
        }
        else
        {
            Debug.LogWarning("Tooltip created manually with auto-fixes");
        }
    }
    
    void CreateTooltipUI(GameObject tooltipGO)
    {
        RectTransform rectTransform = tooltipGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 150);
        
        // Создаем фон
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(tooltipGO.transform, false);
        
        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        
        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        tooltip.backgroundImage = backgroundImage;
        
        // Создаем иконку
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(tooltipGO.transform, false);
        
        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.color = Color.white;
        
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(40, 40);
        iconRect.anchoredPosition = new Vector2(25, 0);
        
        tooltip.iconImage = iconImage;
        
        // Создаем заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(tooltipGO.transform, false);
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Информация";
        titleText.fontSize = 16;
        titleText.color = Color.black;
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(80, 10);
        titleRect.offsetMax = new Vector2(-10, -10);
        
        tooltip.titleText = titleText;
        
        // Создаем описание
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(tooltipGO.transform, false);
        
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = "Описание объекта";
        descText.fontSize = 12;
        descText.color = Color.black;
        
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.5f);
        descRect.offsetMin = new Vector2(10, 10);
        descRect.offsetMax = new Vector2(-10, -10);
        
        tooltip.descriptionText = descText;
    }
} 