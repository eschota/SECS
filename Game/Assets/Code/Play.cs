using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;

public class Play : MonoBehaviour
{
    public enum GameState
    {
        stateCreate,
        stateSimulatePlay
    }
    
    [SerializeField] public Camera _cam;
    [SerializeField] public Creator _creator;
    
    public GameState currentState = GameState.stateCreate;
    private bool isSimulationActive = false;
    
    // Сетевые компоненты
    private NetworkManager networkManager;
    // The type or namespace name 'NetworkObjectSpawner' could not be found, commenting out.
    // private NetworkObjectSpawner spawner;
    // The type or namespace name 'NetworkPlayerManager' could not be found, commenting out.
    // private NetworkPlayerManager playerManager;
    
    // Таймер для предотвращения быстрой смены состояний
    private float localTimer = 0f;
    private float localThreshold = 0.5f;
    
    void Awake()
    {
        // Получаем ссылки на камеру и Creator
        _cam = Camera.main;
        _creator = FindObjectOfType<Creator>();
        
        // Инициализируем сетевые компоненты
        InitializeNetworkComponents();
    }

    void Update()
    {
        localTimer += Time.deltaTime;
        SimulaterPlay();
        HandleNetworkInputs();
        HandleStatusChange();
    }
    
    void SimulaterPlay()
    {
        if (Input.GetKeyDown(KeyCode.Space) && localTimer >= localThreshold)
        {
            localTimer = 0f; // Сбрасываем таймер
            
            if (currentState == GameState.stateCreate)
            {
                // Переключаемся в режим симуляции
                currentState = GameState.stateSimulatePlay;
                CreateHull();
                StartNetworkGame();
                Debug.Log("Switched to simulation mode");
            }
            else
            {
                // Возвращаемся в режим создания
                currentState = GameState.stateCreate;
                ResetHull();
                StopNetworkGame();
                Debug.Log("Switched to creation mode");
            }
        }
    }
    
    void InitializeNetworkComponents()
    {
        // Создаем NetworkManager если его нет
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManager");
            networkManager = networkManagerObj.AddComponent<NetworkManager>();
            
            // Настраиваем NetworkConfig
            NetworkConfig networkConfig = new NetworkConfig();
            networkConfig.PlayerPrefab = Resources.Load<GameObject>("Create/cell");
            
            // Создаем UnityTransport и настраиваем его
            var transport = networkManagerObj.AddComponent<UnityTransport>();
            networkConfig.NetworkTransport = transport;
            
            // Отключаем управление сценой, чтобы избежать конфликтов
            networkConfig.EnableSceneManagement = false;
            
            // Теперь присваиваем полностью настроенный networkConfig
            networkManager.NetworkConfig = networkConfig;
            
            Debug.Log("NetworkManager created with UnityTransport");
        }
        
        // Создаем NetworkObjectSpawner
        // if (spawner == null)
        // {
        //     GameObject spawnerObj = new GameObject("NetworkObjectSpawner");
        //     spawner = spawnerObj.AddComponent<NetworkObjectSpawner>();
        // }
        
        // Создаем NetworkPlayerManager
        // if (playerManager == null)
        // {
        //     GameObject playerManagerObj = new GameObject("NetworkPlayerManager");
        //     playerManager = playerManagerObj.AddComponent<NetworkPlayerManager>();
        // }
        
