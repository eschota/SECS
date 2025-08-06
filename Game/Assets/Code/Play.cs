using UnityEngine;

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
    
    void Awake()
    {
        // Получаем ссылки на камеру и Creator
        _cam = Camera.main;
        _creator = FindObjectOfType<Creator>();
    }

    void Update()
    {
        SimulaterPlay();
    }
    
    void SimulaterPlay()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.stateCreate)
            {
                // Переключаемся в режим симуляции
                currentState = GameState.stateSimulatePlay;
                CreateHull();
                Debug.Log("Switched to simulation mode");
            }
            else
            {
                // Возвращаемся в режим создания
                currentState = GameState.stateCreate;
                ResetHull();
                Debug.Log("Switched to creation mode");
            }
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
}
