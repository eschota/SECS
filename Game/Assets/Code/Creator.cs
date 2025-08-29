using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    {
        Debug.Log("=== Creator Awake started ===");
        instance = this;

        // В билде префабы должны быть уже сериализованы в сцене
        // В редакторе они загружаются в OnValidate
#if !UNITY_EDITOR
        // В билде проверяем что префабы сериализованы
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogError("Prefabs not serialized in build! Make sure to save scene after OnValidate.");
            LoadPrefabs(); // Fallback
        }
        else
        {
            Debug.Log($"Prefabs serialized in build: {prefabs.Count} prefabs");

            // Восстанавливаем prefabLookup из сериализованных префабов
            prefabLookup.Clear();
            foreach (var prefab in prefabs)
            {
                if (prefab != null && !string.IsNullOrEmpty(prefab.name))
                {
                    prefabLookup[prefab.name] = prefab;
                }
            }
            Debug.Log($"PrefabLookup restored: {prefabLookup.Count} entries");
        }
#else
        // В редакторе префабы загружаются в OnValidate
        Debug.Log($"Editor mode: {prefabs.Count} prefabs, {prefabLookup.Count} in lookup");
#endif

        PlacePrefabs();
        CreateCameraWitPivot();
        LoadUI();
        
        // Загружаем текущую машину при старте игры
        LoadCurrentMachineOnStart();
        
        Debug.Log("=== Creator Awake completed ===");
    }

    void Update()
    {
        if (Play.i.currentState != Play.State.Create) return;
        if(Input.mousePosition.x<200 || Input.mousePosition.y<200||Input.mousePosition.x>Screen.width-200 || Input.mousePosition.y>Screen.height-200)return;
        // Проверяем состояние Play - если в режиме симуляции, не выполняем создание
        if (_play != null && _play.currentState == Play.State.Create)
        {
            CreateCell();
            DeleteCell();
        }
    }

    [SerializeField] public List<io_base> prefabs = new List<io_base>();
    [SerializeField] public Dictionary<string, io_base> prefabLookup = new Dictionary<string, io_base>(); // Словарь для поиска префабов по имени
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
    private int lastYawSteps = 0; // Запоминаем последний Yaw у созданной клетки 

    // === УТИЛИТЫ ДЛЯ ПЕРЕИМЕНОВАНИЯ ===
    private static Vector3Int RoundToInt(Vector3 p) =>
        new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));

    private void RenameIoBase(io_base cell)
    {
        if (cell == null) return;

        // Берём фактическое мировое положение объекта
        Vector3Int p = RoundToInt(cell.transform.position);

        // Источник имени — prefab_name, иначе текущее имя
        string baseName = string.IsNullOrEmpty(cell.prefab_name) ? cell.name : cell.prefab_name;

        cell.gameObject.name = $"{baseName}_{p.x}_{p.y}_{p.z}";
    }
    // ===================================

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
                _current_prefab.yawSteps = lastYawSteps; // Применяем последний Yaw
                _current_prefab.target_world_rotation = Quaternion.identity;
                _current_prefab.transform.localScale = Vector3.one * 0.01f;
                _current_prefab.name = "Current Create";
                // Убеждаемся, что текущий префаб остается в иерархии Creator
                _current_prefab.transform.SetParent(transform);
                _current_prefab.Status = io_base.io_base_status.Creating;
                cells.Add(_current_prefab);

                // Логируем созданную клетку с её типом
                string cellType = _current_prefab.GetCellType();
                // Debug.Log($"Created current_prefab: {_current_prefab.name} of type: {cellType}");
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

                // Сохраняем текущий Yaw для следующей клетки
                lastYawSteps = current_prefab.yawSteps;

                // На всякий случай применим целевые позицию/поворот на сам Transform
                current_prefab.transform.position = current_prefab.target_world_position;
                current_prefab.transform.rotation = current_prefab.target_world_rotation;

                // Переводим в финальный статус и ПЕРЕИМЕНОВЫВАЕМ
                current_prefab.Status = io_base.io_base_status.Placing;
                RenameIoBase(current_prefab);

                // Сбрасываем превью
                current_prefab = null;
            }
        }

        // Обработка отпускания кнопки мыши
        if (Input.GetMouseButtonUp(0))
        {
            // Debug.Log("Mouse button up detected!");
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, layer_mask))
        {
            if (SnapGrid)
            {
                var cell_to_check = hit.collider.GetComponent<io_cell>();

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

                    // 1) выравнивание "вверх" по нормали
                    Vector3 up = hitNormal.normalized;
                    Quaternion alignUp = Quaternion.FromToRotation(Vector3.up, up);

                    // 2) дополнительный поворот по локальной Y (yaw) кратно 90°
                    Quaternion yawRot = Quaternion.Euler(0f, 90f * current_prefab.yawSteps, 0f);

                    // 3) финальный поворот
                    Quaternion finalRot = alignUp * yawRot;

                    // Округляем углы до кратных 90°
                    Vector3 eulerAngles = finalRot.eulerAngles;
                    float roundedX = Mathf.Round(eulerAngles.x / 90f) * 90f;
                    float roundedY = Mathf.Round(eulerAngles.y / 90f) * 90f;
                    float roundedZ = Mathf.Round(eulerAngles.z / 90f) * 90f;

                    current_prefab.target_world_rotation = Quaternion.Euler(roundedX, roundedY, roundedZ);

                    // check intersections 
                    foreach (var cell in current_prefab.target_cells)
                    {
                        // Применяем поворот к локальной позиции клетки
                        Vector3 rotatedLocalPosition = current_prefab.target_world_rotation * cell.target_local_position;
                        Vector3 worldCellPosition = current_prefab.target_world_position + rotatedLocalPosition;

                        foreach (var b in cells)
                        {
                            if (b != current_prefab && b != null)
                                foreach (var cell2 in b.target_cells)
                                {
                                    Vector3 rotatedLocalPosition2 = b.target_world_rotation * cell2.target_local_position;
                                    Vector3 worldCellPosition2 = b.target_world_position + rotatedLocalPosition2;
                                    if ((worldCellPosition - worldCellPosition2).sqrMagnitude < 0.1f)
                                    {
                                        if (Input.GetKeyDown(KeyCode.L))
                                        {
                                            Debug.Log(cell2.target_io_base.name);
                                            Debug.Log(cell2.name);
                                        }
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
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, LayerMask.GetMask("io_base")))
            {
                var targetCell = hit.collider.gameObject.GetComponent<io_cell>().target_io_base;
                // Разрешаем удаление любой клетки, кроме превью (Creating),
                // чтобы можно было удалить "невидимого блокера", если такой возник.
                if (targetCell != null && targetCell.Status != io_base.io_base_status.Creating)
                {
                    SaveStateForUndo();
                    Destroy(targetCell.gameObject);
                    Debug.Log($"Deleted cell: {targetCell.name}");
                }
            }
        }
    }

    public List<item_SO> items_list = new List<item_SO>();

    public void LoadPrefabs()
    {
        Debug.Log("=== LoadPrefabs started ===");

        // Очищаем старые данные
        prefabs.Clear();
        prefabLookup.Clear();

        // Загружаем все item_SO из папки Items_serialized включая подпапки
        items_list = Resources.LoadAll<item_SO>("Items_serialized").ToList();

        // Извлекаем prefab из каждого item_SO и добавляем в список
        foreach (var item in items_list)
        {
            // Debug.Log($"Processing item_SO: {item.name}");
            if (item.prefab != null)
            {
                string prefabName = item.prefab.name;
                item.prefab.prefab_name = prefabName;

                // Проверяем тип префаба и логируем
                string cellType = item.prefab.GetCellType();
                // Debug.Log($"  - Prefab type: {cellType}, name: {prefabName}");

                prefabs.Add(item.prefab);
                // Добавляем в словарь для быстрого поиска по имени
                if (!prefabLookup.ContainsKey(prefabName))
                {
                    prefabLookup[prefabName] = item.prefab;
                }
                else
                {
                    Debug.LogWarning($"  - Duplicate prefab name: {prefabName}");
                }
            }
            else
            {
                Debug.LogWarning($"  - Prefab is null for item: {item.name}");
            }
        }

        // Устанавливаем первый префаб как текущий
        if (prefabs.Count > 0)
        {
            current_prefab_to_chabge = prefabs[0];
        }

        Debug.Log($"LoadPrefabs completed. Loaded {prefabs.Count} prefabs, lookup contains {prefabLookup.Count} entries");

        // Проверяем что все префабы имеют правильные имена
        foreach (var prefab in prefabs)
        {
            if (prefab != null && string.IsNullOrEmpty(prefab.prefab_name))
            {
                prefab.prefab_name = prefab.name;
                Debug.Log($"Set prefab_name for {prefab.name}: {prefab.prefab_name}");
            }
        }
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

            // Переименуем начально созданный элемент по позиции (0,0,0), если нужно
            RenameIoBase(new_prefab);

            // Логируем созданную клетку с её типом
            string cellType = new_prefab.GetCellType();
            // Debug.Log($"Placed cell: {new_prefab.name} of type: {cellType}");

            return;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        // Загружаем префабы из новой системы и сериализуем их в сцене
        LoadPrefabs();

        // Также заполняем старый список для совместимости
        _prefabs.Clear();
        foreach (var prefab in prefabs)
        {
            if (!_prefabs.Contains(prefab))
            {
                _prefabs.Add(prefab);
            }
        }

        // Автоматически сохраняем сцену чтобы префабы сериализовались
        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

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
            SaveMachine("");
            return;
        }
        if (button.gameObject.name == "ButtonLoad")
        {
            
            return;
        }
        if (button.gameObject.name == "ButtonUnDo")
        {
            UndoMachine();
            return;
        }
        if (button.gameObject.name == "ButtonReDo")
        {
            RedoMachine();
            return;
        }
        if (button.gameObject.name == "ButtonPlay")
        {
            PlayMachine();
            return;
        }
        // Debug.Log("OnSubTypeSelected: " + button.name);
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
        Debug.Log($"ClearMachineInternal: clearing {cells.Count} cells");

        foreach (var cell in cells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
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

        Debug.Log("ClearMachineInternal: machine cleared successfully");
    }

    // Структура для сериализации машины
    [System.Serializable]
    public class MachineData
    {
        public List<global::io_base_serialized> cells = new List<global::io_base_serialized>();
        public Vector3 cameraPivotPosition;
        public Quaternion cameraPivotRotation;
        public string machine_name;
    }

    // Undo/Redo система
    private List<MachineData> undoStack = new List<MachineData>();
    private List<MachineData> redoStack = new List<MachineData>();
    private const int maxUndoSteps = 20;

    public void SaveMachine(string machine_name)
    {
        
        // Создаем автосейв с ротацией
        int currentCounter = PlayerPrefs.GetInt("auto_save_counter", 0);
        currentCounter = (currentCounter % 5) + 1; // Ротация от 1 до 5
 
        string saveKey ="";
        Debug.Log("=== SaveMachine started ===");
        if (string.IsNullOrEmpty(machine_name))
        {
           saveKey= "auto_save_" + currentCounter;
        }
        else
        {
            saveKey = "machine_" + machine_name;
        }

        MachineData machineData = CreateMachineData(saveKey);
        Debug.Log($"MachineData created with {machineData?.cells?.Count ?? 0} cells");

        string json = JsonUtility.ToJson(machineData, true);
        Debug.Log($"JSON created, length: {json?.Length ?? 0}");

        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetInt("auto_save_counter", currentCounter);
        PlayerPrefs.Save();

        Debug.Log($"Machine saved to {saveKey}");
    }

    public void LoadMachine(string loadKey)
    {
        Debug.Log("=== LoadMachine started ===");

        // Ищем последний автосейв
        int lastCounter = PlayerPrefs.GetInt("auto_save_counter", 0);
        Debug.Log($"Last counter: {lastCounter}");

        if (lastCounter == 0)
        {
            Debug.Log("No auto saves found");
            return;
        }
        if (string.IsNullOrEmpty(loadKey))
        {
            loadKey = "auto_save_" + lastCounter;
        }
        Debug.Log($"Loading from key: {loadKey}");

        string json = PlayerPrefs.GetString(loadKey, "");
        Debug.Log($"JSON length: {json?.Length ?? 0}");

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
            Debug.Log($"MachineData parsed successfully. Cells count: {machineData?.cells?.Count ?? 0}");

            LoadMachineData(machineData);
            Debug.Log($"Machine loaded from {loadKey}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading machine: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Загружает текущую машину при старте игры из PlayerPrefs
    /// </summary>
    private void LoadCurrentMachineOnStart()
    {
        const string CURRENT_KEY_PREF = "current_machine_key";
        string currentKey = PlayerPrefs.GetString(CURRENT_KEY_PREF, "machine_0");
        
        Debug.Log($"[Creator] Загружаем текущую машину: {currentKey}");
        
        // Проверяем, что машина существует
        if (PlayerPrefs.HasKey(currentKey))
        {
            LoadMachine(currentKey);
            Debug.Log($"[Creator] Машина {currentKey} успешно загружена");
        }
        else
        {
            Debug.LogWarning($"[Creator] Машина {currentKey} не найдена, используем автосейв");
            // Пытаемся загрузить автосейв
            LoadMachine("machine_0");
        }
    }

    private void UndoMachine()
    {
        Debug.Log("=== UndoMachine started ===");

        if (undoStack.Count == 0)
        {
            Debug.Log("Nothing to undo");
            return;
        }

        // Сохраняем текущее состояние в redo стек
        MachineData currentState = CreateMachineData("undo");
        redoStack.Add(currentState);

        // Ограничиваем размер redo стека
        if (redoStack.Count > maxUndoSteps)
        {
            redoStack.RemoveAt(0);
        }

        // Загружаем предыдущее состояние
        MachineData previousState = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);

        LoadMachineDataForUndoRedo(previousState);
        Debug.Log("Undo performed");
    }

    private void RedoMachine()
    {
        Debug.Log("=== RedoMachine started ===");

        if (redoStack.Count == 0)
        {
            Debug.Log("Nothing to redo");
            return;
        }

        // Сохраняем текущее состояние в undo стек
        MachineData currentState = CreateMachineData("redo");
        undoStack.Add(currentState);

        // Ограничиваем размер undo стека
        if (undoStack.Count > maxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }

        // Загружаем следующее состояние
        MachineData nextState = redoStack[redoStack.Count - 1];
        redoStack.RemoveAt(redoStack.Count - 1);

        LoadMachineDataForUndoRedo(nextState);
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

    public MachineData CreateMachineData(string Machine_name)
    {
        // Debug.Log($"Total cells in scene: {cells.Count}");

        MachineData data = new MachineData();

        // Сохраняем позицию и поворот камеры
        if (_cam != null)
        {
            data.cameraPivotPosition = _cam.target_pivot_position;
            data.cameraPivotRotation = _cam.target_pivot_rotation;

        }
        data.machine_name = Machine_name;
        // Сохраняем все клетки кроме текущей создаваемой
        foreach (var cell in cells)
        {
            if (cell != null && cell.Status != io_base.io_base_status.Creating && cell.Status != io_base.io_base_status.Intersected)
            {
                // Используем полиморфную сериализацию
                io_base_serialized cellData = CreateSerializedData(cell);
                data.cells.Add(cellData);
            }
            else
            {
                // Debug.Log($"Skipping cell: {cell?.name} (null: {cell == null}, creating: {cell?.Status == io_base.io_base_status.Creating})");
            }
        }

        Debug.Log($"CreateMachineData completed. Saved {data.cells.Count} cells");
        return data;
    }

    private global::io_base_serialized CreateSerializedData(io_base cell)
    {
        // Создаем правильный тип данных на основе типа клетки
        global::io_base_serialized cellData;

        switch (cell.GetCellType())
        {
            case "io_engine":
                cellData = new global::io_engine_serialized();
                break;
            default:
                cellData = new global::io_base_serialized();
                break;
        }

        // Используем полиморфную сериализацию
        cell.SerializeToData(cellData);
        return cellData;
    }

    private void LoadMachineData(MachineData data)
    {
        Debug.Log("=== LoadMachineData started ===");
        Debug.Log($"Available prefabs count: {prefabs.Count}");
        Debug.Log($"PrefabLookup count: {prefabLookup.Count}");

        // Выводим все доступные префабы
        Debug.Log("Available prefab names:");
        foreach (var kvp in prefabLookup)
        {
            Debug.Log($"  - {kvp.Key}");
        }

        // Очищаем текущую машину без сохранения состояния для Undo
        ClearMachineInternal();

        // Восстанавливаем позицию камеры
        if (_cam != null)
        {
            _cam.target_pivot_position = data.cameraPivotPosition;
            _cam.target_pivot_rotation = data.cameraPivotRotation;
        }

        Debug.Log($"Cells to load: {data.cells?.Count ?? 0}");

        // Восстанавливаем клетки
        foreach (var cellData in data.cells)
        {
            Debug.Log($"Processing cell: {cellData.name}, prefabName: '{cellData._prefab_name}', cellType: '{cellData._cell_type}'");

            io_base prefab = null;

            // Ищем префаб по имени
            if (!string.IsNullOrEmpty(cellData._prefab_name) && prefabLookup.TryGetValue(cellData._prefab_name, out prefab))
            {
                Debug.Log($"Found prefab: {cellData._prefab_name}");
                var newCell = Instantiate(prefab, transform);

                // Используем полиморфную десериализацию
                newCell.DeserializeFromData(cellData);

                // Устанавливаем позицию и поворот
                newCell.transform.position = cellData._target_world_position;
                newCell.transform.rotation = cellData._target_world_rotation;

                // Жизненно важно: после загрузки делаем клетку видимой/валидной
                newCell.Status = io_base.io_base_status.Placing;

                // Переименовываем по позиции
                RenameIoBase(newCell);

                cells.Add(newCell);
                string cellType = newCell.GetCellType();
                Debug.Log($"Successfully created cell: {newCell.name} of type {cellType}");

                // Дополнительная информация для двигателей
                if (cellType == "io_engine")
                {
                    var engine = newCell as io_engine;
                    if (engine != null && engine.engineSettings != null)
                    {
                        Debug.Log($"  - Engine details: force_power={engine.engineSettings.force_power}, force_type={engine.engineSettings.force_type}, direction={engine.engineSettings.force_vector_local}");
                    }
                    else if (engine != null)
                    {
                        Debug.LogWarning($"  - Engine {engine.name} has no Engine_SO assigned!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Prefab not found: '{cellData._prefab_name}', skipping cell: {cellData.name}");
                Debug.LogWarning($"PrefabLookup contains keys: {string.Join(", ", prefabLookup.Keys)}");
            }
        }

        Debug.Log($"LoadMachineData completed. Total cells loaded: {cells.Count}");
    }

    // Специальный метод для Undo/Redo с более тщательной очисткой
    private void LoadMachineDataForUndoRedo(MachineData data)
    {
        Debug.Log("=== LoadMachineDataForUndoRedo started ===");

        // Принудительно очищаем все клетки
        Debug.Log($"Force clearing {cells.Count} cells for Undo/Redo");

        // Создаем копию списка чтобы избежать проблем с итерацией
        var cellsToDestroy = new List<io_base>(cells);
        cells.Clear(); // Очищаем список сразу

        // Уничтожаем все клетки
        foreach (var cell in cellsToDestroy)
        {
            if (cell != null && cell.gameObject != null)
            {
                Debug.Log($"Destroying cell: {cell.name}");
#if UNITY_EDITOR
                DestroyImmediate(cell.gameObject);
#else
                Destroy(cell.gameObject);
#endif
            }
        }

        // Очищаем текущий префаб
        current_prefab = null;

        // Восстанавливаем позицию камеры
        if (_cam != null)
        {
            _cam.target_pivot_position = data.cameraPivotPosition;
            _cam.target_pivot_rotation = data.cameraPivotRotation;
        }

        Debug.Log($"Cells to load: {data.cells?.Count ?? 0}");

        // Восстанавливаем клетки
        foreach (var cellData in data.cells)
        {
            Debug.Log($"Processing cell: {cellData.name}, prefabName: '{cellData._prefab_name}', cellType: '{cellData._cell_type}'");

            io_base prefab = null;

            // Ищем префаб по имени
            if (!string.IsNullOrEmpty(cellData._prefab_name) && prefabLookup.TryGetValue(cellData._prefab_name, out prefab))
            {
                Debug.Log($"Found prefab: {cellData._prefab_name}");
                var newCell = Instantiate(prefab, transform);

                // Используем полиморфную десериализацию
                newCell.DeserializeFromData(cellData);

                // Устанавливаем позицию и поворот
                newCell.transform.position = cellData._target_world_position;
                newCell.transform.rotation = cellData._target_world_rotation;

                // Жизненно важно: после загрузки делаем клетку видимой/валидной
                newCell.Status = io_base.io_base_status.Placing;

                // Переименовываем по позиции
                RenameIoBase(newCell);

                cells.Add(newCell);
                string cellType = newCell.GetCellType();
                Debug.Log($"Successfully created cell: {newCell.name} of type {cellType}");

                // Дополнительная информация для двигателей
                if (cellType == "io_engine")
                {
                    var engine = newCell as io_engine;
                    if (engine != null && engine.engineSettings != null)
                    {
                        Debug.Log($"  - Engine details: force_power={engine.engineSettings.force_power}, force_type={engine.engineSettings.force_type}, direction={engine.engineSettings.force_vector_local}");
                    }
                    else if (engine != null)
                    {
                        Debug.LogWarning($"  - Engine {engine.name} has no Engine_SO assigned!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Prefab not found: '{cellData._prefab_name}', skipping cell: {cellData.name}");
                Debug.LogWarning($"PrefabLookup contains keys: {string.Join(", ", prefabLookup.Keys)}");
            }
        }

        Debug.Log($"LoadMachineDataForUndoRedo completed. Total cells loaded: {cells.Count}");
    }

    // Сохраняем состояние для Undo при важных действиях
    private void SaveStateForUndo()
    {
        MachineData currentState = CreateMachineData("undo");
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
