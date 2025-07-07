using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;
using System;

public class Lobby : MonoBehaviour
{
    // Certificate handler to bypass SSL certificate validation in Editor/Dev
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    public static Lobby Instance { get; private set; }
    private static Lobby _instance;

    // Player matchmaking state
    private enum PlayerState { Idle, Searching, InGame }
    private PlayerState _state = PlayerState.Idle;
    private Coroutine _pollCoroutine;
    // Use full domain for WebGL API requests
 

    [Serializable]
    private class UserIdData { public string user_id; }
    [Serializable]
    private class GameInfo { public string game_id; public string[] players; }
    [Serializable]
    private class QueueResponse { public string status; public GameInfo game; }
    [Serializable]
    private class StateResponse { public string status; public string state; public GameInfo game; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Platform-specific initialization of user data
        #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: read from cookies
            currentUser = new User();
            currentUser.user_id = GetCookieString("user_id");
            currentUser.nick_name = GetCookieString("nick_name");
            currentUser.avatar_url = GetCookieString("avatar_url");
            currentUser.email = GetCookieString("email");
            Debug.Log($"[Lobby] User from cookie: {currentUser.user_id} {currentUser.nick_name} {currentUser.email} {currentUser.avatar_url}");
        #elif UNITY_EDITOR
            // Editor: persistent PlayerPrefs storage for development only
            currentUser = new User();
            if (PlayerPrefs.HasKey("user_id") && PlayerPrefs.HasKey("nick_name"))
            {
                currentUser.user_id = PlayerPrefs.GetString("user_id");
                currentUser.nick_name = PlayerPrefs.GetString("nick_name");
                currentUser.avatar_url = PlayerPrefs.GetString("avatar_url", "");
                currentUser.email = PlayerPrefs.GetString("email", "");
            }
            else
            {
                currentUser.user_id = Guid.NewGuid().ToString();
                currentUser.nick_name = Environment.GetEnvironmentVariable("USERNAME") ?? "EditorPlayer";
                currentUser.avatar_url = string.Empty;
                currentUser.email = string.Empty;
                PlayerPrefs.SetString("user_id", currentUser.user_id);
                PlayerPrefs.SetString("nick_name", currentUser.nick_name);
                PlayerPrefs.SetString("avatar_url", currentUser.avatar_url);
                PlayerPrefs.SetString("email", currentUser.email);
                PlayerPrefs.Save();
            }
            Debug.Log($"[Lobby Editor] Loaded user: {currentUser.user_id} {currentUser.nick_name}");
        #endif
        // Initialize lobby UI with loaded user data
        if (ui_lobby.Instance != null)
        {
            ui_lobby.Instance.InitLobby();
            // Resume queue or game state after initialization
            TryResumeQueue();
            // If player not in game yet, ensure 1x1 scene is unloaded
            var scene = SceneManager.GetSceneByName("1x1");
            if (scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync("1x1");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Start searching for a match
    public void AddPlayerToQueue()
    {
        Debug.Log($"[Lobby] AddPlayerToQueue invoked. Current state: {_state}");
        if (_state == PlayerState.Idle)
        {
            StartCoroutine(CoAddPlayerToQueue());
        }
    }

    // Cancel searching or leave queue
    public void RemovePlayerFromQueue()
    {
        if (_state == PlayerState.Searching)
        {
            if (_pollCoroutine != null)
                StopCoroutine(_pollCoroutine);
            _state = PlayerState.Idle;
            StartCoroutine(CoRemovePlayer());
        }
    }

    // Coroutine to add player and handle immediate match
    private IEnumerator CoAddPlayerToQueue()
    {
        main.Instance.SetState(main.State.Search);
        Debug.Log("[Lobby] CoAddPlayerToQueue started");
        // Logging URL and payload
        var payloadObj = new UserIdData { user_id = currentUser.user_id };
        string payloadLog = JsonUtility.ToJson(payloadObj);
        Debug.Log($"[Lobby] Sending POST to {main.QueueUrl} with payload: {payloadLog}");
        _state = PlayerState.Searching;
        // Notify UI
        ui_lobby.Instance.OnSearchStarted();
        // Prepare JSON payload
        string payload = payloadLog;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        UnityWebRequest uwr = new UnityWebRequest(main.QueueUrl, "POST");
        // Bypass certificate validation in Editor and Development builds
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        uwr.certificateHandler = new BypassCertificate();
        #endif
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Lobby] Add to queue failed. Code: {uwr.responseCode}. Error: {uwr.error}. Body: {uwr.downloadHandler.text}");
            _state = PlayerState.Idle;
            ui_lobby.Instance.OnSearchCanceled();
            yield break;
        }
        Debug.Log($"[Lobby] Add to queue success. Code: {uwr.responseCode}. Body: {uwr.downloadHandler.text}");
        var resp = JsonUtility.FromJson<QueueResponse>(uwr.downloadHandler.text);
        if (resp.status == "matched")
        {
            _state = PlayerState.InGame;
            main.Instance.SetState(main.State.MatchFound);
            ui_lobby.Instance.OnMatchFound(resp.game.game_id);
        }
        else
        {
            // Still queued, start polling
            _pollCoroutine = StartCoroutine(PollState());
        }
    }

