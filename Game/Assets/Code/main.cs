using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.Networking;

public class main : MonoBehaviour
{
    public static main Instance { get; private set; }
    
    // API URLs согласно новой документации
    public static string BaseUrl = "https://renderfin.com";
    public static string PlayerUrl = "https://renderfin.com/api-game-player";
    public static string QueueUrl = "https://renderfin.com/api-game-queue";
    public static string MatchUrl = "https://renderfin.com/api-game-match";
    public static string LobbyUrl = "https://renderfin.com/api-game-lobby";
    public static string StatsUrl = "https://renderfin.com/api-game-statistics";
    public static string AdminUrl = "https://renderfin.com/api-game-admin";
    
    // Development/Production server fallback
    public static string[] PossibleServers = {
        "https://renderfin.com",
        "https://localhost:7000",
        "http://localhost:5000"
    };
    
    public static Lobby lobby;

    public static event Action<State> ChangeState;
    
    // Добавляем метод для синхронизации состояний на основе данных от сервера
    public static void SyncStateFromServer(Lobby.PlayerStatus playerStatus, bool hasActiveMatch = false)
    {
        if (Instance == null) return;
        
        State newState = Instance.currentState;
        
        switch (playerStatus)
        {
            case Lobby.PlayerStatus.Unregistered:
                newState = State.PlayerRegistration;
                break;
            case Lobby.PlayerStatus.Registering:
                newState = State.PlayerRegistration;
                break;
            case Lobby.PlayerStatus.Authenticating:
                newState = State.PlayerLogin;
                break;
            case Lobby.PlayerStatus.Idle:
                newState = State.Lobby;
                break;
            case Lobby.PlayerStatus.Searching:
                newState = State.Search;
                break;
            case Lobby.PlayerStatus.InGame:
                if (hasActiveMatch)
                {
                    newState = State.Match;
                }
                else
                {
                    newState = State.MatchFound;
                }
                break;
            case Lobby.PlayerStatus.Error:
                // Не меняем состояние при ошибке, оставляем текущее
                break;
        }
        
        // Обновляем состояние только если оно действительно изменилось
        if (newState != Instance.currentState)
        {
            Debug.Log($"[main] State synced from server: {Instance.currentState} -> {newState} (PlayerStatus: {playerStatus})");
            Instance.SetState(newState);
        }
    }

    public enum State
    {
        Server_connect,
        PlayerRegistration,
        PlayerLogin,
        Lobby,
        Search,
        MatchFound,
        MatchStarted,
        Match,
        Exit,

        Match_Result_win,
        Match_Result_lose,
        Match_Result_surrender,
        Settings_graph,
        Settings_audio,
        Settings_controls,
        Settings_language,
        Settings_credits,
        Settings_about,
        Settings_exit,

    }
    [SerializeField]private State currentState;

    public void SetState(State state)
    {
        currentState = state;
        ChangeState?.Invoke(state);
        
        switch (state)
        {
            case State.Server_connect:
                CheckServerConnection();
                break;
            case State.PlayerRegistration:
                break;
            case State.PlayerLogin:
                break;
            case State.Lobby:
                break;
        }
    }
    void CheckServerConnection()
    {
        StartCoroutine(CheckServerConnectionRoutine());
    }
    IEnumerator CheckServerConnectionRoutine()
    {
        bool serverFound = false;
        
        // Try to find an available server
        foreach (string serverUrl in PossibleServers)
        {
            Debug.Log($"[main] Trying server: {serverUrl}");
            UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/api-game-statistics/");
            
            // Bypass certificate validation for development
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            request.certificateHandler = new BypassCertificate();
            #endif
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[main] Server available: {serverUrl}");
                // Update all URLs to use this server
                UpdateServerUrls(serverUrl);
                serverFound = true;
                break;
            }
            else
            {
                Debug.LogWarning($"[main] Server unavailable: {serverUrl} - {request.error}");
            }
        }
        
        if (!serverFound)
        {
            Debug.LogError("[main] No available servers found!");
            yield break;
        }
        
        yield return new WaitForSeconds(1f);
        SetState(State.PlayerRegistration);
    }
    
    private void UpdateServerUrls(string serverUrl)
    {
        BaseUrl = serverUrl;
        PlayerUrl = serverUrl + "/api-game-player/";
        QueueUrl = serverUrl + "/api-game-queue/";
        MatchUrl = serverUrl + "/api-game-match/";
        LobbyUrl = serverUrl + "/api-game-lobby/";
        StatsUrl = serverUrl + "/api-game-statistics/";
        AdminUrl = serverUrl + "/api-game-admin/";
        
        Debug.Log($"[main] Updated server URLs to: {BaseUrl}");
    }
    
    // Certificate handler for development
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
    void Awake()
    {
        Instance = this;
        
        // Инициализируем логирование Unity
        if (FindObjectOfType<UnityLogger>() == null)
        {
            GameObject loggerGO = new GameObject("UnityLogger");
            loggerGO.AddComponent<UnityLogger>();
            DontDestroyOnLoad(loggerGO);
        }
        
        // Загружаем сцену UI в режиме Additive и после загрузки инициализируем UI
        StartCoroutine(LoadUIAndInitLobby());
    }

    IEnumerator LoadUIAndInitLobby()
    {
        var async = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        while (!async.isDone)
            yield return null;
        // Ждём, пока синглтон ui_lobby появится
        while (ui_lobby.Instance == null)
            yield return null;

        GameObject go = new GameObject("Lobby");
        main.lobby = go.AddComponent<Lobby>();
        ui_lobby.Instance.InitLobby();
        // Запускаем Lobby (если нужно)
        Lobby lobby = FindObjectOfType<Lobby>();
        if (lobby != null)
        {
            // Можно вызвать метод инициализации, если потребуется
            // lobby.Init();
        }
         SetState(State.Server_connect);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
 
