using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine.SceneManagement;


public class Creator : MonoBehaviour
{
    public static Creator instance;
    public enum ActionType
    {
        Create,
        Delete,
        Move,
        Rotate,
    }
    public static event Action<ActionType, io_base> AllActions;
    void Start()
    {

    }
    void Awake()
    { if ((prefabs == null || prefabs.Count == 0) && _prefabs != null && _prefabs.Count > 0)
        prefabs = new List<io_base>(_prefabs);

    // если всё ещё пусто — как раньше, грузим из Resources
    if (prefabs == null || prefabs.Count == 0)
        LoadPrefabs();

    // детерминируем порядок (у всех клиентов одинаково)
    prefabs.Sort((a, b) => string.Compare(a.io_base_cell_type.ToString(), b.io_base_cell_type.ToString(), System.StringComparison.Ordinal));

        instance = this;
        LoadPrefabs();
        PlacePrefabs();
        CreateCameraWitPivot();
        LoadUI();
    }
    void Update()
    {
        if (Play.i.currentState != Play.State.Create) return;


        // Проверяем состояние Play - если в режиме симуляции, не выполняем создание
        if (_play != null && _play.currentState == Play.State.Create)
        {
            CreateCell();
            DeleteCell();
        }
    }
    public List<io_base> prefabs = new List<io_base>();
    [SerializeField] public cam _cam;
    [SerializeField] public Play _play;
    public List<io_base> cells = new List<io_base>();
    [SerializeField] private List<Shader> _shaders = new List<Shader>();
    [SerializeField] private List<io_base> _prefabs = new List<io_base>();
    [SerializeField] private List<io_base_SO> _statuses = new List<io_base_SO>();
    [SerializeField] private List<Material> _materials = new List<Material>();
    public bool SnapGrid = true;
    private io_base _current_prefab;
    public io_base current_prefab_to_chabge; 
    public io_base current_prefab
    {
        get
        {
            if (_current_prefab == null)
            {
                // Проверяем, что у нас есть префаб для создания
                if (current_prefab_to_chabge == null)
                {
                    if (prefabs.Count > 0)
                    {
                        current_prefab_to_chabge = prefabs[0];
                    }
                    else
                    {
                        Debug.LogError("No prefabs available to create!");
                        return null;
                    }
                }
                
                var a = Instantiate(current_prefab_to_chabge, transform);
                _current_prefab = a;
                _current_prefab.transform.position = _cam.target_pivot_position;
                _current_prefab.target_world_position = _cam.target_pivot_position;
                _current_prefab.target_world_rotation = Quaternion.identity;
                _current_prefab.transform.localScale=Vector3.one*0.01f;
                _current_prefab.name = "Current Create";
                // Убеждаемся, что текущий префаб остается в иерархии Creator
                _current_prefab.transform.SetParent(transform);
                _current_prefab.Status = io_base.io_base_status.Creating;
                cells.Add(_current_prefab);
            }
            return _current_prefab;
        }
        set
        {
            if (value != null)
            { 
                _current_prefab = null;
            }
            else
            {
                _current_prefab = null;
            }

        }
    }


   // private io_base last_prefab_over;
    void CreateCell()
    {
        LayerMask layer_mask = LayerMask.GetMask("io_base");

        // Обработка нажатия кнопки мыши
        if (Input.GetMouseButtonDown(0))
        {
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit2, 1000, layer_mask))
            {
                return;
            }


