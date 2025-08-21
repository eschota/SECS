using UnityEngine; 
using System.Collections.Generic;
using System;

public class Play : MonoBehaviour
{
    public enum State
    {
        Start,
        Create,
        SimulateLocal,
        SimulateOnline,
        
    }
    
    [SerializeField] public Camera _cam;
    [SerializeField] public Creator _creator;
    
    private State _currentState = State.Create;
    public State currentState
    {
        get
        {
            return _currentState;
        }
        set
        {
            _currentState = value;
            OnPlayStateChange?.Invoke(_currentState);
        }
    }

    public static event Action<State> OnPlayStateChange;
    private bool isSimulationActive = false;
 
    private float localTimer = 0f;
    private float localThreshold = 0.5f;
    public static Play i;
    void Awake()
    {
        i = this;
        // Получаем ссылки на камеру и Creator
        _cam = Camera.main;
        _creator = FindFirstObjectByType<Creator>();
        
        // Инициализируем сетевые компоненты 
    }

    void Update()
    {
        localTimer += Time.deltaTime;
        if (UI_Canvas.i.currentState == UI_Canvas.UI_State.Chatting) return;
        SimulaterPlay(); 
        HandleStatusChange();
    }
    
    void SimulaterPlay()
    {
       
        if (Input.GetKeyDown(KeyCode.Space) && localTimer >= localThreshold)
        {
            localTimer = 0f; // Сбрасываем таймер
            TogglePlayMode();
        }
    }

public void TogglePlayMode()
{
    var spawner = FindFirstObjectByType<MachineSpawnClient>();

    if (currentState == State.Create)
    {
        // → сет. симуляция
        currentState = State.SimulateOnline;
        if (spawner) spawner.RequestSpawnFromCreator();
        Debug.Log("Switched to simulation mode");
        OnPlayStateChange?.Invoke(currentState);
    }
    else
    {
        // → обратно в конструктор: удаляем свою машину и возвращаем конструктор
        if (spawner) spawner.RequestDespawnOwnedMachine();

        currentState = State.Create;
        ResetHull();
        Debug.Log("Switched to creation mode");
        OnPlayStateChange?.Invoke(currentState);
    }
}

    
     
    
   
    
    
    void OnGUI()
    {
        if (currentState == State.SimulateLocal)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== NETWORK CONTROLS ===");
            GUILayout.Label("H - Start Host");
            GUILayout.Label("C - Start Client");
            GUILayout.Label("S - Start Server");
            GUILayout.Label("D - Disconnect");
            GUILayout.Label("SPACE - Toggle Mode");
            GUILayout.Label($"Timer: {localTimer:F2}");
            GUILayout.EndArea();
        }
    }
    
    void CreateMachine()
    {
        if (_creator != null && _creator.cells.Count > 0)
        {
            









            isSimulationActive = true;
           
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
