using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.Networking;

public class main : MonoBehaviour
{
    public static main Instance { get; private set; }
    public static string QueueUrl = "https://renderfin.com/api-game-queue/";
    public static string GameUrl = "https://renderfin.com/api-game-game/";
    public static Lobby lobby;

    public static event Action<State> ChangeState;
    public enum State
    {
        Server_connect,
        Lobby,
        Search,
        MatchFound,
        MatchStarted,
        Match,
        Exit,

        Match_Result_win,
        Match_Result_lose,
        Match_Result_surrender,
        Settings_graph,
        Settings_audio,
        Settings_controls,
        Settings_language,
        Settings_credits,
        Settings_about,
        Settings_exit,

    }
    [SerializeField]private State currentState;

    public void SetState(State state)
    {
        currentState = state;
        ChangeState?.Invoke(state);
        
        switch (state)
        {
            case State.Server_connect:
            CheckServerConnection();
                break;
            case State.Lobby:
                break;
        }
    }
    void CheckServerConnection()
    {
        StartCoroutine(CheckServerConnectionRoutine());
    }
    IEnumerator CheckServerConnectionRoutine()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://renderfin.com/api-game-lobby");
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Server connection failed: " + request.error);
            yield break;
        }
        yield return new WaitForSeconds(1f);
        SetState(State.Lobby);
    }
    void Awake()
    {
        Instance = this;
        // Загружаем сцену UI в режиме Additive и после загрузки инициализируем UI
        StartCoroutine(LoadUIAndInitLobby());
    }

    IEnumerator LoadUIAndInitLobby()
    {
        var async = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        while (!async.isDone)
            yield return null;
        // Ждём, пока синглтон ui_lobby появится
        while (ui_lobby.Instance == null)
            yield return null;

        GameObject go = new GameObject("Lobby");
        main.lobby = go.AddComponent<Lobby>();
        ui_lobby.Instance.InitLobby();
        // Запускаем Lobby (если нужно)
        Lobby lobby = FindObjectOfType<Lobby>();
        if (lobby != null)
        {
            // Можно вызвать метод инициализации, если потребуется
            // lobby.Init();
        }
         SetState(State.Server_connect);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
 
