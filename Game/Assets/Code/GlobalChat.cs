using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 
using System.Collections.Generic;
using System.Linq;

public class GlobalChat : MonoBehaviour 
{
    [Header("UI")]
    [SerializeField] private TMP_Text onlineCountText;
    [SerializeField] private RectTransform messagesContent;   // ScrollView/Viewport/Content
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private GameObject messagePrefab;        // префаб сообщения
    [SerializeField] private CanvasGroup chatCanvasGroup;     // для показа/скрытия чата
    [SerializeField] private Button chatToggleButton;         // кнопка для открытия/закрытия чата
    [SerializeField] private Image chatToggleButtonImage;     // изображение кнопки для смены цвета
    [SerializeField] private ScrollRect scrollRect;           // для автоскролла

    [Header("Colors")]
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private Color ownMessageColor = new Color(0.8f, 0.9f, 1f, 1f);
    [SerializeField] private Color otherMessageColor = Color.white;
    [SerializeField] private Color systemMessageColor = Color.yellow;

    private bool isChatVisible = false;
    private bool isConnected = false;
    private List<ChatMessage> currentMessages = new List<ChatMessage>();
    
    private void Awake()
    {
        // Скрываем чат на старте
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 0f;
            chatCanvasGroup.interactable = false;
            chatCanvasGroup.blocksRaycasts = false;
        }
        
        // Настройка кнопок
        if (sendButton != null)
            sendButton.onClick.AddListener(SendMessage);
            
