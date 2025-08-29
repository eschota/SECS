using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Play.State state_to_show = Play.State.None;
    public item_SO Item;
    public Image Targetimage;
    public float scalePercentage = 20f;
    public float clickTweenDuration = 0.08f;

    private Vector3 _originalScale;
    private RectTransform _targetRectTransform;
    private bool _isHovered = false;
    private Coroutine _clickCoroutine;
    
    // Selection system
    public enum ButtonType { Menu, Type, Sub }
    public ButtonType buttonType = ButtonType.Sub;
    public int subGroupIndex = -1; // For sub buttons only

    private float localTimer = 0f;
    void Start()
    {
        _targetRectTransform = Targetimage != null ? Targetimage.rectTransform : GetComponent<RectTransform>();
        if (Targetimage == null)
        {
            Targetimage = GetComponent<Image>();
        }
        if (_targetRectTransform != null)
        {
            _originalScale = _targetRectTransform.localScale;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Play.i?.currentState == Play.State.Create) return;
         
            localTimer += Time.deltaTime;
           
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_targetRectTransform == null) return;
        float scaleFactor = 1f + (scalePercentage / 100f);
        _targetRectTransform.localScale = _originalScale * scaleFactor;
        _isHovered = true;
        if (UI_ToolTip.i != null)
        {
            UI_ToolTip.i.BeginHover(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_targetRectTransform == null) return;
        _targetRectTransform.localScale = _originalScale;
        _isHovered = false;
        if (UI_ToolTip.i != null)
        {
            UI_ToolTip.i.EndHover(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_targetRectTransform == null) return;
        if (_clickCoroutine != null)
        {
            StopCoroutine(_clickCoroutine);
        }
        _clickCoroutine = StartCoroutine(ClickPulse());

        // Handle selection
        HandleSelection();
        if (state_to_show != Play.State.None)
        {
            Play.i.currentState = state_to_show;
        }
    }

    System.Collections.IEnumerator ClickPulse()
    {
        Vector3 hoverScale = _originalScale * (1f + (scalePercentage / 100f));
        Vector3 startScale = _isHovered ? hoverScale : _originalScale;
        Vector3 midScale = _isHovered ? _originalScale : hoverScale;
        Vector3 endScale = _isHovered ? hoverScale : _originalScale;

        float halfDuration = Mathf.Max(0.01f, clickTweenDuration * 0.5f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / halfDuration;
            _targetRectTransform.localScale = Vector3.Lerp(startScale, midScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / halfDuration;
            _targetRectTransform.localScale = Vector3.Lerp(midScale, endScale, t);
            yield return null;
        }

        _targetRectTransform.localScale = endScale;
        _clickCoroutine = null;
    }

    void OnDisable()
    {
        if (_targetRectTransform != null)
        {
            _targetRectTransform.localScale = _originalScale;
        }
        if (UI_ToolTip.i != null)
        {
            UI_ToolTip.i.EndHover(this);
        }
        _isHovered = false;
        if (_clickCoroutine != null)
        {
            StopCoroutine(_clickCoroutine);
            _clickCoroutine = null;
        }
    }
    
    // Setup methods
    
    
    public void SetupAsSubButton(int groupIndex)
    {
        buttonType = ButtonType.Sub;
        subGroupIndex = groupIndex;
    }
    
    // Handle selection logic
    private void HandleSelection()
    {
        if (UI_Canvas.i == null) return;
        
        switch (buttonType)
        {
            case ButtonType.Type:
                UI_Canvas.i.SelectTypeButton(this);
                break;
            case ButtonType.Sub:
                // Проверяем, что subGroupIndex в допустимых пределах
                if (subGroupIndex >= 0 && subGroupIndex < UI_Canvas.i.selectedSubButtons.Length)
                {
                    UI_Canvas.i.SelectSubButton(this, subGroupIndex);
                }
                else
                {
                    Debug.LogWarning($"UI_Button {name}: subGroupIndex {subGroupIndex} out of bounds [0, {UI_Canvas.i.selectedSubButtons.Length - 1}]");
                }
                break;
        }
    }
}