    // Poll player state until matched or cancelled
    private IEnumerator PollState()
    {
        float startTime = Time.realtimeSinceStartup;
        while (_state == PlayerState.Searching)
        {
            UnityWebRequest uwr = UnityWebRequest.Get(main.QueueUrl + "state/" + currentUser.user_id);
            // Bypass certificate validation in Editor and Development builds
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            uwr.certificateHandler = new BypassCertificate();
            #endif
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Lobby] Poll state success. Code: {uwr.responseCode}. Body: {uwr.downloadHandler.text}");
                var resp = JsonUtility.FromJson<StateResponse>(uwr.downloadHandler.text);
                if (resp.state == "in_game")
                {
                    _state = PlayerState.InGame;
                    main.Instance.SetState(main.State.MatchFound);
                    ui_lobby.Instance.OnMatchFound(resp.game.game_id);
                    yield break;
                }
                else if (resp.state == "idle")
                {
                    // Cancelled or timed out
                    ui_lobby.Instance.OnSearchCanceled();
                    _state = PlayerState.Idle;
                    yield break;
                }
            }
            else
            {
                Debug.LogError($"[Lobby] Poll state failed. Code: {uwr.responseCode}. Error: {uwr.error}. Body: {uwr.downloadHandler.text}");
            }
            // Timeout after 30 minutes
            if (Time.realtimeSinceStartup - startTime > 1800f)
            {
                RemovePlayerFromQueue();
                yield break;
            }
            yield return new WaitForSeconds(2f);
        }
    }

    // Coroutine to remove player from queue
    private IEnumerator CoRemovePlayer()
    {
        var payloadObj = new UserIdData { user_id = currentUser.user_id };
        string payload = JsonUtility.ToJson(payloadObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        UnityWebRequest uwr = new UnityWebRequest(main.QueueUrl, "DELETE");
        // Bypass certificate validation in Editor and Development builds
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        uwr.certificateHandler = new BypassCertificate();
        #endif
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
            Debug.LogError($"[Lobby] Remove from queue failed. Code: {uwr.responseCode}. Error: {uwr.error}. Body: {uwr.downloadHandler.text}");
        else
            Debug.Log($"[Lobby] Remove from queue response. Code: {uwr.responseCode}. Body: {uwr.downloadHandler.text}");
    }

    // Check existing queue state on init (resume queue after restart)
    public void TryResumeQueue()
    {
        StartCoroutine(CoResumeQueue());
    }
    private IEnumerator CoResumeQueue()
    {
        
        // Check state from server
        UnityWebRequest uwr = UnityWebRequest.Get(main.QueueUrl + "state/" + currentUser.user_id);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        uwr.certificateHandler = new BypassCertificate();
        #endif
        yield return uwr.SendWebRequest();
        if (uwr.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<StateResponse>(uwr.downloadHandler.text);
            if (resp.state == "in_queue")
            {
                _state = PlayerState.Searching;
                ui_lobby.Instance.OnSearchStarted();
                _pollCoroutine = StartCoroutine(PollState());
            }
            else if (resp.state == "in_game")
            {
                _state = PlayerState.InGame;
                main.Instance.SetState(main.State.MatchFound);
                if (resp.game != null)
                    ui_lobby.Instance.OnMatchFound(resp.game.game_id);
            }
        }
        else
        {
            Debug.LogError($"[Lobby] Resume queue failed. Code: {uwr.responseCode}. Error: {uwr.error}. Body: {uwr.downloadHandler?.text}");
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GetCookie(string name, System.IntPtr buffer, int bufferSize);

    private string GetCookieString(string name)
    {
        const int bufferSize = 256;
        System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufferSize);
        try
        {
            GetCookie(name, buffer, bufferSize);
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(buffer);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }
    }
#else
    private string GetCookieString(string name) => "";
    #endif

    public class User
    {
        public string user_id;
        public string nick_name;
        public string avatar_url;
        public string email;
    }

    public User currentUser;
}
