using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DynamicTooltip : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Tooltip Settings")]
    public float offsetX = 10f;
    public float offsetY = 10f;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.1f;
    
    [Header("Content")]
    public string currentHoveredObject = "";
    public string currentSelectedObject = "";
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isVisible = false;
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    
    public bool IsVisible => isVisible;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        SetAlpha(0f);
        gameObject.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        
        // Валидируем тултип при создании
        ValidateTooltip();
    }
    
    void Update()
    {
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
        {
            float delta = Time.deltaTime / (targetAlpha > currentAlpha ? fadeInDuration : fadeOutDuration);
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, delta);
            SetAlpha(currentAlpha);
            if (currentAlpha <= 0f && !isVisible) gameObject.SetActive(false);
        }
        if (isVisible) UpdatePosition();
    }
    
    /// <summary>
    /// Валидирует все элементы тултипа и создает недостающие компоненты
    /// </summary>
    /// <returns>true если тултип валиден, false если есть проблемы</returns>
    public bool ValidateTooltip()
    {
        bool isValid = true;
        string validationErrors = "";
        
        // Проверяем RectTransform
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
                validationErrors += "RectTransform был создан автоматически. ";
            }
        }
        
        // Проверяем CanvasGroup
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                validationErrors += "CanvasGroup был создан автоматически. ";
            }
        }
        
        // Проверяем фон
        if (backgroundImage == null)
        {
            GameObject backgroundGO = transform.Find("Background")?.gameObject;
            if (backgroundGO == null)
            {
                backgroundGO = new GameObject("Background");
                backgroundGO.transform.SetParent(transform, false);
                
                backgroundImage = backgroundGO.AddComponent<Image>();
                backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                
                RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                
                validationErrors += "Background Image был создан автоматически. ";
            }
            else
            {
                backgroundImage = backgroundGO.GetComponent<Image>();
                if (backgroundImage == null)
                {
                    backgroundImage = backgroundGO.AddComponent<Image>();
                    backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                    validationErrors += "Background Image компонент был добавлен автоматически. ";
                }
            }
        }
        
        // Проверяем иконку
        if (iconImage == null)
        {
            GameObject iconGO = transform.Find("Icon")?.gameObject;
            if (iconGO == null)
            {
                iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(transform, false);
                
                iconImage = iconGO.AddComponent<Image>();
                iconImage.color = Color.white;
                
                RectTransform iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.sizeDelta = new Vector2(40, 40);
                iconRect.anchoredPosition = new Vector2(25, 0);
                
                validationErrors += "Icon Image был создан автоматически. ";
            }
            else
            {
                iconImage = iconGO.GetComponent<Image>();
                if (iconImage == null)
                {
                    iconImage = iconGO.AddComponent<Image>();
                    iconImage.color = Color.white;
                    validationErrors += "Icon Image компонент был добавлен автоматически. ";
                }
            }
        }
        
        // Проверяем заголовок
        if (titleText == null)
        {
            GameObject titleGO = transform.Find("Title")?.gameObject;
            if (titleGO == null)
            {
                titleGO = new GameObject("Title");
                titleGO.transform.SetParent(transform, false);
                
                titleText = titleGO.AddComponent<TextMeshProUGUI>();
                titleText.text = "Информация";
                titleText.fontSize = 16;
                titleText.color = Color.black;
                titleText.fontStyle = FontStyles.Bold;
                
                RectTransform titleRect = titleGO.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 0.5f);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.offsetMin = new Vector2(80, 10);
                titleRect.offsetMax = new Vector2(-10, -10);
                
                validationErrors += "Title Text был создан автоматически. ";
            }
            else
            {
                titleText = titleGO.GetComponent<TextMeshProUGUI>();
                if (titleText == null)
                {
                    titleText = titleGO.AddComponent<TextMeshProUGUI>();
                    titleText.text = "Информация";
                    titleText.fontSize = 16;
                    titleText.color = Color.black;
                    titleText.fontStyle = FontStyles.Bold;
                    validationErrors += "Title Text компонент был добавлен автоматически. ";
                }
            }
        }
        
        // Проверяем описание
        if (descriptionText == null)
        {
            GameObject descGO = transform.Find("Description")?.gameObject;
            if (descGO == null)
            {
                descGO = new GameObject("Description");
                descGO.transform.SetParent(transform, false);
                
                descriptionText = descGO.AddComponent<TextMeshProUGUI>();
                descriptionText.text = "Описание объекта";
                descriptionText.fontSize = 12;
                descriptionText.color = Color.black;
                
                RectTransform descRect = descGO.GetComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0, 0);
                descRect.anchorMax = new Vector2(1, 0.5f);
                descRect.offsetMin = new Vector2(10, 10);
                descRect.offsetMax = new Vector2(-10, -10);
                
                validationErrors += "Description Text был создан автоматически. ";
            }
            else
            {
                descriptionText = descGO.GetComponent<TextMeshProUGUI>();
                if (descriptionText == null)
                {
                    descriptionText = descGO.AddComponent<TextMeshProUGUI>();
                    descriptionText.text = "Описание объекта";
                    descriptionText.fontSize = 12;
                    descriptionText.color = Color.black;
                    validationErrors += "Description Text компонент был добавлен автоматически. ";
                }
            }
        }
        
        // Проверяем размеры RectTransform
        if (rectTransform != null && rectTransform.sizeDelta.magnitude < 10f)
        {
            rectTransform.sizeDelta = new Vector2(300, 150);
            validationErrors += "Размер RectTransform был установлен автоматически. ";
        }
        
        // Логируем результат валидации
        if (!string.IsNullOrEmpty(validationErrors))
        {
            Debug.LogWarning($"Tooltip validation completed with auto-fixes: {validationErrors}");
            isValid = false;
        }
        else
        {
            Debug.Log("Tooltip validation passed successfully");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Проверяет, все ли необходимые компоненты присутствуют
    /// </summary>
    /// <returns>true если все компоненты на месте</returns>
    public bool IsValid()
    {
        return rectTransform != null && 
               canvasGroup != null && 
               backgroundImage != null && 
               iconImage != null && 
               titleText != null && 
               descriptionText != null;
    }
    
    public void Show(string hoveredObject = "", string selectedObject = "")
    {
        // Валидируем перед показом
        if (!IsValid())
        {
            Debug.LogWarning("Tooltip is not valid, attempting to fix...");
            ValidateTooltip();
        }
        
        currentHoveredObject = hoveredObject;
        currentSelectedObject = selectedObject;
        UpdateContent();
        if (!isVisible) { gameObject.SetActive(true); isVisible = true; }
        targetAlpha = 1f;
    }
    
    public void Hide() { isVisible = false; targetAlpha = 0f; }
    public void UpdateHoveredObject(string objectName) { currentHoveredObject = objectName; if (isVisible) UpdateContent(); }
    public void UpdateSelectedObject(string objectName) { currentSelectedObject = objectName; if (isVisible) UpdateContent(); }
    
    void UpdateContent()
    {
        if (titleText != null)
        {
            string title = "";
            if (!string.IsNullOrEmpty(currentHoveredObject))
                title += $"Наведено: {currentHoveredObject}";
            if (!string.IsNullOrEmpty(currentSelectedObject))
            {
                if (!string.IsNullOrEmpty(title)) title += "\n";
                title += $"Выбрано: {currentSelectedObject}";
            }
            if (string.IsNullOrEmpty(title)) title = "Информация";
            titleText.text = title;
        }
        
        if (descriptionText != null)
        {
            string description = "Наведите мышь на объект для получения информации";
            if (!string.IsNullOrEmpty(currentHoveredObject))
                description = $"Объект: {currentHoveredObject}\nТип: Клетка корабля\nСостояние: Активна";
            descriptionText.text = description;
        }
    }
    
    void UpdatePosition()
    {
        if (rectTransform != null)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            // Позиционируем тултип рядом с курсором
            Vector2 tooltipSize = rectTransform.sizeDelta;
            Vector2 position = mousePos + new Vector2(offsetX, offsetY);
            
            // Проверяем, не выходит ли тултип за границы экрана
            if (position.x + tooltipSize.x > screenSize.x)
                position.x = mousePos.x - tooltipSize.x - offsetX;
            if (position.y + tooltipSize.y > screenSize.y)
                position.y = mousePos.y - tooltipSize.y - offsetY;
            
            rectTransform.position = position;
        }
    }
    
    void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
    
    // Методы для будущего расширения
    public void AddButton(string text, System.Action onClick) { /* TODO */ }
    public void AddField(string label, string value) { /* TODO */ }
    public void SetIcon(Sprite sprite) { if (iconImage != null) iconImage.sprite = sprite; }
    public void SetBackgroundColor(Color color) { if (backgroundImage != null) backgroundImage.color = color; }
} 