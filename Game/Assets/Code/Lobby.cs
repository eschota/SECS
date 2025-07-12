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

    // Player matchmaking state
    public enum PlayerStatus { 
        Unregistered,
        Registering,
        Authenticating,
        Idle, 
        Searching, 
        InGame,
        Error
    }
    private PlayerStatus _playerStatus = PlayerStatus.Unregistered;
    private Coroutine _pollCoroutine;
    private Coroutine _heartbeatCoroutine;
    private float _heartbeatInterval = 30f; // 30 seconds as per API docs
    private float _lastHeartbeatTime = 0f;

    // API Models
    [Serializable]
    public class PlayerRegistrationRequest
    {
        public string username;
        public string email;
        public string password;
        public string avatar = "https://example.com/default-avatar.png"; // Default avatar URL
    }

    [Serializable]
    public class PlayerLoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class PlayerResponse
    {
        public int id;
        public string username;
        public string email;
        public string avatar;
        public string createdAt;
        public int gamesPlayed;
        public int gamesWon;
        public int score;
        public int level;
        public int mmrOneVsOne;
        public int mmrTwoVsTwo;
        public int mmrFourPlayerFFA;
    }

    [Serializable]
    public class QueueJoinRequest
    {
        public int matchType; // 1 = 1v1, 2 = 2v2, 4 = FFA
    }

    [Serializable]
    public class QueueJoinResponse
    {
        public string message;
        public int queueType;
    }

    [Serializable]
    public class QueueStatusResponse
    {
        public bool inQueue;
        public int queueType;
        public int queueTime;
        public int currentMmrThreshold;
        public int userMmr;
        public string matchId; // ID матча если найден
        public bool matchFound; // Флаг что матч найден
    }

    [Serializable]
    public class HeartbeatRequest
    {
        public int userId;
        public string timestamp;
    }

    [Serializable]
    public class HeartbeatResponse
    {
        public bool success;
        public string timestamp;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Initialize player authentication flow
        StartCoroutine(InitializePlayer());
    }

    void Update()
    {
        // Check if heartbeat is needed
        if (_playerStatus == PlayerStatus.Idle || _playerStatus == PlayerStatus.Searching || _playerStatus == PlayerStatus.InGame)
        {
            if (Time.time - _lastHeartbeatTime > _heartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = Time.time;
            }
        }
    }

    private IEnumerator InitializePlayer()
    {
        Debug.Log("[Lobby] Initializing player authentication...");
        
        // Platform-specific user data initialization
        #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: try to get user data from cookies
            string storedUserId = GetCookieString("user_id");
            string storedEmail = GetCookieString("email");
            
            if (!string.IsNullOrEmpty(storedUserId) && !string.IsNullOrEmpty(storedEmail))
            {
                // Try to authenticate with stored credentials
                yield return StartCoroutine(AuthenticateStoredUser(storedUserId, storedEmail));
            }
            else
            {
                // Register new user
                yield return StartCoroutine(RegisterNewUser());
            }
        #elif UNITY_EDITOR
            // Editor: use persistent PlayerPrefs for development
            string storedUserId = PlayerPrefs.GetString("dev_user_id", "");
            string storedEmail = PlayerPrefs.GetString("dev_email", "");
            
            if (!string.IsNullOrEmpty(storedUserId) && !string.IsNullOrEmpty(storedEmail))
            {
                // Try to authenticate with stored credentials
                yield return StartCoroutine(AuthenticateStoredUser(storedUserId, storedEmail));
            }
            else
            {
                // Register new developer user
                yield return StartCoroutine(RegisterDeveloperUser());
            }
        #endif

        // Start heartbeat system if authenticated
        if (_playerStatus == PlayerStatus.Idle)
        {
            StartHeartbeatSystem();
            // Try to resume any existing queue state
            yield return StartCoroutine(CheckExistingQueueState());
        }
    }

    private IEnumerator RegisterDeveloperUser()
    {
        Debug.Log("[Lobby] Registering developer user...");
        _playerStatus = PlayerStatus.Registering;
        
        string devGuid = Guid.NewGuid().ToString();
        string devUsername = $"Developer account {devGuid}";
        string devEmail = $"dev_{devGuid}@local.dev";
        string devPassword = "dev123";

        var registerRequest = new PlayerRegistrationRequest
        {
            username = devUsername,
            email = devEmail,
            password = devPassword,
            avatar = "https://example.com/default-avatar.png"
        };

        string jsonPayload = JsonUtility.ToJson(registerRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(main.PlayerUrl, "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Developer user registered successfully. Response: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<PlayerResponse>(request.downloadHandler.text);
            currentUser = new User
            {
                user_id = response.id.ToString(),
                nick_name = response.username,
                email = response.email,
                avatar_url = response.avatar
            };

            // Store credentials for future use
            PlayerPrefs.SetString("dev_user_id", currentUser.user_id);
            PlayerPrefs.SetString("dev_email", currentUser.email);
            PlayerPrefs.SetString("dev_username", currentUser.nick_name);
            PlayerPrefs.Save();

            _playerStatus = PlayerStatus.Idle;
            main.Instance.SetState(main.State.Lobby);
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to register developer user. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Error;
        }
    }

    private IEnumerator RegisterNewUser()
    {
        Debug.Log("[Lobby] Registering new user...");
        _playerStatus = PlayerStatus.Registering;
        
        // For WebGL, create a guest user
        string guestGuid = Guid.NewGuid().ToString();
        string guestUsername = $"Guest_{guestGuid.Substring(0, 8)}";
        string guestEmail = $"guest_{guestGuid}@guest.com";
        string guestPassword = "guest123";

        var registerRequest = new PlayerRegistrationRequest
        {
            username = guestUsername,
            email = guestEmail,
            password = guestPassword,
            avatar = "https://example.com/default-avatar.png"
        };

        string jsonPayload = JsonUtility.ToJson(registerRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(main.PlayerUrl, "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Guest user registered successfully. Response: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<PlayerResponse>(request.downloadHandler.text);
            currentUser = new User
            {
                user_id = response.id.ToString(),
                nick_name = response.username,
                email = response.email,
                avatar_url = response.avatar
            };

            #if UNITY_WEBGL && !UNITY_EDITOR
            // Store in cookies for WebGL
            // Note: This would require JavaScript plugin implementation
            #endif

            _playerStatus = PlayerStatus.Idle;
            main.Instance.SetState(main.State.Lobby);
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to register guest user. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Error;
        }
    }

    private IEnumerator AuthenticateStoredUser(string userId, string email)
    {
        Debug.Log($"[Lobby] Authenticating stored user: {userId}, {email}");
        _playerStatus = PlayerStatus.Authenticating;
        
        // For now, just create user object from stored data
        // In a real implementation, you'd want to validate with server
        currentUser = new User
        {
            user_id = userId,
            nick_name = PlayerPrefs.GetString("dev_username", "Developer"),
            email = email,
            avatar_url = "https://example.com/default-avatar.png"
        };

        _playerStatus = PlayerStatus.Idle;
        main.Instance.SetState(main.State.Lobby);
        yield return null;
    }

    private void StartHeartbeatSystem()
    {
        Debug.Log("[Lobby] Starting heartbeat system...");
        _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        _lastHeartbeatTime = Time.time;
    }

    private IEnumerator HeartbeatLoop()
    {
        while (_playerStatus != PlayerStatus.Unregistered && _playerStatus != PlayerStatus.Error)
        {
            yield return new WaitForSeconds(_heartbeatInterval);
            SendHeartbeat();
        }
    }

    private void SendHeartbeat()
    {
        if (currentUser == null || string.IsNullOrEmpty(currentUser.user_id))
            return;

        StartCoroutine(SendHeartbeatCoroutine());
    }

    private IEnumerator SendHeartbeatCoroutine()
    {
        var heartbeatRequest = new HeartbeatRequest
        {
            userId = int.Parse(currentUser.user_id),
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        string jsonPayload = JsonUtility.ToJson(heartbeatRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(main.PlayerUrl + "heartbeat", "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Heartbeat sent successfully. Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogWarning($"[Lobby] Heartbeat failed. Error: {request.error}. Response: {request.downloadHandler?.text}");
        }
    }

    // Start searching for a match
    public void AddPlayerToQueue()
    {
        Debug.Log($"[Lobby] AddPlayerToQueue invoked. Current status: {_playerStatus}");
        
        // Validate player status before joining queue
        if (_playerStatus == PlayerStatus.Searching)
        {
            Debug.LogWarning("[Lobby] Player is already searching for a match");
            return;
        }
        
        if (_playerStatus == PlayerStatus.InGame)
        {
            Debug.LogWarning("[Lobby] Player is already in a game");
            return;
        }
        
        if (_playerStatus != PlayerStatus.Idle)
        {
            Debug.LogWarning($"[Lobby] Player status is not idle: {_playerStatus}");
            return;
        }

        StartCoroutine(JoinQueueCoroutine());
    }

    private IEnumerator JoinQueueCoroutine()
    {
        Debug.Log("[Lobby] Joining queue...");
        _playerStatus = PlayerStatus.Searching;
        
        var queueRequest = new QueueJoinRequest
        {
            matchType = 1 // 1v1 for now
        };

        string jsonPayload = JsonUtility.ToJson(queueRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(main.QueueUrl + currentUser.user_id + "/join", "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Successfully joined queue. Response: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<QueueJoinResponse>(request.downloadHandler.text);
            
            // Notify UI
            ui_lobby.Instance.OnSearchStarted();
            main.Instance.SetState(main.State.Search);
            
            // Start polling queue status
            _pollCoroutine = StartCoroutine(PollQueueStatus());
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to join queue. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Idle;
            ui_lobby.Instance.OnSearchCanceled();
        }
    }

    // Cancel searching or leave queue
    public void RemovePlayerFromQueue()
    {
        Debug.Log($"[Lobby] RemovePlayerFromQueue invoked. Current status: {_playerStatus}");
        
        if (_playerStatus != PlayerStatus.Searching)
        {
            Debug.LogWarning("[Lobby] Player is not currently searching");
            return;
        }

        StartCoroutine(LeaveQueueCoroutine());
    }

    private IEnumerator LeaveQueueCoroutine()
    {
        Debug.Log("[Lobby] Leaving queue...");
        
        // Stop polling
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }

        UnityWebRequest request = new UnityWebRequest(main.QueueUrl + currentUser.user_id + "/leave", "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Successfully left queue. Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to leave queue. Error: {request.error}. Response: {request.downloadHandler?.text}");
        }

        _playerStatus = PlayerStatus.Idle;
        ui_lobby.Instance.OnSearchCanceled();
    }

    private IEnumerator PollQueueStatus()
    {
        float pollInterval = 2f; // Poll every 2 seconds
        float timeoutDuration = 1800f; // 30 minutes timeout
        float startTime = Time.time;

        while (_playerStatus == PlayerStatus.Searching)
        {
            UnityWebRequest request = UnityWebRequest.Get(main.QueueUrl + currentUser.user_id + "/status");
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            request.certificateHandler = new BypassCertificate();
            #endif
            
            yield return request.SendWebRequest();

                         if (request.result == UnityWebRequest.Result.Success)
             {
                 Debug.Log($"[Lobby] Queue status poll successful. Response: {request.downloadHandler.text}");
                 
                 try
                 {
                     var response = JsonUtility.FromJson<QueueStatusResponse>(request.downloadHandler.text);
                     
                     if (!response.inQueue)
                     {
                         // Player is no longer in queue - check why
                         Debug.LogWarning("[Lobby] Player left queue. Checking for match...");
                         
                         // For now, assume player left queue without finding match
                         // In the future, we can check for additional match info from server
                         Debug.LogWarning("[Lobby] Player left queue without finding match - returning to idle state");
                         _playerStatus = PlayerStatus.Idle;
                         ui_lobby.Instance.OnSearchCanceled();
                         yield break;
                         
                         // TODO: Add proper match detection when server supports it
                         /*
                         if (response.matchFound && !string.IsNullOrEmpty(response.matchId))
                         {
                             // Match actually found!
                             Debug.Log($"[Lobby] Match found! Match ID: {response.matchId}");
                             _playerStatus = PlayerStatus.InGame;
                             main.Instance.SetState(main.State.MatchFound);
                             ui_lobby.Instance.OnMatchFound(response.matchId);
                             yield break;
                         }
                         else
                         {
                             // Player left queue for other reasons (timeout, error, etc.)
                             Debug.LogWarning("[Lobby] Player left queue without finding match - returning to idle state");
                             _playerStatus = PlayerStatus.Idle;
                             ui_lobby.Instance.OnSearchCanceled();
                             yield break;
                         }
                         */
                     }
                     else
                     {
                         // Still in queue - show current stats
                         Debug.Log($"[Lobby] Still in queue - Time: {response.queueTime}s, MMR: {response.userMmr}, Threshold: {response.currentMmrThreshold}");
                     }
                 }
                 catch (System.Exception ex)
                 {
                     Debug.LogError($"[Lobby] Failed to parse queue status response: {ex.Message}. Response: {request.downloadHandler.text}");
                 }
             }
            else
            {
                Debug.LogError($"[Lobby] Queue status poll failed. Error: {request.error}. Response: {request.downloadHandler?.text}");
            }

            // Check timeout
            if (Time.time - startTime > timeoutDuration)
            {
                Debug.LogWarning("[Lobby] Queue timeout reached, leaving queue...");
                RemovePlayerFromQueue();
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator CheckExistingQueueState()
    {
        Debug.Log("[Lobby] Checking existing queue state...");
        
        UnityWebRequest request = UnityWebRequest.Get(main.QueueUrl + currentUser.user_id + "/status");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] Queue state check successful. Response: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<QueueStatusResponse>(request.downloadHandler.text);
            
            if (response.inQueue)
            {
                Debug.Log("[Lobby] Resuming existing queue state...");
                _playerStatus = PlayerStatus.Searching;
                ui_lobby.Instance.OnSearchStarted();
                main.Instance.SetState(main.State.Search);
                _pollCoroutine = StartCoroutine(PollQueueStatus());
            }
        }
        else
        {
            Debug.LogError($"[Lobby] Queue state check failed. Error: {request.error}. Response: {request.downloadHandler?.text}");
        }
    }

    void OnDestroy()
    {
        if (_pollCoroutine != null)
            StopCoroutine(_pollCoroutine);
        
        if (_heartbeatCoroutine != null)
            StopCoroutine(_heartbeatCoroutine);
    }

    // Public getters for UI
    public PlayerStatus GetPlayerStatus() => _playerStatus;
    public bool CanJoinQueue() => _playerStatus == PlayerStatus.Idle;
    public bool CanLeaveQueue() => _playerStatus == PlayerStatus.Searching;
    public bool IsInGame() => _playerStatus == PlayerStatus.InGame;

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
