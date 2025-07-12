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
            status_text.text = "Connecting to server...";
        
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
            status_text.text = "Creating player account...";
        
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
            status_text.text = "Authenticating...";
        
        if (text_nickname != null)
            text_nickname.text = "Logging in...";
    }

    public void InitLobby()
    {
        Debug.Log("[ui_lobby] Initializing lobby UI...");
        
        // Clear any existing listeners
        button_PlayNow.onClick.RemoveAllListeners();
        button_Cancel.onClick.RemoveAllListeners();
        
        // Add button listeners
        button_PlayNow.onClick.AddListener(OnPlayNowClick);
        button_Cancel.onClick.AddListener(OnCancelClick);
        
        // Update UI state
        UpdateUIState();
        
        // Set nickname
        string nickname = "Player";
        if (Lobby.Instance != null && Lobby.Instance.currentUser != null && !string.IsNullOrEmpty(Lobby.Instance.currentUser.nick_name))
        {
            nickname = Lobby.Instance.currentUser.nick_name;
        }
        
        if (text_nickname != null)
            text_nickname.text = nickname;
        
        // Set game version and server info
        if (game_version != null)
        {
            try
            {
                Debug.Log("[BuildInfo] Attempting to load build_info TextAsset...");
                TextAsset buildInfoAsset = Resources.Load<TextAsset>("build_info");
                string versionText = "N/A";
                
                if (buildInfoAsset != null)
                {
                    Debug.Log($"[BuildInfo] Raw build_info text: {buildInfoAsset.text}");
                    string[] parts = buildInfoAsset.text.Split('|');
                    Debug.Log($"[BuildInfo] Parsed parts count: {parts.Length}");
                    if (parts.Length == 2)
                    {
                        Debug.Log($"[BuildInfo] Setting version text: {parts[0]} ({parts[1]})");
                        versionText = $"{parts[0]} ({parts[1]})";
                    }
                    else
                    {
                        Debug.LogWarning("[BuildInfo] Unexpected format in build_info, using raw text");
                        versionText = buildInfoAsset.text;
                    }
                }
                else
                {
                    Debug.LogError("[BuildInfo] build_info TextAsset not found in Resources");
                    versionText = "N/A";
                }
                
                // Add server info
                string serverInfo = GetServerDisplayName();
                game_version.text = $"{versionText}\n{serverInfo}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildInfo] Exception while loading build info: {ex}");
                game_version.text = "Error";
            }
        }
    }

    private void UpdateUIState()
    {
        if (Lobby.Instance == null)
        {
            Debug.LogWarning("[ui_lobby] Lobby instance not found, cannot update UI state");
            return;
        }

        var playerStatus = Lobby.Instance.GetPlayerStatus();
        Debug.Log($"[ui_lobby] Updating UI state for player status: {playerStatus}");

        switch (playerStatus)
        {
            case Lobby.PlayerStatus.Unregistered:
            case Lobby.PlayerStatus.Registering:
            case Lobby.PlayerStatus.Authenticating:
                // Show loading state
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(false);
                button_play_now_text.text = "Loading...";
                if (status_text != null)
                    status_text.text = "Initializing...";
                break;

            case Lobby.PlayerStatus.Idle:
                // Show Play Now button
                button_PlayNow.gameObject.SetActive(true);
                button_Cancel.gameObject.SetActive(false);
                button_PlayNow.interactable = true;
                button_play_now_text.text = "Play Now";
                if (status_text != null)
                    status_text.text = "Ready to play";
                break;

            case Lobby.PlayerStatus.Searching:
                // Show Cancel button and timer
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(true);
                button_Cancel.interactable = true;
                if (status_text != null)
                    status_text.text = "Searching for match...";
                break;

            case Lobby.PlayerStatus.InGame:
                // Show in-game state
                button_PlayNow.gameObject.SetActive(false);
                button_Cancel.gameObject.SetActive(false);
                button_play_now_text.text = "In Game";
                if (status_text != null)
                    status_text.text = "Match in progress";
                break;

            case Lobby.PlayerStatus.Error:
                // Show error state
                button_PlayNow.gameObject.SetActive(true);
                button_Cancel.gameObject.SetActive(false);
                button_PlayNow.interactable = false;
                button_play_now_text.text = "Error";
                if (status_text != null)
                    status_text.text = "Connection error";
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
        Debug.Log("[ui_lobby] Play Now button clicked");
        
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

        Lobby.Instance.AddPlayerToQueue();
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
        
        // Update UI state
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
        
        // Update UI state
        UpdateUIState();
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
            button_play_now_text.text = string.Format("{0:00}:{1:00}", (int)ts.TotalMinutes, ts.Seconds);
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
