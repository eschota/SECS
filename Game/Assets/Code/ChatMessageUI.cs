using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
