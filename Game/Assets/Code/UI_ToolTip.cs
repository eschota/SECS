using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ToolTip : MonoBehaviour
{
    public static UI_ToolTip i;

    [Header("Refs")]
    [SerializeField] private RectTransform tooltipRect; // this object RectTransform
    [SerializeField] private RectTransform backPlateRect; // panel background
    [SerializeField] private TextMeshProUGUI textTMP; // text component

    [Header("Settings")]
    [SerializeField] private float showDelaySeconds = 0.25f;
    [SerializeField] private Vector2 offset = new Vector2(16f, 16f);

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private UI_Button _hoverTargetButton;
    private item_SO _hoverItem;
    private float _hoverTimer;
    private bool _isShowing;

    // Fade-out
    [SerializeField] private float fadeOutSeconds = 0.25f;
    private bool _isFadingOut;
    private float _fadeElapsed;
    private float _fadeStartAlpha;

    // Sizing baseline
    private float _baseBackWidth;
    private float _baseBackHeight;
    private float _widthPerChar;

    void Awake()
    {
        i = this;
        if (tooltipRect == null) tooltipRect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (backPlateRect == null)
        {
            var back = transform.Find("backPlate");
            if (back != null) backPlateRect = back.GetComponent<RectTransform>();
        }
        if (textTMP == null && backPlateRect != null)
        {
            textTMP = backPlateRect.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (backPlateRect != null)
        {
            _baseBackWidth = backPlateRect.sizeDelta.x;
            _baseBackHeight = backPlateRect.sizeDelta.y;
            _widthPerChar = _baseBackWidth / 12f; // 12 chars baseline
        }

        // Keep object active; hide by alpha so component keeps updating
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _isShowing = false;
        _isFadingOut = false;
    }

    void Update()
    {
        // Handle fade-out even if no hover target
        if (_isFadingOut)
        {
            _fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / Mathf.Max(0.0001f, fadeOutSeconds));
            _canvasGroup.alpha = Mathf.Lerp(_fadeStartAlpha, 0f, t);
            if (t >= 1f)
            {
                _isFadingOut = false;
                _canvasGroup.alpha = 0f;
            }
        }

        if (_hoverTargetButton == null)
        {
            _hoverTimer = 0f;
            if (_isShowing) StartFadeOut();
            return;
        }

        // Timer logic
        if (!_isShowing)
        {
            _hoverTimer += Time.unscaledDeltaTime;
            if (_hoverTimer >= showDelaySeconds)
            {
                ShowNow();
            }
        }
        else
        {
            // If currently showing but text is empty, fade out
            if (textTMP != null && string.IsNullOrEmpty(textTMP.text))
            {
                StartFadeOut();
                return;
            }
            // Follow target instantly
            UpdatePositionAndFlip();
        }
    }

    public void BeginHover(UI_Button targetButton)
    {
        _hoverTargetButton = targetButton;
        _hoverItem = targetButton != null ? targetButton.Item : null;
        _hoverTimer = 0f;
        //Debug.Log($"UI_ToolTip BeginHover target={(targetButton!=null?targetButton.name:"null")} title={_hoverItem?.Title}");
        if (_isShowing)
        {
            // Update text/size immediately when switching targets
            if (!ApplyTextAndResize())
            {
                StartFadeOut();
                return;
            }
            UpdatePositionAndFlip();
        }
    }

    public void EndHover(UI_Button targetButton)
    {
        if (_hoverTargetButton == targetButton)
        {
            _hoverTargetButton = null;
            _hoverItem = null;
            _hoverTimer = 0f;
//            Debug.Log("UI_ToolTip EndHover");
            StartFadeOut();
        }
    }

    private void ShowNow()
    {
        if (!ApplyTextAndResize())
        {
            StartFadeOut();
            return;
        }
        UpdatePositionAndFlip();
        SetVisible(true);
        //Debug.Log($"UI_ToolTip ShowNow target={_hoverTargetButton?.name} title={_hoverItem?.Title} screen=({Screen.width}x{Screen.height}) worldPos={tooltipRect.position}");
    }

    private bool ApplyTextAndResize()
    {
        if (textTMP != null)
        {
            string title = _hoverItem != null ? _hoverItem.Title : string.Empty;
            textTMP.text = title ?? string.Empty;
            textTMP.textWrappingMode = TextWrappingModes.Normal;
            if (string.IsNullOrEmpty(title))
            {
                return false;
            }
        }

        if (backPlateRect != null)
        {
            int charCount = textTMP != null ? textTMP.text.Length : 0;
            int widthChars = Mathf.Clamp(charCount, 12, 30);
            float newWidth = Mathf.Max(_baseBackWidth, _widthPerChar * widthChars);

            int lines = Mathf.Max(1, Mathf.CeilToInt(charCount / 30f));
            float newHeight = _baseBackHeight * lines;

            backPlateRect.sizeDelta = new Vector2(newWidth, newHeight);
            // Do not change Text RectTransform that is configured manually in the scene
        }
        return true;
    }

    private void UpdatePositionAndFlip()
    {
        if (_hoverTargetButton == null) return;
        RectTransform targetRect = _hoverTargetButton.GetComponent<RectTransform>();
        if (targetRect == null) return;

        // Compute true world center of the target regardless of pivot/anchors
        Vector3 worldCenter;
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            worldCenter = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
        }

        // Decide offset direction based on screen quadrant from the center point
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, worldCenter);
        float dirX = screenPos.x > (Screen.width * 0.5f) ? -1f : 1f;
        float dirY = screenPos.y > (Screen.height * 0.5f) ? -1f : 1f;

        // Auto offset = half of target size (in world space-ish using lossyScale) + user padding
        Vector2 targetSize = targetRect.rect.size;
        Vector3 targetScale = targetRect.lossyScale;
        Vector2 halfSize = new Vector2(targetSize.x * Mathf.Abs(targetScale.x) * 0.5f,
                                       targetSize.y * Mathf.Abs(targetScale.y) * 0.5f);
        Vector2 autoOffset = new Vector2(halfSize.x + Mathf.Abs(offset.x), halfSize.y + Mathf.Abs(offset.y));

        tooltipRect.position = worldCenter + new Vector3(autoOffset.x * dirX, autoOffset.y * dirY, 0f);
        // Flip tooltip so он всегда «смотрит» внутрь экрана
        tooltipRect.localScale = new Vector3(dirX, dirY, 1f);

        // И одновременно инвертируем скейл текста по тем же осям,
        // чтобы текст не был зеркальным
        if (textTMP != null)
        {
            var textRect = (RectTransform)textTMP.transform;
            textRect.localScale = new Vector3(dirX < 0f ? -1f : 1f, dirY < 0f ? -1f : 1f, 1f);
        }
    }

    private void SetVisible(bool visible)
    {
        _isShowing = visible;
        _isFadingOut = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    private void StartFadeOut()
    {
        _isShowing = false;
        if (_canvasGroup == null)
        {
            return;
        }
        _isFadingOut = true;
        _fadeElapsed = 0f;
        _fadeStartAlpha = _canvasGroup.alpha;
    }
}
