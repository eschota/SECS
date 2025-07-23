using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ui_lobby : MonoBehaviour
{
    public static ui_lobby Instance { get; private set; }
    [SerializeField] TextMeshProUGUI text_nickname;
    [SerializeField] TextMeshProUGUI game_version;
    [SerializeField] Button button_PlayNow;
    [SerializeField] Button button_Cancel;
    [SerializeField] TextMeshProUGUI button_play_now_text;
    [SerializeField] TextMeshProUGUI status_text; // Optional status display
    
    // Кнопки выбора типа матча
    [SerializeField] Button button_1v1;
    [SerializeField] Button button_2v2;
    [SerializeField] Button button_4ffa;
    [SerializeField] TextMeshProUGUI text_selected_match_type;
    
    private int _selectedMatchType = 1; // По умолчанию 1v1

    private Coroutine _searchTimerCoroutine;
    private Coroutine _statusUpdateCoroutine;

    private void OnChangeState(main.State state)
    {
        switch (state)
        {   
            case main.State.Lobby:
                InitLobby();
                break;
            case main.State.Server_connect:
                UpdateUIForServerConnection();
                break;
            case main.State.PlayerRegistration:
                UpdateUIForRegistration();
                break;
            case main.State.PlayerLogin:
                UpdateUIForLogin();
                break;
            case main.State.Search:
                break; // Handled by OnSearchStarted
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        main.ChangeState += OnChangeState;
    }

    void Start()
    {
        // Start periodic status updates
        _statusUpdateCoroutine = StartCoroutine(UpdateUIStatusPeriodically());
    }

    void OnDestroy()
    {
        main.ChangeState -= OnChangeState;
        
        if (_searchTimerCoroutine != null)
            StopCoroutine(_searchTimerCoroutine);
        
        if (_statusUpdateCoroutine != null)
            StopCoroutine(_statusUpdateCoroutine);
    }

    private void UpdateUIForServerConnection()
    {
        Debug.Log("[ui_lobby] Updating UI for server connection...");
        
        // Disable buttons during connection
        button_PlayNow.interactable = false;
        button_Cancel.interactable = false;
        button_play_now_text.text = "Connecting...";
        
        if (status_text != null)
            status_text.text = "🔄 Connecting to server...";
        
        if (text_nickname != null)
            text_nickname.text = "Connecting...";
    }

    private void UpdateUIForRegistration()
    {
        Debug.Log("[ui_lobby] Updating UI for player registration...");
        
        // Disable buttons during registration
        button_PlayNow.interactable = false;
        button_Cancel.interactable = false;
        button_play_now_text.text = "Registering...";
        
        if (status_text != null)
            status_text.text = "📝 Creating player account...";
        
        if (text_nickname != null)
            text_nickname.text = "Registering...";
    }

    private void UpdateUIForLogin()
    {
        Debug.Log("[ui_lobby] Updating UI for player login...");
        
        // Disable buttons during login
        button_PlayNow.interactable = false;
        button_Cancel.interactable = false;
        button_play_now_text.text = "Logging in...";
        
        if (status_text != null)
            status_text.text = "🔐 Authenticating...";
        
        if (text_nickname != null)
            text_nickname.text = "Logging in...";
    }

    public void InitLobby()
    {
        Debug.Log("[ui_lobby] Initializing lobby UI");
        
        // Disable all buttons initially
        if (button_PlayNow != null)
        {
            button_PlayNow.interactable = false;
            button_PlayNow.onClick.RemoveAllListeners();
            button_PlayNow.onClick.AddListener(OnPlayNowClick);
        }
        
        if (button_Cancel != null)
        {
            button_Cancel.interactable = false;
            button_Cancel.onClick.RemoveAllListeners();
            button_Cancel.onClick.AddListener(OnCancelClick);
        }
        
        // Настройка кнопок выбора типа матча
        if (button_1v1 != null)
        {
            button_1v1.onClick.RemoveAllListeners();
            button_1v1.onClick.AddListener(() => SelectMatchType(1));
        }
        
        if (button_2v2 != null)
        {
            button_2v2.onClick.RemoveAllListeners();
            button_2v2.onClick.AddListener(() => SelectMatchType(2));
        }
        
        if (button_4ffa != null)
        {
            button_4ffa.onClick.RemoveAllListeners();
            button_4ffa.onClick.AddListener(() => SelectMatchType(4));
        }
        
        // Устанавливаем начальный тип матча
        SelectMatchType(1); // По умолчанию 1v1
        
        // Check if we have a valid user and are connected to server
        if (Lobby.Instance != null && Lobby.Instance.currentUser != null && !string.IsNullOrEmpty(Lobby.Instance.currentUser.user_id))
        {
            Debug.Log($"[ui_lobby] User found: {Lobby.Instance.currentUser.nick_name}");
            
            // Update username display
            if (text_nickname != null)
            {
                text_nickname.text = Lobby.Instance.currentUser.nick_name;
            }
            
            // Update version display
            if (game_version != null)
            {
                game_version.text = "v1.0 - " + GetServerDisplayName();
            }
            
            // Now enable buttons and start status monitoring
            if (button_PlayNow != null)
            {
                button_PlayNow.interactable = true;
            }
            
            // Start periodic status monitoring
            _statusUpdateCoroutine = StartCoroutine(UpdateUIStatusPeriodically());
        }
        else
        {
            Debug.LogWarning("[ui_lobby] No valid user found or not connected to server");
            
            // Update displays to show disconnected state
            if (text_nickname != null)
            {
                text_nickname.text = "Not Connected";
            }
            
            if (game_version != null)
            {
                game_version.text = "v1.0 - Disconnected";
            }
        }
    }
    
    // Метод для выбора типа матча
    private void SelectMatchType(int matchType)
    {
        _selectedMatchType = matchType;
        
        // Обновляем визуальное отображение выбранного типа
        UpdateMatchTypeButtons();
        
        // Обновляем текст выбранного типа
        if (text_selected_match_type != null)
        {
            string typeName = GetMatchTypeName(matchType);
            text_selected_match_type.text = $"Выбран: {typeName}";
        }
        
        Debug.Log($"[ui_lobby] Selected match type: {matchType} ({GetMatchTypeName(matchType)})");
    }
    
    // Обновление визуального состояния кнопок типа матча
    private void UpdateMatchTypeButtons()
    {
        // Здесь можно добавить визуальные изменения кнопок (цвет, размер и т.д.)
        // Пока просто логируем
        Debug.Log($"[ui_lobby] Updated match type buttons, selected: {_selectedMatchType}");
    }
    
    // Вспомогательный метод для получения названия типа матча
    private string GetMatchTypeName(int matchType)
    {
        return matchType switch
        {
            1 => "1v1",
            2 => "2v2",
            4 => "1v1x1x1 (FFA)",
            _ => "Unknown"
        };
    }

    private void UpdateUIState()
    {
        if (Lobby.Instance == null)
        {
            Debug.LogWarning("[ui_lobby] Lobby instance not found, cannot update UI state");
            if (status_text != null)
                status_text.text = "❌ No connection to lobby system";
            return;
        }

        var playerStatus = Lobby.Instance.GetPlayerStatus();
        Debug.Log($"[ui_lobby] Updating UI state for player status: {playerStatus}");

        // Always update status text with current player status
        UpdatePlayerStatusText(playerStatus);

        switch (playerStatus)
        {
            case Lobby.PlayerStatus.Unregistered:
            case Lobby.PlayerStatus.Registering:
            case Lobby.PlayerStatus.Authenticating:
                // Show loading state
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(false);
                button_play_now_text.text = "Loading...";
                break;

            case Lobby.PlayerStatus.Idle:
                // Show Play Now button
                button_PlayNow.gameObject.SetActive(true);
                button_Cancel.gameObject.SetActive(false);
                button_PlayNow.interactable = true;
                button_play_now_text.text = "Play Now";
                break;

            case Lobby.PlayerStatus.Searching:
                // Show Cancel button and timer
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(true);
                button_Cancel.interactable = true;
                break;

            case Lobby.PlayerStatus.InGame:
                // Show in-game state
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(false);
                button_play_now_text.text = "In Game";
                break;

            case Lobby.PlayerStatus.Error:
                // Show error state
                button_PlayNow.gameObject.SetActive(true);
                button_Cancel.gameObject.SetActive(false);
                button_PlayNow.interactable = false;
                button_play_now_text.text = "Error";
                break;
        }
    }

    private string GetPlayerName()
    {
        if (Lobby.Instance != null && Lobby.Instance.currentUser != null && !string.IsNullOrEmpty(Lobby.Instance.currentUser.nick_name))
        {
            return Lobby.Instance.currentUser.nick_name;
        }
        return "Player";
    }

    private void UpdatePlayerStatusText(Lobby.PlayerStatus playerStatus)
    {
        if (status_text == null) return;
        
        switch (playerStatus)
        {
            case Lobby.PlayerStatus.Unregistered:
                status_text.text = "Не зарегистрирован";
                break;
            case Lobby.PlayerStatus.Registering:
                status_text.text = "Регистрация...";
                break;
            case Lobby.PlayerStatus.Authenticating:
                status_text.text = "Авторизация...";
                break;
            case Lobby.PlayerStatus.Idle:
                status_text.text = "Готов к игре";
                break;
            case Lobby.PlayerStatus.Searching:
                // Показываем выбранный тип матча при поиске
                string matchTypeName = GetMatchTypeName(_selectedMatchType);
                status_text.text = $"Поиск {matchTypeName}...";
                break;
            case Lobby.PlayerStatus.InGame:
                status_text.text = "В игре";
                break;
            case Lobby.PlayerStatus.Error:
                status_text.text = "Ошибка подключения";
                break;
            default:
                status_text.text = "Неизвестный статус";
                break;
        }
    }

    private IEnumerator UpdateUIStatusPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Update every second
            UpdateUIState();
        }
    }

    // Handle Play Now click: start search
    private void OnPlayNowClick()
    {
        Debug.Log($"[ui_lobby] Play Now button clicked for match type: {_selectedMatchType} ({GetMatchTypeName(_selectedMatchType)})");
        
        if (Lobby.Instance == null)
        {
            Debug.LogError("[ui_lobby] Lobby instance not found");
            return;
        }

        // Validate that player can join queue
        if (!Lobby.Instance.CanJoinQueue())
        {
            Debug.LogWarning($"[ui_lobby] Cannot join queue in current state: {Lobby.Instance.GetPlayerStatus()}");
            return;
        }

        // Передаем выбранный тип матча в Lobby
        Lobby.Instance.AddPlayerToQueue(_selectedMatchType);
    }

    // Handle Cancel click: stop search
    private void OnCancelClick()
    {
        Debug.Log("[ui_lobby] Cancel button clicked");
        
        if (Lobby.Instance == null)
        {
            Debug.LogError("[ui_lobby] Lobby instance not found");
            return;
        }

        // Validate that player can leave queue
        if (!Lobby.Instance.CanLeaveQueue())
        {
            Debug.LogWarning($"[ui_lobby] Cannot leave queue in current state: {Lobby.Instance.GetPlayerStatus()}");
            return;
        }

        Lobby.Instance.RemovePlayerFromQueue();
    }

    public void OnSearchStarted()
    {
        Debug.Log("[ui_lobby] Search started - updating UI");
        
        // Показываем выбранный тип матча в кнопке
        if (button_play_now_text != null)
        {
            string matchTypeName = GetMatchTypeName(_selectedMatchType);
            button_play_now_text.text = $"Поиск {matchTypeName}...";
        }
        
        // Update UI state and status text
        UpdateUIState();
        
        // Start search timer
        _searchTimerCoroutine = StartCoroutine(SearchTimerCoroutine());
    }

    public void OnSearchCanceled()
    {
        Debug.Log("[ui_lobby] Search canceled - updating UI");
        
        // Stop timer
        if (_searchTimerCoroutine != null)
        {
            StopCoroutine(_searchTimerCoroutine);
            _searchTimerCoroutine = null;
        }
        
        // Update UI state and status text
        UpdateUIState();
        
        // Force immediate status update for idle
        if (status_text != null)
        {
            status_text.text = $"✅ {GetPlayerName()} - Ready to play";
        }
    }

    public void OnMatchFound(string gameId)
    {
        Debug.Log($"[ui_lobby] Match found: {gameId}");
        
        // Stop timer
        if (_searchTimerCoroutine != null)
        {
            StopCoroutine(_searchTimerCoroutine);
            _searchTimerCoroutine = null;
        }
        
        // Force immediate status update for match found
        if (status_text != null)
        {
            status_text.text = $"🎮 {GetPlayerName()} - Match found! Loading...";
        }
        
        // Update UI state
        UpdateUIState();
        
        // Notify main state machine
        main.Instance.SetState(main.State.MatchFound);
        
        // Load match scene
        main.Instance.SetState(main.State.MatchStarted);
        
        // Load 1x1 scene additively
        StartCoroutine(LoadMatchScene());
    }

    private IEnumerator LoadMatchScene()
    {
        Debug.Log("[ui_lobby] Loading match scene...");
        
        // Wait a moment for state to settle
        yield return new WaitForSeconds(0.5f);
        
        // Load match scene
        SceneManager.LoadScene("1x1", LoadSceneMode.Additive);
    }

    private IEnumerator SearchTimerCoroutine()
    {
        float startTime = Time.time;
        
        while (true)
        {
            float elapsed = Time.time - startTime;
            TimeSpan ts = TimeSpan.FromSeconds(elapsed);
            
            // Update button text with timer
            button_play_now_text.text = string.Format("{0:00}:{1:00}", (int)ts.TotalMinutes, ts.Seconds);
            
            // Update status text with search time
            if (status_text != null)
            {
                status_text.text = $"🔍 {GetPlayerName()} - Searching... {(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
            }
            
            yield return new WaitForSeconds(1f);
        }
    }

    // Public method to force UI refresh
    public void RefreshUI()
    {
        UpdateUIState();
    }
    
    private string GetServerDisplayName()
    {
        if (main.BaseUrl.Contains("renderfin.com"))
            return "🌍 Production Server";
        else if (main.BaseUrl.Contains("localhost"))
            return "🏠 Local Server";
        else
            return "🔗 Custom Server";
    }

    // Update is called once per frame
    void Update()
    {
        // Keep UI responsive
    }
}
