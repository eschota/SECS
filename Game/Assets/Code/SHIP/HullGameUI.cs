using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HullGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private Button buildModeButton;
    [SerializeField] private Button spaceModeButton;
    [SerializeField] private Button clearHullButton;
    [SerializeField] private Button saveHullButton;
    [SerializeField] private Button loadHullButton;
    
    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI pointsCountText;
    [SerializeField] private TextMeshProUGUI wallsCountText;
    [SerializeField] private TextMeshProUGUI doorsCountText;
    
    [Header("Instructions")]
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private TextMeshProUGUI instructionsText;
    
    private HULL hullComponent;
    private HullBuilder hullBuilder;
    
    void Start()
    {
        // Находим компоненты
        hullComponent = FindObjectOfType<HULL>();
        hullBuilder = FindObjectOfType<HullBuilder>();
        
        // Настраиваем UI
        SetupUI();
        
        // Подписываемся на события
        SubscribeToEvents();
        
        // Показываем инструкции
        ShowInstructions();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void SetupUI()
    {
        // Настраиваем кнопки
        if (buildModeButton != null)
        {
            buildModeButton.onClick.AddListener(OnBuildModeClicked);
        }
        
        if (spaceModeButton != null)
        {
            spaceModeButton.onClick.AddListener(OnSpaceModeClicked);
        }
        
        if (clearHullButton != null)
        {
            clearHullButton.onClick.AddListener(OnClearHullClicked);
        }
        
        if (saveHullButton != null)
        {
            saveHullButton.onClick.AddListener(OnSaveHullClicked);
        }
        
        if (loadHullButton != null)
        {
            loadHullButton.onClick.AddListener(OnLoadHullClicked);
        }
        
        // Инициализируем статус
        UpdateStatus();
    }
    
    private void SubscribeToEvents()
    {
        if (hullComponent != null)
        {
            HULL.OnPointAdded += OnPointAdded;
            HULL.OnWallAdded += OnWallAdded;
            HULL.OnDoorAdded += OnDoorAdded;
        }
        
        if (hullBuilder != null)
        {
            HullBuilder.OnBuildingStateChanged += OnBuildingStateChanged;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (hullComponent != null)
        {
            HULL.OnPointAdded -= OnPointAdded;
            HULL.OnWallAdded -= OnWallAdded;
            HULL.OnDoorAdded -= OnDoorAdded;
        }
        
        if (hullBuilder != null)
        {
            HullBuilder.OnBuildingStateChanged -= OnBuildingStateChanged;
        }
    }
    
    private void ShowInstructions()
    {
        if (instructionsPanel != null && instructionsText != null)
        {
            instructionsText.text = 
                "🚀 СИСТЕМА СТРОИТЕЛЬСТВА КОРПУСА\n\n" +
                "🎯 УПРАВЛЕНИЕ:\n" +
                "• Нажмите 'Build Mode' для входа в режим строительства\n" +
                "• Зажмите ЛКМ и перетаскивайте для создания стен\n" +
                "• Точки автоматически привязываются к сетке 1x1м\n" +
                "• Предварительный просмотр показывает будущую позицию\n\n" +
                "🔧 ФУНКЦИИ:\n" +
                "• Clear Hull - очистить корпус\n" +
                "• Save Hull - сохранить корпус\n" +
                "• Load Hull - загрузить корпус\n\n" +
                "💡 СОВЕТЫ:\n" +
                "• Стройте по сетке для аккуратного результата\n" +
                "• Используйте предварительный просмотр для точности\n" +
                "• Сохраняйте работу регулярно";
            
            instructionsPanel.SetActive(true);
            
            // Скрываем инструкции через 10 секунд
            Invoke("HideInstructions", 10f);
        }
    }
    
    private void HideInstructions()
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }
    }
    
    // UI Event Handlers
    private void OnBuildModeClicked()
    {
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_editor_main_module_0);
            UpdateStatus();
        }
    }
    
    private void OnSpaceModeClicked()
    {
        if (SHIP_UI.Instance != null)
        {
            SHIP_UI.Instance.SetState(SHIP_UI.State._ship_state_space);
            UpdateStatus();
        }
    }
    
    private void OnClearHullClicked()
    {
        if (hullComponent != null)
        {
            // Очищаем корпус
            string emptyHull = "{\"points\":[],\"walls\":[],\"doors\":[]}";
            hullComponent.DeserializeHull(emptyHull);
            
            Debug.Log("[HullGameUI] Корпус очищен");
            UpdateStatus();
        }
    }
    
    private void OnSaveHullClicked()
    {
        if (hullComponent != null)
        {
            string hullData = hullComponent.SerializeHull();
            PlayerPrefs.SetString("SavedHull", hullData);
            PlayerPrefs.Save();
            
            Debug.Log("[HullGameUI] Корпус сохранен");
            UpdateStatus();
        }
    }
    
    private void OnLoadHullClicked()
    {
        if (hullComponent != null)
        {
            string hullData = PlayerPrefs.GetString("SavedHull", "");
            if (!string.IsNullOrEmpty(hullData))
            {
                hullComponent.DeserializeHull(hullData);
                Debug.Log("[HullGameUI] Корпус загружен");
            }
            else
            {
                Debug.LogWarning("[HullGameUI] Нет сохраненных данных корпуса");
            }
            
            UpdateStatus();
        }
    }
    
    // Event Handlers
    private void OnPointAdded(HullPoint point)
    {
        UpdateStatus();
    }
    
    private void OnWallAdded(HullWall wall)
    {
        UpdateStatus();
    }
    
    private void OnDoorAdded(HullDoor door)
    {
        UpdateStatus();
    }
    
    private void OnBuildingStateChanged(bool isActive)
    {
        UpdateStatus();
    }
    
    private void UpdateStatus()
    {
        if (statusText != null)
        {
            bool isBuilding = (hullBuilder != null && hullBuilder.IsBuildingActive);
            string status = isBuilding ? "🔨 РЕЖИМ СТРОИТЕЛЬСТВА" : "🚀 РЕЖИМ КОСМОСА";
            statusText.text = status;
        }
        
        if (pointsCountText != null && hullComponent != null)
        {
            pointsCountText.text = $"Точки: {hullComponent.points?.Count ?? 0}";
        }
        
        if (wallsCountText != null && hullComponent != null)
        {
            wallsCountText.text = $"Стены: {hullComponent.walls?.Count ?? 0}";
        }
        
        if (doorsCountText != null && hullComponent != null)
        {
            doorsCountText.text = $"Двери: {hullComponent.doors?.Count ?? 0}";
        }
    }
    
    void Update()
    {
        // Обновляем статус каждые 0.5 секунды
        if (Time.frameCount % 30 == 0)
        {
            UpdateStatus();
        }
        
        // Обработка клавиш
        HandleKeyboardInput();
    }
    
    private void HandleKeyboardInput()
    {
        // Переключение режимов по клавишам
        if (Input.GetKeyDown(KeyCode.B))
        {
            OnBuildModeClicked();
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            OnSpaceModeClicked();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            OnClearHullClicked();
        }
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            OnSaveHullClicked();
        }
        
        if (Input.GetKeyDown(KeyCode.F9))
        {
            OnLoadHullClicked();
        }
        
        // Показать/скрыть инструкции
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (instructionsPanel != null)
            {
                instructionsPanel.SetActive(!instructionsPanel.activeSelf);
            }
        }
    }
    
    // Публичные методы для внешнего доступа
    public void ShowBuildPanel()
    {
        if (buildPanel != null)
        {
            buildPanel.SetActive(true);
        }
    }
    
    public void HideBuildPanel()
    {
        if (buildPanel != null)
        {
            buildPanel.SetActive(false);
        }
    }
} 