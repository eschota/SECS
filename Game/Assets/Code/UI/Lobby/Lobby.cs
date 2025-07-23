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
    private int _currentMatchType = 1; // Текущий тип матча для поиска
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
    public class GameMatchResponse
    {
        public int matchId;
        public int matchType;
        public int[] playersId;
        public int[] teamId;
        public string startTime;
        public string endTime;
        public int status;
        public float matchMaxTimeLimit;
        public int[] winnersList;
        public int[] losersList;
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
        main.SyncStateFromServer(_playerStatus);
        
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
            main.SyncStateFromServer(_playerStatus);
            main.Instance.SetState(main.State.Lobby);
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to register developer user. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Error;
            main.SyncStateFromServer(_playerStatus);
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
            main.SyncStateFromServer(_playerStatus);
            main.Instance.SetState(main.State.Lobby);
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to register guest user. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Error;
            main.SyncStateFromServer(_playerStatus);
        }
    }

    private IEnumerator AuthenticateStoredUser(string userId, string email)
    {
        Debug.Log($"[Lobby] Authenticating stored user: {userId}, {email}");
        _playerStatus = PlayerStatus.Authenticating;
        main.SyncStateFromServer(_playerStatus);
        
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
        main.SyncStateFromServer(_playerStatus);
        main.Instance.SetState(main.State.Lobby);
        yield return null;
    }

    private void StartHeartbeatSystem()
    {
        Debug.Log("[Lobby] Starting heartbeat system...");
        // Отправляем первый heartbeat сразу
        SendHeartbeat();
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
        // Проверяем что пользователь инициализирован
        if (currentUser == null || string.IsNullOrEmpty(currentUser.user_id))
        {
            Debug.LogError("[Lobby] ❌ Cannot send heartbeat: currentUser or user_id is null/empty");
            _playerStatus = PlayerStatus.Error;
            main.SyncStateFromServer(_playerStatus);
            yield break;
        }
        
        // New heartbeat system: simply check player status (automatically updates heartbeat on server)
        string requestUrl = main.PlayerUrl +  currentUser.user_id;
        Debug.Log($"[Lobby] 💓 Sending heartbeat to: {requestUrl}");
        
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif

        yield return request.SendWebRequest();

        bool shouldCheckMatchStatus = false;
        
        Debug.Log($"[Lobby] 📡 Heartbeat response - Result: {request.result}, Code: {request.responseCode}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] ✅ Heartbeat successful. Response: {request.downloadHandler.text}");
            
            try
            {
                var playerResponse = JsonUtility.FromJson<PlayerResponse>(request.downloadHandler.text);
                
                // Update local player data with fresh info from server
                currentUser.nick_name = playerResponse.username;
                
                Debug.Log($"[Lobby] ✅ Player status updated via heartbeat. Games played: {playerResponse.gamesPlayed}, Score: {playerResponse.score}");
                
                // Если мы были в Error состоянии, но сервер отвечает нормально - восстанавливаем состояние
                if (_playerStatus == PlayerStatus.Error)
                {
                    Debug.Log("[Lobby] 🔄 Recovering from Error state - server is responsive");
                    _playerStatus = PlayerStatus.Idle;
                    main.SyncStateFromServer(_playerStatus);
                }
                
                // Флаг что нужно проверить статус матча
                shouldCheckMatchStatus = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] ❌ Failed to parse heartbeat response: {ex.Message}. Raw response: {request.downloadHandler.text}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
            }
        }
        else
        {
            Debug.LogError($"[Lobby] ❌ Heartbeat failed - Result: {request.result}, Code: {request.responseCode}, Error: {request.error}");
            Debug.LogError($"[Lobby] ❌ Response text: {request.downloadHandler?.text}");
            
            // 🔧 Автоматическое восстановление: если пользователь не найден на сервере
            if (TryAutoRecoverUserNotFound(request, "during heartbeat"))
            {
                yield break; // Выходим из текущего heartbeat
            }
            
            // Более мягкая обработка ошибок - устанавливаем Error только если это серьезная проблема
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[Lobby] ❌ Setting Error status due to {request.result}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
            }
            else
            {
                Debug.LogWarning($"[Lobby] ⚠️ Heartbeat failed but not setting Error status. Result: {request.result}");
            }
        }
        
        // Проверяем статус матча вне try-catch блока
        if (shouldCheckMatchStatus)
        {
            Debug.Log("[Lobby] 🔍 Checking match status after successful heartbeat");
            yield return CheckPlayerMatchStatus();
        }
        else
        {
            Debug.LogWarning("[Lobby] ⚠️ Skipping match status check due to heartbeat failure");
        }
    }

    // Проверяем статус матча игрока для синхронизации состояний
    private IEnumerator CheckPlayerMatchStatus()
    {
        // Проверяем что пользователь инициализирован
        if (currentUser == null || string.IsNullOrEmpty(currentUser.user_id))
        {
            Debug.LogError("[Lobby] ❌ Cannot check match status: currentUser or user_id is null/empty");
            _playerStatus = PlayerStatus.Error;
            main.SyncStateFromServer(_playerStatus);
            yield break;
        }
        
        string matchUrl = main.MatchUrl +currentUser.user_id;
        Debug.Log($"[Lobby] 🔍 Checking match status at: {matchUrl}");
        
        UnityWebRequest matchRequest = UnityWebRequest.Get(matchUrl);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        matchRequest.certificateHandler = new BypassCertificate();
        #endif
        
        yield return matchRequest.SendWebRequest();

        Debug.Log($"[Lobby] 📡 Match status response - Result: {matchRequest.result}, Code: {matchRequest.responseCode}");

        if (matchRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] ✅ Match status check successful. Response: {matchRequest.downloadHandler.text}");
            
            try
            {
                var matchData = matchRequest.downloadHandler.text;
                
                if (string.IsNullOrEmpty(matchData) || matchData == "[]" || matchData == "null")
                {
                    Debug.Log("[Lobby] ✅ No active matches found - setting status to Idle");
                    _playerStatus = PlayerStatus.Idle;
                    main.SyncStateFromServer(_playerStatus, false);
                }
                else
                {
                    Debug.Log($"[Lobby] 🎯 Active match found - parsing match data: {matchData}");
                    
                    // Parse the JSON array - server returns GameMatch objects
                    if (matchData.StartsWith("[") && matchData.EndsWith("]"))
                    {
                        // Remove brackets and parse first match
                        var cleanData = matchData.Substring(1, matchData.Length - 2);
                        if (!string.IsNullOrEmpty(cleanData))
                        {
                            try
                            {
                                // Parse as GameMatch object from server
                                var gameMatch = JsonUtility.FromJson<GameMatchResponse>(cleanData);
                                
                                // Проверяем не только ID, но и статус матча
                                // Status: 0 = InProgress, 1 = Completed, 2 = Cancelled
                                bool hasActiveMatch = gameMatch.matchId > 0 && gameMatch.status == 0;
                                
                                Debug.Log($"[Lobby] 🎯 Match analysis - MatchId: {gameMatch.matchId}, Status: {gameMatch.status}, HasActiveMatch: {hasActiveMatch}");
                                
                                if (hasActiveMatch)
                                {
                                    _playerStatus = PlayerStatus.InGame;
                                    Debug.Log("[Lobby] ✅ Player is in active match - setting status to InGame");
                                    
                                    // Load game scene with match data
                                    main.SyncStateFromServer(_playerStatus, hasActiveMatch);
                                    ui_lobby.Instance.OnMatchFound(gameMatch.matchId.ToString());
                                    
                                    // Load game scene
                                    Debug.Log($"[Lobby] 🎮 Loading game scene for match {gameMatch.matchId}");
                                    UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                                }
                                else
                                {
                                    _playerStatus = PlayerStatus.Idle;
                                    if (gameMatch.status == 1)
                                    {
                                        Debug.Log($"[Lobby] ✅ Match {gameMatch.matchId} is completed - setting status to Idle");
                                    }
                                    else if (gameMatch.status == 2)
                                    {
                                        Debug.Log($"[Lobby] ✅ Match {gameMatch.matchId} is cancelled - setting status to Idle");
                                    }
                                    else
                                    {
                                        Debug.Log($"[Lobby] ✅ Match {gameMatch.matchId} has unknown status {gameMatch.status} - setting status to Idle");
                                    }
                                    main.SyncStateFromServer(_playerStatus, hasActiveMatch);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[Lobby] ❌ Failed to parse GameMatch: {ex.Message}. Data: {cleanData}");
                                _playerStatus = PlayerStatus.Idle;
                                main.SyncStateFromServer(_playerStatus, false);
                            }
                        }
                        else
                        {
                            Debug.Log("[Lobby] ✅ Empty match data - setting status to Idle");
                            _playerStatus = PlayerStatus.Idle;
                            main.SyncStateFromServer(_playerStatus, false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Lobby] ⚠️ Unexpected match data format: {matchData}");
                        _playerStatus = PlayerStatus.Idle;
                        main.SyncStateFromServer(_playerStatus, false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] ❌ Failed to parse match status response: {ex.Message}. Raw response: {matchRequest.downloadHandler.text}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
            }
        }
        else
        {
            Debug.LogError($"[Lobby] ❌ Match status check failed - Result: {matchRequest.result}, Code: {matchRequest.responseCode}, Error: {matchRequest.error}");
            Debug.LogError($"[Lobby] ❌ Match response text: {matchRequest.downloadHandler?.text}");
            
            // 🔧 Автоматическое восстановление: если пользователь не найден на сервере
            if (TryAutoRecoverUserNotFound(matchRequest, "during match check"))
            {
                yield break; // Выходим из текущего метода
            }
            
            // Более мягкая обработка ошибок для проверки матча
            if (matchRequest.result == UnityWebRequest.Result.ConnectionError || 
                matchRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[Lobby] ❌ Setting Error status due to match check {matchRequest.result}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
            }
            else
            {
                Debug.LogWarning($"[Lobby] ⚠️ Match check failed but not setting Error status. Assuming Idle.");
                _playerStatus = PlayerStatus.Idle;
                main.SyncStateFromServer(_playerStatus, false);
            }
        }
    }

    // Backwards compatibility method - defaults to 1v1
    public void AddPlayerToQueue()
    {
        AddPlayerToQueue(1); // Default to 1v1 match type
    }

    // Start searching for a match
    public void AddPlayerToQueue(int matchType)
    {
        Debug.Log($"[Lobby] Adding player to queue for match type: {matchType} ({GetMatchTypeName(matchType)})");
        
        // Store the match type for UI display
        _currentMatchType = matchType;
        
        // Validate player status before joining queue
        if (currentUser == null)
        {
            Debug.LogError("[Lobby] Cannot join queue - no user logged in");
            return;
        }
        
        if (string.IsNullOrEmpty(currentUser.user_id))
        {
            Debug.LogError("[Lobby] Cannot join queue - invalid user ID");
            return;
        }
        
        if (_playerStatus != PlayerStatus.Idle)
        {
            Debug.LogWarning($"[Lobby] Player status is not idle: {_playerStatus}");
            return;
        }

        StartCoroutine(JoinQueueCoroutine(matchType));
    }

    private IEnumerator JoinQueueCoroutine(int matchType)
    {
        Debug.Log("[Lobby] Joining queue...");
        _playerStatus = PlayerStatus.Searching;
        main.SyncStateFromServer(_playerStatus);
        
        var queueRequest = new QueueJoinRequest
        {
            matchType = matchType
        };

        // Request body with match type (according to server API)
        string jsonData = $"{{\"MatchType\": {matchType}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        Debug.Log($"[Lobby] Joining queue for match type: {matchType} ({GetMatchTypeName(matchType)})");

        // Fix URL: use correct join endpoint
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
            Debug.Log($"[Lobby] Successfully joined queue for {GetMatchTypeName(matchType)}. Response: {request.downloadHandler.text}");
            
            var response = JsonUtility.FromJson<QueueJoinResponse>(request.downloadHandler.text);
            
            // Notify UI
            ui_lobby.Instance.OnSearchStarted();
            main.Instance.SetState(main.State.Search);
            
            // Start polling queue status
            _pollCoroutine = StartCoroutine(PollQueueStatus());
        }
        else
        {
            Debug.LogError($"[Lobby] Failed to join queue for {GetMatchTypeName(matchType)}. Error: {request.error}. Response: {request.downloadHandler?.text}");
            _playerStatus = PlayerStatus.Idle;
            main.SyncStateFromServer(_playerStatus);
            ui_lobby.Instance.OnSearchCanceled();
        }
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

        // Fix URL: use correct leave endpoint (POST method, not DELETE)
        UnityWebRequest request = new UnityWebRequest(main.QueueUrl + currentUser.user_id + "/leave", "POST");
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        request.downloadHandler = new DownloadHandlerBuffer();

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
        main.SyncStateFromServer(_playerStatus);
        ui_lobby.Instance.OnSearchCanceled();
    }

    private IEnumerator PollQueueStatus()
    {
        float pollInterval = 2f; // Poll every 2 seconds
        float timeoutDuration = 18000f; // 5 hours timeout (увеличено в 10 раз)
        float startTime = Time.time;

        while (_playerStatus == PlayerStatus.Searching)
        {
            // Fix URL: use correct player status endpoint
            UnityWebRequest request = UnityWebRequest.Get(main.QueueUrl + currentUser.user_id + "/status");
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            request.certificateHandler = new BypassCertificate();
            #endif
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Lobby] Queue status poll successful. Response: {request.downloadHandler.text}");
                
                // Parse response outside try-catch to avoid yield in try block
                QueueStatusResponse response = null;
                bool parseSuccess = false;
                
                try
                {
                    response = JsonUtility.FromJson<QueueStatusResponse>(request.downloadHandler.text);
                    parseSuccess = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Lobby] Failed to parse queue status response: {ex.Message}. Response: {request.downloadHandler.text}");
                    parseSuccess = false;
                }
                
                // Handle parsed response (outside try-catch so we can use yield)
                if (parseSuccess && response != null)
                {
                    if (!response.inQueue)
                    {
                        // Player is no longer in queue - check player status to determine why
                        Debug.LogWarning("[Lobby] Player left queue. Checking player status for match...");
                        
                        // Check player status to see if they're in a match
                        yield return StartCoroutine(CheckPlayerStatusForMatch());
                        yield break;
                    }
                    else
                    {
                        // Still in queue - show current stats
                        Debug.Log($"[Lobby] Still in queue - Time: {response.queueTime}s, MMR: {response.userMmr}, Threshold: {response.currentMmrThreshold}");
                    }
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

    private IEnumerator CheckPlayerStatusForMatch()
    {
        Debug.Log("[Lobby] 🔍 Checking player status for match...");
        
        // First, check if player is in an active match
        UnityWebRequest matchRequest = UnityWebRequest.Get(main.MatchUrl + "user/" + currentUser.user_id);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        matchRequest.certificateHandler = new BypassCertificate();
        #endif
        
        yield return matchRequest.SendWebRequest();

        Debug.Log($"[Lobby] 📡 Match check response - Result: {matchRequest.result}, Code: {matchRequest.responseCode}");

        if (matchRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] ✅ Player match check successful. Response: {matchRequest.downloadHandler.text}");
            
            try
            {
                // Parse the match array response
                string jsonResponse = matchRequest.downloadHandler.text;
                
                // Check if response contains active matches
                if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse.Trim() != "[]")
                {
                    // Player has active matches - assume match found
                    Debug.Log($"[Lobby] 🎯 Match found! Player has active matches: {jsonResponse}");
                            _playerStatus = PlayerStatus.InGame;
                    main.SyncStateFromServer(_playerStatus, true);
                    ui_lobby.Instance.OnMatchFound("active_match");
                            yield break;
                        }
                        else
                        {
                    Debug.Log("[Lobby] ✅ No active matches found in response");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lobby] ❌ Failed to parse match response: {ex.Message}. Raw response: {matchRequest.downloadHandler.text}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
                            yield break;
                        }
                    }
                    else
                    {
            Debug.LogError($"[Lobby] ❌ Player match check failed - Result: {matchRequest.result}, Code: {matchRequest.responseCode}, Error: {matchRequest.error}");
            Debug.LogError($"[Lobby] ❌ Match check response text: {matchRequest.downloadHandler?.text}");
        }
        
        // If no active match found, check player status directly
        string playerUrl = main.PlayerUrl + "/" + currentUser.user_id;
        Debug.Log($"[Lobby] 🔍 Checking player status directly at: {playerUrl}");
        
        UnityWebRequest playerRequest = UnityWebRequest.Get(playerUrl);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        playerRequest.certificateHandler = new BypassCertificate();
        #endif
        
        yield return playerRequest.SendWebRequest();

        Debug.Log($"[Lobby] 📡 Player status response - Result: {playerRequest.result}, Code: {playerRequest.responseCode}");

        if (playerRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[Lobby] ✅ Player status check successful. Response: {playerRequest.downloadHandler.text}");
            
            try
            {
                var playerResponse = JsonUtility.FromJson<PlayerResponse>(playerRequest.downloadHandler.text);
                
                // Check if player data suggests they're in a match
                // This is a fallback - in the future we can add more specific match detection
                Debug.Log($"[Lobby] ✅ Player status updated. Games played: {playerResponse.gamesPlayed}");
                
                // For now, assume player genuinely left queue without finding match
                Debug.Log("[Lobby] ✅ No active match found - player left queue without finding match, setting to Idle");
                _playerStatus = PlayerStatus.Idle;
                main.SyncStateFromServer(_playerStatus, false);
                ui_lobby.Instance.OnSearchCanceled();
                }
                catch (System.Exception ex)
                {
                Debug.LogError($"[Lobby] ❌ Failed to parse player response: {ex.Message}. Raw response: {playerRequest.downloadHandler.text}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
                }
            }
            else
            {
            Debug.LogError($"[Lobby] ❌ Player status check failed - Result: {playerRequest.result}, Code: {playerRequest.responseCode}, Error: {playerRequest.error}");
            Debug.LogError($"[Lobby] ❌ Player status response text: {playerRequest.downloadHandler?.text}");
            
            // Более мягкая обработка ошибок
            if (playerRequest.result == UnityWebRequest.Result.ConnectionError || 
                playerRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[Lobby] ❌ Setting Error status due to player status check {playerRequest.result}");
                _playerStatus = PlayerStatus.Error;
                main.SyncStateFromServer(_playerStatus);
            }
            else
            {
                Debug.LogWarning($"[Lobby] ⚠️ Player status check failed but not setting Error status. Assuming Idle.");
                _playerStatus = PlayerStatus.Idle;
                main.SyncStateFromServer(_playerStatus, false);
                ui_lobby.Instance.OnSearchCanceled();
            }
        }
    }

    private IEnumerator CheckExistingQueueState()
    {
        Debug.Log("[Lobby] Checking existing queue state...");
        
        // Fix URL: use correct player status endpoint
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
    public int GetCurrentMatchType() => _currentMatchType;
    public string GetCurrentMatchTypeName() => GetMatchTypeName(_currentMatchType);
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

    // 🔧 Автоматическое восстановление при ошибке "пользователь не найден"
    private bool TryAutoRecoverUserNotFound(UnityWebRequest request, string context = "")
    {
        if (request.responseCode == 404 && 
            !string.IsNullOrEmpty(request.downloadHandler?.text) && 
            (request.downloadHandler.text.Contains("не найден") || 
             request.downloadHandler.text.Contains("not found") ||
             request.downloadHandler.text.Contains("User not found")))
        {
            Debug.LogWarning($"[Lobby] 🔧 User not found {context}! Auto-recovering by clearing data and re-registering...");
            
            // Очищаем все сохраненные данные
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            // Сбрасываем текущего пользователя
            currentUser = null;
            
            // Останавливаем heartbeat систему
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            
            // Запускаем повторную инициализацию
            Debug.Log($"[Lobby] 🔄 Starting automatic re-registration {context}...");
            _playerStatus = PlayerStatus.Unregistered;
            main.SyncStateFromServer(_playerStatus);
            
            // Запускаем инициализацию заново
            StartCoroutine(InitializePlayer());
            
            return true; // Восстановление запущено
        }
        
        return false; // Не ошибка "пользователь не найден"
    }
}