        if (chatToggleButton != null)
            chatToggleButton.onClick.AddListener(ToggleChat);
            
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        }
        
        // Устанавливаем красный цвет кнопки (не подключен)
        SetButtonColor(disconnectedColor);
        isConnected = false;
    }

    private void Start()
    {
        // Создаем GameHTTPClient если его нет
        if (GameHTTPClient.Instance == null)
        {
            //Debug.Log("Creating GameHTTPClient...");
            GameObject httpClientObj = new GameObject("GameHTTPClient");
            httpClientObj.AddComponent<GameHTTPClient>();
            DontDestroyOnLoad(httpClientObj);
        }
        
        // Ждем создания и подписываемся на события
        StartCoroutine(WaitForHTTPClientAndConnect());
        
        // Запускаем периодическое обновление
        StartCoroutine(UpdateRoutine());
    }
    
    private System.Collections.IEnumerator WaitForHTTPClientAndConnect()
    {
        // Ждем пока GameHTTPClient будет создан
        while (GameHTTPClient.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        //Debug.Log("GameHTTPClient found, subscribing to events...");
        
        // Подписываемся на события HTTP клиента
        GameHTTPClient.Instance.OnChatMessagesReceived += OnChatMessagesReceived;
        GameHTTPClient.Instance.OnNewChatMessage += OnNewChatMessage;
        GameHTTPClient.Instance.OnOnlineCountUpdate += OnOnlineCountUpdate;
        GameHTTPClient.Instance.OnConnectionStatusChanged += OnConnectionStatusChanged;
        GameHTTPClient.Instance.OnUserLoggedIn += OnUserLoggedIn;
        
        //Debug.Log("Events subscribed, GameHTTPClient should start auto-login process...");
        
        // Принудительно запрашиваем счетчик онлайн
        StartCoroutine(GameHTTPClient.Instance.GetOnlineCount());
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (GameHTTPClient.Instance != null)
        {
            GameHTTPClient.Instance.OnChatMessagesReceived -= OnChatMessagesReceived;
            GameHTTPClient.Instance.OnNewChatMessage -= OnNewChatMessage;
            GameHTTPClient.Instance.OnOnlineCountUpdate -= OnOnlineCountUpdate;
            GameHTTPClient.Instance.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            GameHTTPClient.Instance.OnUserLoggedIn -= OnUserLoggedIn;
        }
    }
    
    private void OnUserLoggedIn(UserData userData)
    {
        //Debug.Log($"User logged in: {userData.nick_name}");
        // Загружаем сообщения чата при подключении
        if (GameHTTPClient.Instance != null)
        {
            StartCoroutine(GameHTTPClient.Instance.GetChatMessages());
        }
    }
    
    private void OnConnectionStatusChanged(bool connected)
    {
        isConnected = connected;
        SetButtonColor(connected ? connectedColor : disconnectedColor);
        
        if (connected)
        {
            //Debug.Log("Connected to chat server");
        }
        else
        {
            //Debug.Log("Disconnected from chat server");
        }
    }
    
    private void OnChatMessagesReceived(List<ChatMessage> messages)
    {
        //Debug.Log($"GlobalChat: OnChatMessagesReceived called with {messages?.Count ?? 0} messages");
        
        if (messages == null) 
        {
            //Debug.Log("GlobalChat: Messages list is null");
            return;
        }
        
        // Если это первая загрузка сообщений, просто заменяем весь список
        if (currentMessages.Count == 0)
        {
            //Debug.Log("GlobalChat: First time loading messages, replacing all");
            currentMessages = new List<ChatMessage>(messages);
            RefreshChatDisplay();
            return;
        }
        
        // Проверяем, есть ли новые сообщения
        bool hasNewMessages = false;
        
        foreach (var message in messages)
        {
            bool messageExists = currentMessages.Any(m => m.id == message.id);
            if (!messageExists)
            {
                //Debug.Log($"GlobalChat: Adding new message: {message.message}");
                currentMessages.Add(message);
                hasNewMessages = true;
            }
        }
        
        if (hasNewMessages)
        {
            //Debug.Log("GlobalChat: Refreshing chat display due to new messages");
            RefreshChatDisplay();
        }
        else
        {
            //Debug.Log("GlobalChat: No new messages to display");
        }
    }
    
    private void OnNewChatMessage(ChatMessage message)
    {
        //Debug.Log($"GlobalChat: Received new message from {message.nick_name}: {message.message}");
        currentMessages.Add(message);
        AddMessageToDisplay(message);
        ScrollToBottom();
    }
    
    private void OnOnlineCountUpdate(int count)
    {
        if (onlineCountText != null)
        {
            onlineCountText.text = $"Онлайн: {count}";
        }
    }
    
    private void SetButtonColor(Color color)
    {
        if (chatToggleButtonImage != null)
        {
            chatToggleButtonImage.color = color;
        }
    }
    
    private void ToggleChat()
    {
        isChatVisible = !isChatVisible;

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = isChatVisible ? 1f : 0f;
            chatCanvasGroup.interactable = isChatVisible;
            chatCanvasGroup.blocksRaycasts = isChatVisible;
            if (isChatVisible)
                UI_Canvas.i.currentState = UI_Canvas.UI_State.Chatting;
            else
                UI_Canvas.i.currentState = UI_Canvas.UI_State.None;
        }
        
        if (isChatVisible)
        {
            ScrollToBottom();
        }
    }
    
    private void SendMessage()
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text.Trim()))
            return;
            
        if (!isConnected || GameHTTPClient.Instance == null)
        {
            Debug.LogWarning("Cannot send message: not connected to server");
            return;
        }
        
        string message = inputField.text.Trim();
        inputField.text = "";
        
        StartCoroutine(GameHTTPClient.Instance.SendChatMessage(message));
        
        // Возвращаем фокус на поле ввода после отправки сообщения
        StartCoroutine(RefocusInputField());
    }
    
    private IEnumerator RefocusInputField()
    {
        // Ждем один кадр, чтобы UI обновился
        yield return null;
        
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    private void OnInputFieldEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
            // Фокус вернется через RefocusInputField в SendMessage
        }
    }
    
    private void RefreshChatDisplay()
    {
        // Очищаем все сообщения
        foreach (Transform child in messagesContent)
        {
            Destroy(child.gameObject);
        }
        
        // Добавляем все сообщения заново
        foreach (var message in currentMessages)
        {
            AddMessageToDisplay(message);
        }
        
        // Прокручиваем вниз после обновления
        StartCoroutine(ScrollToBottomDelayed());
    }
    
    private void AddMessageToDisplay(ChatMessage message)
    {
//        //Debug.Log($"GlobalChat: AddMessageToDisplay called for message: {message?.message}");
        
        if (messagePrefab == null)
        {
            Debug.LogError("GlobalChat: messagePrefab is null! Creating a simple one...");
            
            // Создаем простой префаб на лету
            messagePrefab = new GameObject("SimpleMessagePrefab");
            var tmpText = messagePrefab.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = "Sample Message";
            tmpText.fontSize = 14;
            tmpText.color = Color.white;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TMPro.TextOverflowModes.Overflow;
            
            // Настраиваем RectTransform
            var rectTransform = messagePrefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(380, 30); // Немного уже для отступов
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            
            // Добавляем LayoutElement для правильного размера
            var layoutElement = messagePrefab.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.minHeight = 20;
            layoutElement.preferredHeight = -1;
            layoutElement.flexibleHeight = 1;
            layoutElement.layoutPriority = 1;
            
            // Добавляем ContentSizeFitter для автоматического размера
            var sizeFitter = messagePrefab.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            //Debug.Log("GlobalChat: Created simple message prefab");
        }
        
        if (messagesContent == null)
        {
            Debug.LogError("GlobalChat: messagesContent is null!");
            return;
        }
        
        // Убеждаемся, что у Content есть правильный VerticalLayoutGroup
        var layoutGroup = messagesContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = messagesContent.gameObject.AddComponent<VerticalLayoutGroup>();
            //Debug.Log("GlobalChat: Added VerticalLayoutGroup to messagesContent");
        }
        
        // Настраиваем VerticalLayoutGroup
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 5f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        
        // Убеждаемся, что у Content есть ContentSizeFitter
        var contentSizeFitter = messagesContent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = messagesContent.gameObject.AddComponent<ContentSizeFitter>();
            //Debug.Log("GlobalChat: Added ContentSizeFitter to messagesContent");
        }
        
        // Настраиваем ContentSizeFitter
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
        GameObject messageObj = Instantiate(messagePrefab, messagesContent);
        //Debug.Log($"GlobalChat: Created message object: {messageObj.name}");
        ChatMessageUI messageUI = messageObj.GetComponent<ChatMessageUI>();
        //Debug.Log($"GlobalChat: ChatMessageUI component: {(messageUI != null ? "Found" : "Not found")}");
        
        // Если есть компонент ChatMessageUI, используем его
        if (messageUI != null)
        {
            string displayText;
            Color textColor;
            
            if (message.message_type == "system")
            {
                displayText = $"[СИСТЕМА] {message.message}";
                textColor = systemMessageColor;
                messageUI.SetAsSystemMessage();
                messageUI.SetMessage(displayText, textColor, TextAlignmentOptions.Left);
            }
            else
            {
                bool isOwnMessage = GameHTTPClient.Instance != null && 
                                   message.user_id == GameHTTPClient.Instance.currentUserId;
                
                displayText = $"{message.nick_name}: {message.message}";
                textColor = isOwnMessage ? ownMessageColor : otherMessageColor;
                
                messageUI.SetAsOwnMessage(isOwnMessage);
                TextAlignmentOptions alignment = isOwnMessage ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
                messageUI.SetMessage(displayText, textColor, alignment);
            }
        }
        else
        {
            // Fallback для простого TMP_Text компонента
            TMP_Text messageText = messageObj.GetComponent<TMP_Text>();
            
            if (messageText != null)
            {
                string displayText;
                Color textColor;
                TextAlignmentOptions alignment;
                
                if (message.message_type == "system")
                {
                    displayText = $"[СИСТЕМА] {message.message}";
                    textColor = systemMessageColor;
                    alignment = TextAlignmentOptions.Left;
                }
                else
                {
                    bool isOwnMessage = GameHTTPClient.Instance != null && 
                                       message.user_id == GameHTTPClient.Instance.currentUserId;
                    
                    displayText = $"{message.nick_name}: {message.message}";
                    textColor = isOwnMessage ? ownMessageColor : otherMessageColor;
                    alignment = isOwnMessage ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
                }
                
                messageText.text = displayText;
                messageText.color = textColor;
                messageText.alignment = alignment;
            }
        }
    }
    
    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            // Принудительно обновляем layout
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(messagesContent);
            Canvas.ForceUpdateCanvases();
            
            // Прокручиваем к низу
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
    
    private IEnumerator ScrollToBottomDelayed()
    {
        yield return new WaitForEndOfFrame();
        ScrollToBottom();
    }
    
    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);
            
            if (isConnected && GameHTTPClient.Instance != null)
            {
                // Отправляем heartbeat и обновляем счетчик онлайн
                StartCoroutine(GameHTTPClient.Instance.SendHeartbeat());
                StartCoroutine(GameHTTPClient.Instance.GetOnlineCount());
                
                // Периодически загружаем новые сообщения
                StartCoroutine(GameHTTPClient.Instance.GetChatMessages());
            }
        }
    }
}

