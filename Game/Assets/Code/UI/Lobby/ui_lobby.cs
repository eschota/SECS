using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ui_lobby : MonoBehaviour
{
    public static ui_lobby Instance { get; private set; }
    [SerializeField] TextMeshProUGUI text_nickname;
    [SerializeField] TextMeshProUGUI game_version;
    [SerializeField] Button button_PlayNow;
    [SerializeField] Button button_Cancel;
    [SerializeField] TextMeshProUGUI button_play_now_text;


    private void OnChangeState(main.State state)
    {
        switch (state)
        {   
            case main.State.Lobby:
                InitLobby();
                break;
        }
    }
    private void OnDestroy()
    {
        main.ChangeState -= OnChangeState;
    }

    private Coroutine _searchTimerCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        main.ChangeState += OnChangeState;
    }


    public void InitLobby()
    {
        // Initially hide cancel button
        button_Cancel.gameObject.SetActive(false);
        button_Cancel.onClick.AddListener(OnCancelClick);
        string nickname = "Player";
#if UNITY_WEBGL && !UNITY_EDITOR
        // Получаем никнейм из Lobby.User (инициализировано из cookie)
        Lobby lobby = FindObjectOfType<Lobby>();
        if (lobby != null && lobby.currentUser != null && !string.IsNullOrEmpty(lobby.currentUser.nick_name))
        {
            nickname = lobby.currentUser.nick_name;
        }
#elif UNITY_EDITOR
        // В редакторе берём имя из переменной окружения (например, USERNAME)
        nickname = System.Environment.GetEnvironmentVariable("USERNAME") ?? "EditorPlayer";
#endif
        if (text_nickname != null)
            text_nickname.text = nickname;
        if (game_version != null)
        {
            try
            {
                Debug.Log("[BuildInfo] Attempting to load build_info TextAsset...");
                TextAsset buildInfoAsset = Resources.Load<TextAsset>("build_info");
                if (buildInfoAsset != null)
                {
                    Debug.Log($"[BuildInfo] Raw build_info text: {buildInfoAsset.text}");
                    string[] parts = buildInfoAsset.text.Split('|');
                    Debug.Log($"[BuildInfo] Parsed parts count: {parts.Length}");
                    if (parts.Length == 2)
                    {
                        Debug.Log($"[BuildInfo] Setting version text: {parts[0]} ({parts[1]})");
                        game_version.text = $"{parts[0]} ({parts[1]})";
                    }
                    else
                    {
                        Debug.LogWarning("[BuildInfo] Unexpected format in build_info, using raw text");
                        game_version.text = buildInfoAsset.text;
                    }
                }
                else
                {
                    Debug.LogError("[BuildInfo] build_info TextAsset not found in Resources");
                    game_version.text = "N/A";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildInfo] Exception while loading build info: {ex}");
                game_version.text = "Error";
            }
        }
        // Hook PlayNow button
        button_PlayNow.onClick.AddListener(OnPlayNowClick);
    }

    // Handle Play Now click: start search
    private void OnPlayNowClick()
    {
        Debug.Log("[ui_lobby] Play Now button clicked");
        Lobby.Instance.AddPlayerToQueue();
    }

    // Handle Cancel click: stop search
    private void OnCancelClick()
    {
        Debug.Log("[ui_lobby] Cancel button clicked");
        Lobby.Instance.RemovePlayerFromQueue();
        OnSearchCanceled();
    }

    public void OnSearchStarted()
    {
        // Show cancel, hide play, start timer
        button_PlayNow.gameObject.SetActive(false);
        button_Cancel.gameObject.SetActive(true);
        button_play_now_text.text = "00:00";
        _searchTimerCoroutine = StartCoroutine(SearchTimerCoroutine());
    }

    public void OnSearchCanceled()
    {
        // Hide cancel, show play, stop timer, reset text
        button_Cancel.gameObject.SetActive(false);
        button_PlayNow.gameObject.SetActive(true);
        if (_searchTimerCoroutine != null)
            StopCoroutine(_searchTimerCoroutine);
        button_play_now_text.text = "Play Now";
    }

    public void OnMatchFound(string gameId)
    {
        // Notify state machine
        main.Instance.SetState(main.State.MatchFound);
        Debug.Log($"[ui_lobby] Match found: {gameId}");
        // Load match scene
        main.Instance.SetState(main.State.MatchStarted);
        // Reset UI
        OnSearchCanceled();
        // Load 1x1 scene additively
        SceneManager.LoadScene("1x1", LoadSceneMode.Additive);
    }

    private IEnumerator SearchTimerCoroutine()
    {
        float startTime = Time.time;
        while (true)
        {
            float elapsed = Time.time - startTime;
            TimeSpan ts = TimeSpan.FromSeconds(elapsed);
            button_play_now_text.text = string.Format("{0:00}:{1:00}", (int)ts.TotalMinutes, ts.Seconds);
            yield return new WaitForSeconds(1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