            // Создание клетки при нажатии
            if (current_prefab != null && current_prefab.Status != io_base.io_base_status.Intersected)
            {
                // Сохраняем состояние для Undo перед созданием клетки
                SaveStateForUndo();
                
                AllActions?.Invoke(ActionType.Create, current_prefab);

                // Убеждаемся, что клетка остается в иерархии Creator, а не попадает в пивот
                current_prefab.transform.SetParent(transform);

                // Смещаем пивот к точке создания клетки
                _cam.target_pivot_position = current_prefab.transform.position;
                current_prefab.Status = io_base.io_base_status.Placing;
                current_prefab = null;
            }
        }

        // Обработка отпускания кнопки мыши
        if (Input.GetMouseButtonUp(0))
        {
//            Debug.Log("Mouse button up detected!");
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, layer_mask))
        {
            if (SnapGrid)
            {
                var cell_to_check = hit.collider.GetComponent<io_cell>();

                // if (cell_to_check.target_io_base != last_prefab_over)
                // { 
                //     last_prefab_over = cell_to_check.target_io_base;
                //     last_prefab_over.statusTransitionTimer = 0;
                // }
                if (!cell_to_check.possible_to_place)
                {
                    if (current_prefab != null)
                        current_prefab.Status = io_base.io_base_status.Hidden;
                    return;
                }


                // Определяем направление касания (normal)
                Vector3 hitNormal = hit.normal;

                // Получаем позицию центра коллайдера объекта, который мы касаемся
                // Коллайдер гарантированно стоит в сетке 1x1x1
                Vector3 colliderCenter = hit.collider.transform.position;

                // Вычисляем смещение в зависимости от направления касания
                Vector3 offset = Vector3.zero;

                // Определяем, с какой стороны куба мы касаемся
                if (Mathf.Abs(hitNormal.x) > 0.5f) // Касаемся боковой грани по X
                {
                    offset.x = hitNormal.x > 0 ? 1f : -1f;
                }
                else if (Mathf.Abs(hitNormal.y) > 0.5f) // Касаемся грани по Y
                {
                    offset.y = hitNormal.y > 0 ? 1f : -1f;
                }
                else if (Mathf.Abs(hitNormal.z) > 0.5f) // Касаемся грани по Z
                {
                    offset.z = hitNormal.z > 0 ? 1f : -1f;
                }

                // Размещаем объект в позиции, кратной 1x1x1, относительно центра коллайдера
                Vector3 targetPosition = colliderCenter + offset;
                targetPosition = new Vector3(Mathf.Round(targetPosition.x), Mathf.Round(targetPosition.y), Mathf.Round(targetPosition.z));

                if (current_prefab != null)
                {
                    if ((targetPosition - current_prefab.target_world_position).sqrMagnitude > 0.1f)
                    {
                        current_prefab.statusTransitionTimer = 0;
                    }

                    current_prefab.target_world_position = targetPosition;
                    // check direction of hitNormal and rotate current_prefab to this direction
                    Vector3 up = hitNormal.normalized;
                    Quaternion alignUp = Quaternion.FromToRotation(Vector3.up, up);

                    // 2) дополнительный поворот по локальной Y (yaw) кратно 90°
                    Quaternion yawRot = Quaternion.Euler(0f, 90f * current_prefab.yawSteps, 0f);

                    // 3) финальный поворот
                    Quaternion finalRot = alignUp * yawRot;
                    current_prefab.target_world_rotation = finalRot;   // check intersections 
                    foreach (var cell in current_prefab.target_cells)
                    {
                        // Применяем поворот к локальной позиции клетки
                        Vector3 rotatedLocalPosition = current_prefab.target_world_rotation * cell.target_local_position;
                        Vector3 worldCellPosition = current_prefab.target_world_position + rotatedLocalPosition;

                        foreach (var b in cells)
                        {
                            if (b != current_prefab)
                                foreach (var cell2 in b.target_cells)
                                {
                                    Vector3 rotatedLocalPosition2 = b.target_world_rotation * cell2.target_local_position;
                                    Vector3 worldCellPosition2 = b.target_world_position + rotatedLocalPosition2;
                                    if ((worldCellPosition - worldCellPosition2).sqrMagnitude < 0.1f)
                                    {
                                        current_prefab.Status = io_base.io_base_status.Intersected;
                                        if (Input.GetKeyDown(KeyCode.R)) current_prefab.Rotate();

                                        return;
                                    }
                                }

                        }
                    }
                    current_prefab.Status = io_base.io_base_status.Creating;
                    if (Input.GetKeyDown(KeyCode.R)) current_prefab.Rotate();
                }

            }
            else
            {
                //  current_prefab.Status = io_base.io_base_status.Hidden;
            }
        }
        else
        {
            if (current_prefab != null && current_prefab.Status != io_base.io_base_status.Intersected)
                current_prefab.Status = io_base.io_base_status.Hidden;
            // Если луч не попал никуда, можно скрыть объект или разместить в дефолтной позиции

        }
    }
    void DeleteCell()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, LayerMask.GetMask("io_base")))
            {
                if(hit.collider.gameObject.GetComponent<io_cell>().target_io_base.Status == io_base.io_base_status.Placing)
                {
                    SaveStateForUndo();
                    Destroy(hit.collider.gameObject.transform.parent.gameObject);
                }
            }
            
        }
    }
    void LoadPrefabs()
    {
        var prefabs_list = Resources.LoadAll<io_base>("Create");
        foreach (var prefab in prefabs_list)
        {
            prefabs.Add(prefab);
        }
        current_prefab_to_chabge = prefabs[0];
    }
    void CreateCameraWitPivot()
    {

        _cam = gameObject.AddComponent<cam>();
        _play = gameObject.AddComponent<Play>();
    }
    public Vector3 current_prefab_position = Vector3.zero;
     
    void PlacePrefabs()
    {
        foreach (var prefab in prefabs)
        {
            var new_prefab = Instantiate(prefab, transform);
            cells.Add(new_prefab);
            new_prefab.Status = io_base.io_base_status.Placing;
            return;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _shaders.Clear();
        var shaders_in_folder = Resources.LoadAll<Shader>("Shaders");
        if (_shaders.Count != shaders_in_folder.Length)
        {
            foreach (var shader in shaders_in_folder)
            {
                if (!_shaders.Contains(shader))
                {
                    _shaders.Add(shader);
                }
            }
        }

        _prefabs.Clear();
        var prefabs_in_folder = Resources.LoadAll<io_base>("Create");
        if (_prefabs.Count != prefabs_in_folder.Length)
        {
            foreach (var prefab in prefabs_in_folder)
            {
                if (!_prefabs.Contains(prefab))
                {
                    _prefabs.Add(prefab);
                }
            }
        }

        _statuses.Clear();
        var statuses_in_folder = Resources.LoadAll<io_base_SO>("Statuses");
        if (_statuses.Count != statuses_in_folder.Length)
        {
            foreach (var status in statuses_in_folder)
            {
                if (!_statuses.Contains(status))
                {
                    _statuses.Add(status);
                }
            }
        }
        
        _materials.Clear();
        var materials_in_folder = Resources.LoadAll<Material>("mats");
        if (_materials.Count != materials_in_folder.Length)
        {
            foreach (var material in materials_in_folder)
            {
                if (!_materials.Contains(material))
                {
                    _materials.Add(material);
                }
            }
        }
    }
#endif
    private void LoadUI()
    {
        // load scene UI additively
        SceneManager.LoadScene("UI", LoadSceneMode.Additive);
        StartCoroutine(WaitForUIAndDestroy());
    }

    private System.Collections.IEnumerator WaitForUIAndDestroy()
    {
        // Ждем пока UI_Canvas инициализируется
        while (UI_Canvas.i == null)
        {
            yield return null;
        }

        // Теперь безопасно удаляем камеру UI
        if (UI_Canvas.i.ui_camera != null)
        {
            Destroy(UI_Canvas.i.ui_camera.gameObject);
        }
        UI_Canvas.UI_ChangeState += OnSubTypeSelected; 
    }

    private void OnSubTypeSelected(UI_Button button)
    {
        if (button == null) return;
        if (button.gameObject.name == "ButtonClearMachine")
        {
            ClearMachine();
            return;
        }
        if (button.gameObject.name == "ButtonSave")
        {
            SaveMachine();
            return;
        } if (button.gameObject.name == "ButtonLoad")
        {
            LoadMachine();
            return;
        } if (button.gameObject.name == "ButtonUnDo")
        {
            UndoMachine();
            return;
        } if (button.gameObject.name == "ButtonReDo")
        {
            RedoMachine();
            return;
        } if (button.gameObject.name == "ButtonPlay")
        {
            PlayMachine();
            return;
        }
//        Debug.Log("OnSubTypeSelected: " + button.name);
        current_prefab_to_chabge = button.Item.prefab;
        if (current_prefab != null)
        {
            current_prefab_position = current_prefab.transform.position;
            Destroy(current_prefab.gameObject);  
        }
        current_prefab = null;
    }
    void OnDestroy()
    {
        UI_Canvas.UI_ChangeState -= OnSubTypeSelected;
    }
    private void ClearMachine()
    {
        // Сохраняем состояние для Undo перед очисткой
        if (cells.Count > 0)
        {
            SaveStateForUndo();
        }
        
        ClearMachineInternal();
    }

    private void ClearMachineInternal()
    {
        foreach (var cell in cells)
        {
            Destroy(cell.gameObject);
        }
        cells.Clear();
        current_prefab = null;
        // НЕ обнуляем current_prefab_to_chabge, чтобы можно было создавать новые префабы
        if (current_prefab_to_chabge == null && prefabs.Count > 0)
        {
            current_prefab_to_chabge = prefabs[0];
        }
        _cam.target_pivot_position = Vector3.zero;
        _cam.target_pivot_rotation = Quaternion.identity;        
        _cam.target_pivot_position = Vector3.zero;
        _cam.target_pivot_rotation = Quaternion.identity;
    }

    // Структура для сериализации машины
    [System.Serializable]
    public class MachineData
    {
        public List<CellData> cells = new List<CellData>();
        public Vector3 cameraPivotPosition;
        public Quaternion cameraPivotRotation;
    }

    [System.Serializable]
    public class CellData
    {
        public int prefabIndex;
        public Vector3 position;
        public Quaternion rotation;
        public int status;
        public string name;
    }

    // Undo/Redo система
    private List<MachineData> undoStack = new List<MachineData>();
    private List<MachineData> redoStack = new List<MachineData>();
    private const int maxUndoSteps = 20;

    private void SaveMachine()
    {
        // Создаем автосейв с ротацией
        int currentCounter = PlayerPrefs.GetInt("auto_save_counter", 0);
        currentCounter = (currentCounter % 5) + 1; // Ротация от 1 до 5
        
        string saveKey = "auto_save_" + currentCounter;
        MachineData machineData = CreateMachineData();
        string json = JsonUtility.ToJson(machineData, true);
        
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetInt("auto_save_counter", currentCounter);
        PlayerPrefs.Save();
        
        Debug.Log($"Machine saved to {saveKey}");
    }

    private void LoadMachine()
    {
        // Ищем последний автосейв
        int lastCounter = PlayerPrefs.GetInt("auto_save_counter", 0);
        if (lastCounter == 0)
        {
            Debug.Log("No auto saves found");
            return;
        }
        
        string loadKey = "auto_save_" + lastCounter;
        string json = PlayerPrefs.GetString(loadKey, "");
        
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("No save data found");
            return;
        }
        
        try
        {
            // Сохраняем текущее состояние для Undo перед загрузкой
            SaveStateForUndo();
            
            MachineData machineData = JsonUtility.FromJson<MachineData>(json);
            LoadMachineData(machineData);
            Debug.Log($"Machine loaded from {loadKey}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading machine: {e.Message}");
        }
    }

    private void UndoMachine()
    {
        if (undoStack.Count == 0)
        {
            Debug.Log("Nothing to undo");
            return;
        }

        // Сохраняем текущее состояние в redo стек
        MachineData currentState = CreateMachineData();
        redoStack.Add(currentState);
        
        // Ограничиваем размер redo стека
        if (redoStack.Count > maxUndoSteps)
        {
            redoStack.RemoveAt(0);
        }

        // Загружаем предыдущее состояние
        MachineData previousState = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        
        LoadMachineData(previousState);
        Debug.Log("Undo performed");
    }

    private void RedoMachine()
    {
        if (redoStack.Count == 0)
        {
            Debug.Log("Nothing to redo");
            return;
        }

        // Сохраняем текущее состояние в undo стек
        MachineData currentState = CreateMachineData();
        undoStack.Add(currentState);
        
        // Ограничиваем размер undo стека
        if (undoStack.Count > maxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }

        // Загружаем следующее состояние
        MachineData nextState = redoStack[redoStack.Count - 1];
        redoStack.RemoveAt(redoStack.Count - 1);
        
        LoadMachineData(nextState);
        Debug.Log("Redo performed");
    }

    private void PlayMachine()
    {
        if (_play == null)
        {
            Debug.LogError("Play component not found!");
            return;
        }

        // Используем публичный метод из Play.cs
        _play.TogglePlayMode();
        Debug.Log("Play mode toggled via button");
    }

    private MachineData CreateMachineData()
    {
        MachineData data = new MachineData();
        
        // Сохраняем позицию и поворот камеры
        if (_cam != null)
        {
            data.cameraPivotPosition = _cam.target_pivot_position;
            data.cameraPivotRotation = _cam.target_pivot_rotation;
        }
        
        // Сохраняем все клетки кроме текущей создаваемой
        foreach (var cell in cells)
        {
            if (cell != null && cell.Status != io_base.io_base_status.Creating)
            {
                CellData cellData = new CellData();
                
                // Находим индекс префаба
                cellData.prefabIndex = FindPrefabIndex(cell);
                cellData.position = cell.target_world_position;
                cellData.rotation = cell.target_world_rotation;
                cellData.status = (int)cell.Status;
                cellData.name = cell.name;
                
                data.cells.Add(cellData);
            }
        }
        
        return data;
    }

    private void LoadMachineData(MachineData data)
    {
        // Очищаем текущую машину без сохранения состояния для Undo
        ClearMachineInternal();
        
        // Восстанавливаем позицию камеры
        if (_cam != null)
        {
            _cam.target_pivot_position = data.cameraPivotPosition;
            _cam.target_pivot_rotation = data.cameraPivotRotation;
        }
        
        // Восстанавливаем клетки
        foreach (var cellData in data.cells)
        {
            if (cellData.prefabIndex >= 0 && cellData.prefabIndex < prefabs.Count)
            {
                var newCell = Instantiate(prefabs[cellData.prefabIndex], transform);
                newCell.target_world_position = cellData.position;
                newCell.target_world_rotation = cellData.rotation;
                newCell.transform.position = cellData.position;
                newCell.transform.rotation = cellData.rotation;
                newCell.Status = (io_base.io_base_status)cellData.status;
                newCell.name = cellData.name;
                
                cells.Add(newCell);
            }
        }
    }

    public int FindPrefabIndex(io_base cell)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i].io_base_cell_type == cell.io_base_cell_type)
            {
                return i;
            }
        }
        return 0; // Возвращаем первый префаб если не найден
    }

    // Сохраняем состояние для Undo при важных действиях
    private void SaveStateForUndo()
    {
        MachineData currentState = CreateMachineData();
        undoStack.Add(currentState);
        
        // Ограничиваем размер undo стека
        if (undoStack.Count > maxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }
        
        // Очищаем redo стек при новом действии
        redoStack.Clear();
    }
  
}
