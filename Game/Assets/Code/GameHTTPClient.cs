using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

[System.Serializable]
public class ChatMessage
{
    public int id;
    public string user_id;
    public string message;
    public string message_type;
    public string created_at;
    public string nick_name;
    public string avatar_url;
}

[System.Serializable]
public class ChatResponse
{
    public string status;
    public List<ChatMessage> messages;
    public ChatMessage message;
    public int total;
    public int online_count;
}

[System.Serializable]
public class UserResponse
{
    public string status;
    public string player_id;
    public string nick_name;
    public UserData user;
    public string message;
}

[System.Serializable]
public class UserData
{
    public string user_id;
    public string nick_name;
    public string email;
    public string avatar_url;
    public string mmr;
    public string status;
    public string profile_data;
    public string created_at;
    public string last_login;
}

[System.Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[System.Serializable]
public class RegisterRequest
{
    public string email;
    public string password;
    public string nick_name;
}

    [System.Serializable]
    public class ChatMessageRequest
    {
        public string user_id;
        public string message;
    }
    
    [System.Serializable]
    public class JoinLobbyRequest
    {
        public string player_id;
    }
    
    [System.Serializable]
    public class HeartbeatRequest
    {
        public string user_id;
    }

public class GameHTTPClient : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverURL = "https://renderfin.com";
    
    [Header("Current User")]
    public string currentUserId;
    public string currentUserNickname;
    public bool isLoggedIn = false;
    
    public static GameHTTPClient Instance;
    
    public event System.Action<List<ChatMessage>> OnChatMessagesReceived;
    public event System.Action<ChatMessage> OnNewChatMessage;
    public event System.Action<int> OnOnlineCountUpdate;
    public event System.Action<bool> OnConnectionStatusChanged;
    public event System.Action<UserData> OnUserLoggedIn;
    public event System.Action OnUserLoggedOut;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        Debug.Log("<color=green>GameHTTPClient: Starting up...</color>");
        Debug.Log($"<color=green>GameHTTPClient: Server URL set to {serverURL}</color>");
        
        // Проверяем сохраненные данные пользователя
        CheckSavedUserData();
    }
    
    private void CheckSavedUserData()
    {
        bool isRegistered = PlayerPrefs.GetInt("isRegistered", 0) == 1;
        Debug.Log($"<color=green>GameHTTPClient: isRegistered = {isRegistered}</color>");
        
        if (isRegistered)
        {
            string savedEmail = PlayerPrefs.GetString("userEmail", "");
            string savedPassword = PlayerPrefs.GetString("userPassword", "");
            string savedUserId = PlayerPrefs.GetString("userId", "");
            string savedNickname = PlayerPrefs.GetString("userNickname", "");
            
                    Debug.Log($"<color=green>GameHTTPClient: Found saved email: {savedEmail}</color>");
        Debug.Log($"<color=green>GameHTTPClient: Found saved userId: {savedUserId}</color>");
        Debug.Log($"<color=green>GameHTTPClient: Found saved nickname: {savedNickname}</color>");
            
            if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
            {
                Debug.Log("GameHTTPClient: Found saved credentials, attempting login...");
                StartCoroutine(LoginUser(savedEmail, savedPassword));
            }
            else
            {
                Debug.Log("GameHTTPClient: No saved credentials found, need to register");
                StartRegistrationProcess();
            }
        }
        else
        {
            Debug.Log("GameHTTPClient: User not registered, starting registration process");
            StartRegistrationProcess();
        }
    }
    
    private void StartRegistrationProcess()
    {
        // Генерируем случайные данные для регистрации
        string randomEmail = "player_" + System.DateTime.Now.Ticks + "@game.local";
        string randomPassword = "pass_" + UnityEngine.Random.Range(100000, 999999);
        string randomNickname = "Player_" + UnityEngine.Random.Range(1000, 9999);
        
        Debug.Log($"GameHTTPClient: Registering new user: {randomEmail}");
        StartCoroutine(RegisterUser(randomEmail, randomPassword, randomNickname));
    }
    
    public IEnumerator RegisterUser(string email, string password, string nickname)
    {
        Debug.Log($"GameHTTPClient: Starting registration for {email}");
        
        RegisterRequest registerData = new RegisterRequest
        {
            email = email,
            password = password,
            nick_name = nickname
        };
        
        string jsonData = JsonUtility.ToJson(registerData);
        Debug.Log($"GameHTTPClient: Sending registration request to {serverURL}/api-game-user/register");
        Debug.Log($"GameHTTPClient: Request data: {jsonData}");
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-user/register", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            Debug.Log("GameHTTPClient: Sending web request...");
            yield return request.SendWebRequest();
            
            Debug.Log($"GameHTTPClient: Request completed. Result: {request.result}");
            Debug.Log($"GameHTTPClient: Response code: {request.responseCode}");
            Debug.Log($"GameHTTPClient: Response text: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                UserResponse response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log($"<color=green>GameHTTPClient: User registered successfully: {response.player_id}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: Registration response: {request.downloadHandler.text}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id = '{response.player_id}'</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id is null: {response.player_id == null}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id is empty: {string.IsNullOrEmpty(response.player_id)}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.nick_name = '{response.nick_name}'</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.user = {(response.user != null ? "not null" : "null")}</color>");
                    if (response.user != null)
                    {
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id = '{response.user.user_id}'</color>");
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id is null: {response.user.user_id == null}</color>");
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id is empty: {string.IsNullOrEmpty(response.user.user_id)}</color>");
                    }
                    
                    // Авторизуемся
                    string userIdToUse = response.player_id;
                    if (string.IsNullOrEmpty(userIdToUse) && response.user != null)
                    {
                        userIdToUse = response.user.user_id;
                        Debug.Log($"GameHTTPClient: Using response.user.user_id for currentUserId: {userIdToUse}");
                    }
                    
                    currentUserId = userIdToUse;
                    currentUserNickname = response.nick_name;
                    isLoggedIn = true;
                    
                    // Сохраняем данные пользователя
                    PlayerPrefs.SetInt("isRegistered", 1);
                    PlayerPrefs.SetString("userEmail", email);
                    PlayerPrefs.SetString("userPassword", password);
                    PlayerPrefs.SetString("userId", currentUserId);
                    PlayerPrefs.SetString("userNickname", response.nick_name);
                    PlayerPrefs.Save();
                    
                    Debug.Log($"<color=green>GameHTTPClient: Set currentUserId to: '{currentUserId}'</color>");
                    
                    OnUserLoggedIn?.Invoke(response.user);
                    OnConnectionStatusChanged?.Invoke(true);
                    
                    // Добавляем пользователя в лобби (регистрация)
                    string playerIdToUse = response.player_id;
                    if (string.IsNullOrEmpty(playerIdToUse) && response.user != null)
                    {
                        playerIdToUse = response.user.user_id;
                        Debug.Log($"GameHTTPClient: Using response.user.user_id instead: {playerIdToUse}");
                    }
                    
                    if (!string.IsNullOrEmpty(playerIdToUse))
                    {
                        StartCoroutine(JoinLobby(playerIdToUse));
                    }
                    else
                    {
                        Debug.LogError("GameHTTPClient: Cannot join lobby - no valid player_id found (registration)");
                    }
                    
                    // Отправляем системное сообщение о подключении
                    StartCoroutine(SendSystemMessage($"{currentUserNickname} присоединился к чату и готов играть!"));
                }
                else
                {
                    Debug.LogError($"Registration failed: {response.message}");
                    Debug.Log("GameHTTPClient: Registration failed, setting disconnected status");
                    OnConnectionStatusChanged?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"Registration request failed: {request.error}");
                Debug.LogError($"Registration response code: {request.responseCode}");
                OnConnectionStatusChanged?.Invoke(false);
                
                // НЕ пытаемся снова автоматически, чтобы избежать бесконечного цикла
                Debug.Log("GameHTTPClient: Registration failed, will not retry automatically");
            }
        }
    }
    
    public IEnumerator LoginUser(string email, string password)
    {
        LoginRequest loginData = new LoginRequest
        {
            email = email,
            password = password
        };
        
        string jsonData = JsonUtility.ToJson(loginData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-user/login", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                UserResponse response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log($"<color=green>GameHTTPClient: User logged in successfully: {response.player_id}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: Login response: {request.downloadHandler.text}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id = '{response.player_id}'</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id is null: {response.player_id == null}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.player_id is empty: {string.IsNullOrEmpty(response.player_id)}</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.nick_name = '{response.nick_name}'</color>");
                    Debug.Log($"<color=green>GameHTTPClient: response.user = {(response.user != null ? "not null" : "null")}</color>");
                    if (response.user != null)
                    {
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id = '{response.user.user_id}'</color>");
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id is null: {response.user.user_id == null}</color>");
                        Debug.Log($"<color=green>GameHTTPClient: response.user.user_id is empty: {string.IsNullOrEmpty(response.user.user_id)}</color>");
                    }
                    
                    string userIdToUse = response.player_id;
                    if (string.IsNullOrEmpty(userIdToUse) && response.user != null)
                    {
                        userIdToUse = response.user.user_id;
                        Debug.Log($"GameHTTPClient: Using response.user.user_id for currentUserId: {userIdToUse}");
                    }
                    
                    currentUserId = userIdToUse;
                    currentUserNickname = response.nick_name;
                    isLoggedIn = true;
                    
                    Debug.Log($"<color=green>GameHTTPClient: Set currentUserId to: '{currentUserId}'</color>");
                    
                    OnUserLoggedIn?.Invoke(response.user);
                    OnConnectionStatusChanged?.Invoke(true);
                    
                    // Сохраняем обновленные данные пользователя
                    PlayerPrefs.SetString("userId", currentUserId);
                    PlayerPrefs.SetString("userNickname", currentUserNickname);
                    PlayerPrefs.Save();
                    
                    // Добавляем пользователя в лобби (логин)
                    string playerIdToUse = response.player_id;
                    if (string.IsNullOrEmpty(playerIdToUse) && response.user != null)
                    {
                        playerIdToUse = response.user.user_id;
                        Debug.Log($"GameHTTPClient: Using response.user.user_id instead: {playerIdToUse}");
                    }
                    
                    if (!string.IsNullOrEmpty(playerIdToUse))
                    {
                        StartCoroutine(JoinLobby(playerIdToUse));
                    }
                    else
                    {
                        Debug.LogError("GameHTTPClient: Cannot join lobby - no valid player_id found (login)");
                    }
                    
                    // Отправляем системное сообщение о подключении
                    StartCoroutine(SendSystemMessage($"{currentUserNickname} присоединился к чату и готов играть!"));
                }
                else
                {
                    Debug.LogError($"Login failed: {response.message}");
                    OnConnectionStatusChanged?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"Login request failed: {request.error}");
                Debug.LogError($"Login response code: {request.responseCode}");
                OnConnectionStatusChanged?.Invoke(false);
                
                // Если логин не удался, попробуем зарегистрироваться заново
                Debug.Log("GameHTTPClient: Login failed, trying to register new user...");
                StartRegistrationProcess();
            }
        }
    }
    
    public IEnumerator SendChatMessage(string message)
    {
        if (!isLoggedIn)
        {
            Debug.LogError("Cannot send message: user not logged in");
            yield break;
        }
        
        ChatMessageRequest messageData = new ChatMessageRequest
        {
            user_id = currentUserId,
            message = message
        };
        
        string jsonData = JsonUtility.ToJson(messageData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-chat/", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log("Message sent successfully");
                    Debug.Log($"Response message: {JsonUtility.ToJson(response.message)}");
                    OnNewChatMessage?.Invoke(response.message);
                }
                else
                {
                    Debug.LogError($"Failed to send message: {response.status}");
                }
            }
            else
            {
                Debug.LogError($"Send message request failed: {request.error}");
                Debug.LogError($"Send message response code: {request.responseCode}");
                
                // Если не удалось отправить сообщение, возможно потеряли соединение
                if (request.responseCode == 0)
                {
                    Debug.Log("GameHTTPClient: Connection lost during message send, trying to reconnect...");
                    OnConnectionStatusChanged?.Invoke(false);
                }
            }
        }
    }
    
    public IEnumerator SendSystemMessage(string message)
    {
        if (!isLoggedIn)
        {
            Debug.LogError("Cannot send system message: user not logged in");
            yield break;
        }
        
        ChatMessageRequest messageData = new ChatMessageRequest
        {
            user_id = currentUserId,
            message = message
        };
        
        string jsonData = JsonUtility.ToJson(messageData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-chat/system", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log("System message sent successfully");
                }
                else
                {
                    Debug.LogError($"Failed to send system message: {response.status}");
                }
            }
            else
            {
                // Не считаем ошибку 404 критичной - пользователь мог быть удален
                if (request.responseCode == 404)
                {
                    Debug.Log("GameHTTPClient: User not found when sending system message (probably already logged out)");
                }
                else
                {
                    Debug.LogError($"GameHTTPClient: Send system message request failed: {request.error}");
                }
            }
        }
    }
    
    public IEnumerator GetChatMessages(int limit = 50, int offset = 0)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverURL}/api-game-chat/?limit={limit}&offset={offset}"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"GameHTTPClient: GetChatMessages response: {request.downloadHandler.text}");
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log($"GameHTTPClient: Received {response.messages?.Count ?? 0} messages");
                    OnChatMessagesReceived?.Invoke(response.messages);
                }
                else
                {
                    Debug.LogError($"Failed to get messages: {response.status}");
                }
            }
            else
            {
                Debug.LogError($"Get messages request failed: {request.error}");
            }
        }
    }
    
    public IEnumerator GetOnlineCount()
    {
        Debug.Log("GameHTTPClient: Requesting online count...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverURL}/api-game-chat/online_count"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"GameHTTPClient: GetOnlineCount response: {request.downloadHandler.text}");
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                
                if (response.status == "success")
                {
                    Debug.Log($"GameHTTPClient: Online count: {response.online_count}");
                    OnOnlineCountUpdate?.Invoke(response.online_count);
                }
                else
                {
                    Debug.LogError($"GameHTTPClient: Failed to get online count: {response.status}");
                }
            }
            else
            {
                Debug.LogError($"GameHTTPClient: Get online count request failed: {request.error}");
                Debug.LogError($"GameHTTPClient: Get online count response code: {request.responseCode}");
            }
        }
    }
    
    public IEnumerator JoinLobby(string playerId)
    {
        // Проверяем, что playerId не пустой
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("GameHTTPClient: Cannot join lobby - playerId is null or empty");
            yield break;
        }
        
        Debug.Log($"GameHTTPClient: Attempting to join lobby with playerId: {playerId}");
        
        JoinLobbyRequest requestData = new JoinLobbyRequest { player_id = playerId };
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"<color=green>GameHTTPClient: JoinLobby JSON data: {jsonData}</color>");
        Debug.Log($"<color=green>GameHTTPClient: JoinLobby JSON data length: {jsonData?.Length ?? 0}</color>");
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-lobby/join", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("GameHTTPClient: Successfully joined lobby");
                Debug.Log($"GameHTTPClient: Join lobby response: {request.downloadHandler.text}");
                
                // После успешного присоединения к лобби обновляем счетчик онлайн
                StartCoroutine(GetOnlineCount());
            }
            else
            {
                Debug.LogError($"GameHTTPClient: Failed to join lobby: {request.error}");
                Debug.LogError($"GameHTTPClient: Join lobby response code: {request.responseCode}");
                Debug.LogError($"GameHTTPClient: Join lobby response: {request.downloadHandler.text}");
            }
        }
    }
    
    public IEnumerator LeaveLobby(string playerId)
    {
        JoinLobbyRequest requestData = new JoinLobbyRequest { player_id = playerId };
        string jsonData = JsonUtility.ToJson(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-lobby/leave", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("GameHTTPClient: Successfully left lobby");
            }
            else
            {
                // Не считаем ошибку 404 критичной - пользователь мог уже покинуть лобби
                if (request.responseCode == 404)
                {
                    Debug.Log("GameHTTPClient: User already left lobby or was not in lobby");
                }
                else
                {
                    Debug.LogWarning($"Failed to leave lobby: {request.error}");
                }
            }
        }
    }
    
    public IEnumerator SendHeartbeat()
    {
        if (!isLoggedIn)
        {
            Debug.Log("GameHTTPClient: Cannot send heartbeat - not logged in");
            yield break;
        }
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("GameHTTPClient: Cannot send heartbeat - currentUserId is null or empty");
            yield break;
        }
        
        Debug.Log($"<color=green>GameHTTPClient: SendHeartbeat - isLoggedIn={isLoggedIn}, currentUserId='{currentUserId}'</color>");
        Debug.Log($"<color=green>GameHTTPClient: currentUserId length: {currentUserId?.Length ?? 0}</color>");
        Debug.Log($"<color=green>GameHTTPClient: currentUserId is null: {currentUserId == null}</color>");
        Debug.Log($"<color=green>GameHTTPClient: currentUserId is empty: {string.IsNullOrEmpty(currentUserId)}</color>");
            
        HeartbeatRequest requestData = new HeartbeatRequest { user_id = currentUserId };
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"<color=green>GameHTTPClient: Sending heartbeat for user {currentUserId}</color>");
        Debug.Log($"<color=green>GameHTTPClient: Heartbeat JSON data: {jsonData}</color>");
        Debug.Log($"<color=green>GameHTTPClient: JSON data length: {jsonData?.Length ?? 0}</color>");
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/api-game-chat/heartbeat", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=green>GameHTTPClient: Heartbeat sent successfully. Response: {request.downloadHandler.text}</color>");
                
                // После успешного heartbeat обновляем счетчик онлайн
                StartCoroutine(GetOnlineCount());
            }
            else
            {
                Debug.LogWarning($"GameHTTPClient: Failed to send heartbeat: {request.error}");
                Debug.LogWarning($"GameHTTPClient: Heartbeat response code: {request.responseCode}");
                Debug.LogWarning($"GameHTTPClient: Heartbeat response: {request.downloadHandler.text}");
                
                // Если heartbeat не удался, возможно потеряли соединение
                if (request.responseCode == 0)
                {
                    Debug.Log("GameHTTPClient: Connection lost during heartbeat, attempting reconnect...");
                    OnConnectionStatusChanged?.Invoke(false);
                    
                    // Попробуем переподключиться
                    StartCoroutine(AttemptReconnect());
                }
                else if (request.responseCode >= 400)
                {
                    Debug.LogWarning($"GameHTTPClient: Heartbeat failed with code {request.responseCode}");
                }
            }
        }
    }
    
    public IEnumerator AttemptReconnect()
    {
        Debug.Log("GameHTTPClient: Attempting to reconnect...");
        
        // Ждем немного перед попыткой переподключения
        yield return new WaitForSeconds(5f);
        
        // Проверяем, есть ли сохраненные данные для переподключения
        bool isRegistered = PlayerPrefs.GetInt("isRegistered", 0) == 1;
        
        if (isRegistered)
        {
            string savedEmail = PlayerPrefs.GetString("userEmail", "");
            string savedPassword = PlayerPrefs.GetString("userPassword", "");
            string savedUserId = PlayerPrefs.GetString("userId", "");
            
            Debug.Log($"GameHTTPClient: AttemptReconnect - savedUserId: {savedUserId}");
            
            if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
            {
                Debug.Log("GameHTTPClient: Attempting to reconnect with saved credentials...");
                StartCoroutine(LoginUser(savedEmail, savedPassword));
            }
            else
            {
                Debug.Log("GameHTTPClient: No saved credentials for reconnect");
            }
        }
    }
    
    public void LogoutUser()
    {
        Debug.Log("GameHTTPClient: LogoutUser called");
        if (isLoggedIn)
        {
            Debug.Log($"GameHTTPClient: Logging out user {currentUserNickname}");
            
            // Отправляем системное сообщение и покидаем лобби без ожидания
            StartCoroutine(SendSystemMessage($"{currentUserNickname} покинул чат"));
            StartCoroutine(LeaveLobby(currentUserId));
            
            currentUserId = "";
            currentUserNickname = "";
            isLoggedIn = false;
            
            OnUserLoggedOut?.Invoke();
            OnConnectionStatusChanged?.Invoke(false);
        }
        else
        {
            Debug.Log("GameHTTPClient: LogoutUser called but user not logged in");
        }
    }
    
    private float lastFocusLostTime = 0f;
    private const float FOCUS_LOGOUT_DELAY = 300f; // 5 минут до отключения
    private Coroutine logoutCoroutine = null;
    
    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"GameHTTPClient: OnApplicationPause called with pauseStatus={pauseStatus}");
        
        // На мобильных устройствах используем OnApplicationPause вместо OnApplicationFocus
        #if UNITY_ANDROID || UNITY_IOS
        if (pauseStatus && isLoggedIn)
        {
            Debug.Log("GameHTTPClient: App paused on mobile, starting logout timer");
            if (logoutCoroutine != null)
                StopCoroutine(logoutCoroutine);
            logoutCoroutine = StartCoroutine(DelayedLogout());
        }
        else if (!pauseStatus && isLoggedIn)
        {
            Debug.Log("GameHTTPClient: App resumed on mobile, cancelling logout");
            if (logoutCoroutine != null)
            {
                StopCoroutine(logoutCoroutine);
                logoutCoroutine = null;
            }
        }
        #endif
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"GameHTTPClient: OnApplicationFocus called with hasFocus={hasFocus}");
        
        // В Editor НЕ отключаемся вообще
        #if UNITY_EDITOR
        return;
        #endif
        
        // На Desktop отключаемся только через 5 минут без фокуса
        #if !UNITY_ANDROID && !UNITY_IOS
        if (!hasFocus && isLoggedIn)
        {
            lastFocusLostTime = Time.time;
            Debug.Log("GameHTTPClient: Focus lost, starting logout timer");
            if (logoutCoroutine != null)
                StopCoroutine(logoutCoroutine);
            logoutCoroutine = StartCoroutine(DelayedLogout());
        }
        else if (hasFocus && logoutCoroutine != null)
        {
            Debug.Log("GameHTTPClient: Focus regained, cancelling logout");
            StopCoroutine(logoutCoroutine);
            logoutCoroutine = null;
        }
        #endif
    }
    
    private IEnumerator DelayedLogout()
    {
        Debug.Log($"GameHTTPClient: Starting {FOCUS_LOGOUT_DELAY} second logout timer");
        yield return new WaitForSeconds(FOCUS_LOGOUT_DELAY);
        
        if (isLoggedIn)
        {
            Debug.Log("GameHTTPClient: Logout timer expired, disconnecting user");
            LogoutUser();
        }
    }
}
