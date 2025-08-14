using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class player_ui : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement movement;
    public Player_Name playerName;

    [Header("Layout Settings")]
    public float widthPercent = 0.10f;   // 10%
    public float heightPercent = 0.01f;  // 1%
    public float marginLeft = 12f;
    public float marginBottom = 12f;

    private Canvas _canvas;
    private RectTransform _rootPanel;
    private Image _staminaBg;
    private Image _staminaFill;
    private Button _nameButton;
    private Text _nameText;
    private InputField _nameInput;
    private bool _editingName = false;

    void Start()
    {
        EnsureEventSystem();
        EnsureCanvas();
        BuildUI();
        RefreshLayout();
        UpdateNameText();
    }

    void Update()
    {
        if (_rootPanel == null || movement == null) return;

        // Update stamina bar
        float max = Mathf.Max(0.0001f, movement.MaxStamina);
        float ratio = Mathf.Clamp01(movement.Stamina / max);

        var size = _rootPanel.sizeDelta;
        if (_staminaFill != null)
        {
            var fillRt = _staminaFill.rectTransform;
            fillRt.sizeDelta = new Vector2(size.x * ratio, size.y);

            if (ratio > 0.85f)
                _staminaFill.color = Color.green;
            else if (ratio > 0.30f)
                _staminaFill.color = Color.yellow;
            else
                _staminaFill.color = Color.red;
        }

        // Update name text live (in case it changed over network)
        if (!_editingName)
        {
            UpdateNameText();
        }

        // Resize on resolution change
        if (_lastScreenW != Screen.width || _lastScreenH != Screen.height)
        {
            RefreshLayout();
        }
    }

    private int _lastScreenW = -1;
    private int _lastScreenH = -1;

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void EnsureCanvas()
    {
        var existing = GameObject.Find("PlayerHUDCanvas");
        if (existing != null)
        {
            _canvas = existing.GetComponent<Canvas>();
            return;
        }

        var go = new GameObject("PlayerHUDCanvas");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
    }

    private void BuildUI()
    {
        // Root stamina panel (bottom-left)
        var panel = new GameObject("StaminaPanel");
        panel.transform.SetParent(_canvas.transform, false);
        _rootPanel = panel.AddComponent<RectTransform>();
        _rootPanel.anchorMin = new Vector2(0f, 0f);
        _rootPanel.anchorMax = new Vector2(0f, 0f);
        _rootPanel.pivot = new Vector2(0f, 0f);

        // Background
        var bg = new GameObject("StaminaBG");
        bg.transform.SetParent(_rootPanel, false);
        _staminaBg = bg.AddComponent<Image>();
        _staminaBg.color = new Color(0f, 0f, 0f, 0.5f);
        var bgRt = _staminaBg.rectTransform;
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot = new Vector2(0f, 0f);

        // Fill
        var fill = new GameObject("StaminaFill");
        fill.transform.SetParent(_rootPanel, false);
        _staminaFill = fill.AddComponent<Image>();
        _staminaFill.color = Color.green;
        var fillRt = _staminaFill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 0f);
        fillRt.pivot = new Vector2(0f, 0f);

        // Name button (above stamina)
        var nameBtnGO = new GameObject("PlayerNameButton");
        nameBtnGO.transform.SetParent(_canvas.transform, false);
        _nameButton = nameBtnGO.AddComponent<Button>();
        var btnImage = nameBtnGO.AddComponent<Image>();
        btnImage.color = new Color(0f, 0f, 0f, 0.25f);
        var btnRt = nameBtnGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 0f);
        btnRt.anchorMax = new Vector2(0f, 0f);
        btnRt.pivot = new Vector2(0f, 0f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(nameBtnGO.transform, false);
        _nameText = textGO.AddComponent<Text>();
        _nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _nameText.alignment = TextAnchor.MiddleLeft;
        _nameText.color = Color.white;
        var textRt = _nameText.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(8f, 0f);
        textRt.offsetMax = new Vector2(-8f, 0f);

        _nameButton.onClick.AddListener(OnNameClicked);

        // Hidden input field for editing name
        var inputGO = new GameObject("NameInput");
        inputGO.transform.SetParent(_canvas.transform, false);
        _nameInput = inputGO.AddComponent<InputField>();
        var inputImage = inputGO.AddComponent<Image>();
        inputImage.color = new Color(0f, 0f, 0f, 0.6f);
        var inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        var inputText = inputTextGO.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleLeft;
        _nameInput.textComponent = inputText;
        _nameInput.characterLimit = 32;
        _nameInput.onEndEdit.AddListener(OnNameEdited);
        _nameInput.gameObject.SetActive(false);
    }

    private void RefreshLayout()
    {
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;

        float w = Mathf.Round(Screen.width * widthPercent);
        float h = Mathf.Round(Screen.height * heightPercent);

        _rootPanel.sizeDelta = new Vector2(w, h);
        _rootPanel.anchoredPosition = new Vector2(marginLeft, marginBottom);

        // BG and Fill match bar rect
        if (_staminaBg != null)
        {
            _staminaBg.rectTransform.sizeDelta = new Vector2(w, h);
            _staminaBg.rectTransform.anchoredPosition = Vector2.zero;
        }
        if (_staminaFill != null)
        {
            _staminaFill.rectTransform.sizeDelta = new Vector2(w, h);
            _staminaFill.rectTransform.anchoredPosition = Vector2.zero;
        }

        // Name button sits above the stamina bar
        float nameHeight = Mathf.Max(22f, h * 1.5f);
        var btnRt = _nameButton.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(w, nameHeight);
        btnRt.anchoredPosition = new Vector2(marginLeft, marginBottom + h + 6f);
        var inputRt = _nameInput.GetComponent<RectTransform>();
        inputRt.sizeDelta = new Vector2(w, nameHeight);
        inputRt.anchoredPosition = btnRt.anchoredPosition;
        inputRt.anchorMin = btnRt.anchorMin;
        inputRt.anchorMax = btnRt.anchorMax;
        inputRt.pivot = btnRt.pivot;
    }

    private void OnNameClicked()
    {
        if (_editingName) return;
        _editingName = true;
        _nameInput.text = playerName != null ? playerName.PlayerName : _nameText.text;
        _nameInput.gameObject.SetActive(true);
        _nameInput.ActivateInputField();
        _nameInput.Select();
    }

    private void OnNameEdited(string newValue)
    {
        _editingName = false;
        _nameInput.gameObject.SetActive(false);
        if (playerName != null)
        {
            playerName.SetPlayerName(string.IsNullOrWhiteSpace(newValue) ? playerName.PlayerName : newValue.Trim());
        }
        UpdateNameText();
    }

    private void UpdateNameText()
    {
        if (_nameText == null) return;
        _nameText.text = playerName != null ? playerName.PlayerName : "Player";
    }
}


