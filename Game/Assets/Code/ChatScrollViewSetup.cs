using UnityEngine;
using UnityEngine.UI;

public class ChatScrollViewSetup : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    
    private void Awake()
    {
        SetupScrollView();
    }
    
    private void SetupScrollView()
    {
        // Настройка ScrollRect
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
            
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1.0f;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }
        
        // Настройка Content
        if (content == null)
        {
            Transform viewportTransform = transform.Find("Viewport");
            if (viewportTransform != null)
            {
                content = viewportTransform.Find("Content") as RectTransform;
            }
        }
        
        if (content != null)
        {
            // Настройка якорей для прокрутки снизу вверх
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(1, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            
            // Добавляем VerticalLayoutGroup если его нет
            if (layoutGroup == null)
                layoutGroup = content.GetComponent<VerticalLayoutGroup>();
                
            if (layoutGroup == null)
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                
            // Настройка VerticalLayoutGroup
            layoutGroup.childAlignment = TextAnchor.LowerLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            // Добавляем ContentSizeFitter если его нет
            if (contentSizeFitter == null)
                contentSizeFitter = content.GetComponent<ContentSizeFitter>();
                
            if (contentSizeFitter == null)
                contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                
            // Настройка ContentSizeFitter
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    
    public void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
    
    public void AddMessage(GameObject messageObject)
    {
        if (content != null && messageObject != null)
        {
            messageObject.transform.SetParent(content, false);
            
            // Автоматически прокручиваем вниз после добавления сообщения
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }
    
    private System.Collections.IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        ScrollToBottom();
    }
    
    public void ClearMessages()
    {
        if (content != null)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