        Debug.Log("Network components initialized");
    }
    
    void HandleNetworkInputs()
    {
        if (currentState == GameState.stateSimulatePlay)
        {
            // Клавиши для сетевого управления
            if (Input.GetKeyDown(KeyCode.H)) // Host
            {
                StartHost();
            }
            if (Input.GetKeyDown(KeyCode.C)) // Client
            {
                StartClient();
            }
            if (Input.GetKeyDown(KeyCode.S)) // Server
            {
                StartServer();
            }
            if (Input.GetKeyDown(KeyCode.D)) // Disconnect
            {
                Disconnect();
            }
        }
    }
    
    void StartNetworkGame()
    {
        if (networkManager != null && !networkManager.IsListening)
        {
            Debug.Log("Starting network game...");
            // Автоматически создаем комнату как хост
            StartHost();
        }
    }
    
    void StopNetworkGame()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            Debug.Log("Stopping network game...");
            networkManager.Shutdown();
        }
    }
    
    void StartHost()
    {
        if (networkManager != null && !networkManager.IsListening)
        {
            networkManager.StartHost();
            Debug.Log("Started as Host");
        }
    }
    
    void StartClient()
    {
        if (networkManager != null && !networkManager.IsListening)
        {
            networkManager.StartClient();
            Debug.Log("Started as Client");
        }
    }
    
    void StartServer()
    {
        if (networkManager != null && !networkManager.IsListening)
        {
            networkManager.StartServer();
            Debug.Log("Started as Server");
        }
    }
    
    void Disconnect()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
            Debug.Log("Disconnected from network");
        }
    }
    
    void OnGUI()
    {
        if (currentState == GameState.stateSimulatePlay)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== NETWORK CONTROLS ===");
            GUILayout.Label("H - Start Host");
            GUILayout.Label("C - Start Client");
            GUILayout.Label("S - Start Server");
            GUILayout.Label("D - Disconnect");
            GUILayout.Label("SPACE - Toggle Mode");
            GUILayout.Label($"Timer: {localTimer:F2}");
            GUILayout.Label($"Network Status: {(networkManager?.IsListening == true ? "Connected" : "Disconnected")}");
            GUILayout.EndArea();
        }
    }
    
    void CreateHull()
    {
        if (_creator != null && _creator.cells.Count > 0)
        {
            isSimulationActive = true;
            
            // Убираем текущую клетку в небо, чтобы она не мешала
            if (_creator.current_prefab != null)
            {
                _creator.current_prefab.transform.position = new Vector3(0, 100, 0);
                _creator.current_prefab.target_world_position = new Vector3(0, 100, 0);
                Debug.Log("Current cell moved to sky");
            }

            foreach (var cell in _creator.cells)
            {
                cell.TurnColliders(true);
                
                cell.targetRigidbody.isKinematic = false;
                cell.targetRigidbody.useGravity = true;
            }
        }
    }
    
    void ResetHull()
    {
        if (_creator != null && _creator.cells.Count > 0)
        {
            isSimulationActive = false;

            // Возвращаем текущую клетку в нормальное состояние
            if (_creator.current_prefab != null)
            {
                _creator.current_prefab.transform.position = new Vector3(0, 0, 0);
                _creator.current_prefab.target_world_position = new Vector3(0, 0, 0);
                Debug.Log("Current cell returned to normal position");
                _creator.current_prefab.TurnColliders(false);
                _creator.current_prefab.targetRigidbody.isKinematic = true; 
            }
            
            foreach (var cell in _creator.cells)
            {
                if (cell.targetRigidbody != null)
                {
                    // Отключаем физику и возвращаем в исходную позицию
                    cell.targetRigidbody.isKinematic = true; 
                    
                    Debug.Log($"Cell {cell.name} reset to original position");
                }
            }
        }
    }
    
    void HandleStatusChange()
    {
        if (_creator == null || _creator.cells.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetAllCellsStatus(io_base.io_base_status.Selected);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SetAllCellsStatus(io_base.io_base_status.Hovered);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SetAllCellsStatus(io_base.io_base_status.Dragging);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetAllCellsStatus(io_base.io_base_status.Placing);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SetAllCellsStatus(io_base.io_base_status.Physics);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetAllCellsStatus(io_base.io_base_status.PhysicsToTargetPositions);
        }
    }

    void SetAllCellsStatus(io_base.io_base_status newStatus)
    {
        foreach (var cell in _creator.cells)
        {
            if (cell != null)
            {
                cell.Status = newStatus;
            }
        }
        Debug.Log($"Set all cells to {newStatus}");
    }
}