[System.Serializable]
public class ChatMessageUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    
    [Header("Message Settings")]
    [SerializeField] private float minHeight = 30f;
    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float padding = 10f;
    
    private void Awake()
    {
        SetupComponents();
    }
    
    private void SetupComponents()
    {
        // Получаем компоненты если они не назначены
        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();
            
        // Добавляем LayoutElement если его нет
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
            
        // Настраиваем LayoutElement
        layoutElement.minHeight = minHeight;
        layoutElement.preferredHeight = -1; // Автоматический размер по высоте
        layoutElement.flexibleWidth = 1;
        layoutElement.flexibleHeight = 1;
        layoutElement.layoutPriority = 1;
        
        // Настраиваем текст
        if (messageText != null)
        {
            messageText.enableWordWrapping = true;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.fontSize = 14;
        }
        
        // Добавляем ContentSizeFitter для автоматического размера
        var sizeFitter = GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
    
    public void SetMessage(string text, Color textColor, TextAlignmentOptions alignment)
    {
        if (messageText != null)
        {
            messageText.text = text;
            messageText.color = textColor;
            messageText.alignment = alignment;
        }
    }
    
    public void SetBackgroundColor(Color backgroundColor)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
    }
    
    public void SetAsOwnMessage(bool isOwn)
    {
        if (isOwn)
        {
            // Выравнивание справа для собственных сообщений
            if (messageText != null)
                messageText.alignment = TextAlignmentOptions.Right;
                
            // Можно добавить дополнительное оформление
            SetBackgroundColor(new Color(0.2f, 0.4f, 0.8f, 0.3f));
        }
        else
        {
            // Выравнивание слева для сообщений других игроков
            if (messageText != null)
                messageText.alignment = TextAlignmentOptions.Left;
                
            SetBackgroundColor(new Color(0.3f, 0.3f, 0.3f, 0.3f));
        }
    }
    
    public void SetAsSystemMessage()
    {
        // Системные сообщения слева
        if (messageText != null)
        {
            messageText.alignment = TextAlignmentOptions.Left;
            messageText.fontStyle = FontStyles.Italic;
        }
        
        SetBackgroundColor(new Color(1f, 1f, 0f, 0.2f)); // Желтоватый фон
    }
}
