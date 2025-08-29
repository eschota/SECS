using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UI_SaveLoadSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button saveButton;            // можно привязать SaveCurrentMachine()
    [SerializeField] Button loadButton;            // не используется — загрузка по выбору из списка
    [SerializeField] Button deleteButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button newMechineButton;      // создаёт новую пустую машину (в слотах 1..N)
    [SerializeField] TMP_InputField MachineNameInput;
    [SerializeField] Image MachinePreview;         // не используется здесь
    [SerializeField] TextMeshProUGUI[] MachineDetailsTexts; // не используется здесь
    [SerializeField] ScrollRect ScrollView;

    [Header("Template & List")]
    [Tooltip("Элемент в нулевом индексе — шаблон (disabled). Он будет клонироваться для каждой сохранённой машины.")]
    [SerializeField] List<TMP_InputField> MachinesLoadedInputFields;

    private readonly List<Creator.MachineData> loadedMachines = new List<Creator.MachineData>();

    private class MachineEntry
    {
        public string key;                   // "machine_#"
        public Creator.MachineData data;     // десериализованные данные
        public TMP_InputField uiField;       // строка UI
    }
    private readonly List<MachineEntry> machineEntries = new List<MachineEntry>();

    private const int MAX_SCAN = 512;                 // верхняя граница сканирования ключей
    private const string CURRENT_KEY_PREF = "current_machine_key";
    private int selectedIndex = -1;                   // последний выбранный элемент списка

    #region Unity lifecycle

    void Start()
    {
        Play.OnPlayStateChange += OnPlayStateChange;

        // Шаблон: выключаем объект, оставляем редактируемым для клонов
        if (MachinesLoadedInputFields != null && MachinesLoadedInputFields.Count > 0 && MachinesLoadedInputFields[0] != null)
        {
            MachinesLoadedInputFields[0].gameObject.SetActive(false);
            MachinesLoadedInputFields[0].readOnly = false;
            MachinesLoadedInputFields[0].interactable = true;
        }

        if (newMechineButton)
            newMechineButton.onClick.AddListener(CreateNewMachine);

        if (deleteButton) deleteButton.onClick.AddListener(() =>
        {
            // Удаляем выбранную машину
            if (selectedIndex >= 0 && selectedIndex < machineEntries.Count)
            {
                var entry = machineEntries[selectedIndex];
                DeleteMachine(ExtractIndexName(entry.key));
            }
            else
            {
                Debug.LogWarning("[UI_SaveLoadSystem] Нет выбранной машины для удаления");
            }
        });

        if (saveButton) saveButton.onClick.AddListener(SaveCurrentMachine);

        // loadButton здесь не нужен
        if (loadButton) loadButton.onClick.RemoveAllListeners();
    }

    void OnDestroy()
    {
        Play.OnPlayStateChange -= OnPlayStateChange;
    }

    #endregion

    #region State handler

    void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.SaveLoad)
        {
            // 1) Автосейв текущей машины в НУЛЕВОЙ слот
            AutoSaveCurrentToSlot0();

            // 2) Обновляем список
            LoadMachines();

            // 3) Выбрать машину по умолчанию:
            //    - если ранее игрок выбирал слот — его
            //    - иначе — machine_0 (автосейв)
            SelectDefaultOnOpen();
        }
    }

    #endregion

    #region Public API (для кнопок)

    /// <summary>
    /// Сохраняет текущую машину в ВЫБРАННЫЙ слот списка.
    /// Если ничего не выбрано — сохраняет в machine_0 (автосейв).
    /// Имя берётся из редактируемого поля выбранной строки (если пусто — подставляется автоматически).
    /// Также обновляет "current_machine_key", чтобы при следующем запуске грузилась последняя сохранённая пользователем машина.
    /// </summary>
    public void SaveCurrentMachine()
    {
        if (Creator.instance == null)
        {
            Debug.LogError("[UI_SaveLoadSystem] SaveCurrentMachine(): Creator.instance == null");
            return;
        }

        // Определяем целевой ключ
        string targetKey;
        string displayName;

        if (selectedIndex >= 0 && selectedIndex < machineEntries.Count)
        {
            var entry = machineEntries[selectedIndex];
            targetKey = entry.key;

            // Имя из редактируемого поля или данных/индекса
            displayName = (entry.uiField != null && !string.IsNullOrEmpty(entry.uiField.text))
                ? entry.uiField.text
                : (!string.IsNullOrEmpty(entry.data?.machine_name) ? entry.data.machine_name : ExtractIndexName(entry.key));
        }
        else
        {
            // Фолбек — автосейв в machine_0
            targetKey = "machine_0";
            displayName = "AutoSave";
        }

        // Собираем данные из конструктора
        var data = Creator.instance.CreateMachineData(displayName);
        string json = JsonUtility.ToJson(data, true);

        // Перезаписываем слот
        if (PlayerPrefs.HasKey(targetKey))
            PlayerPrefs.DeleteKey(targetKey);
        PlayerPrefs.SetString(targetKey, json);

        // Сохраняем "текущую" машину для автозагрузки после рестарта
        PlayerPrefs.SetString(CURRENT_KEY_PREF, targetKey);
        PlayerPrefs.Save();

        // Обновим список + восстановим выбор
        string savedKey = targetKey;
        LoadMachines();
        int reselect = machineEntries.FindIndex(e => e.key == savedKey);
        if (reselect >= 0)
            OnMachineItemSelected(reselect);

        Debug.Log($"[UI_SaveLoadSystem] SaveCurrentMachine(): saved to '{targetKey}' as '{displayName}' and set as current.");
    }

    /// <summary>
    /// Создаёт новую пустую машину (с одной базовой клеткой из первого префаба) в первом свободном слоте 1..N.
    /// Нулевой индекс зарезервирован под автосейв.
    /// </summary>
    public void CreateNewMachine()
    {
        int index = FindFirstEmptyIndex(1);
        string key = $"machine_{index}";

        var newData = new Creator.MachineData
        {
            machine_name = index.ToString(),
            cameraPivotPosition = Vector3.zero,
            cameraPivotRotation = Quaternion.identity,
            cells = new List<io_base_serialized>()
        };

        // Базовая клетка — первый префаб из Creator
        var basePrefab = (Creator.instance != null && Creator.instance.prefabs != null && Creator.instance.prefabs.Count > 0)
            ? Creator.instance.prefabs[0]
            : null;

        if (basePrefab != null)
        {
            var baseName = string.IsNullOrEmpty(basePrefab.prefab_name) ? basePrefab.name : basePrefab.prefab_name;

            var baseCell = new io_base_serialized
            {
                _prefab_name = baseName,
                _target_world_position = new Vector3(0, 12, 0),
                _target_world_rotation = Quaternion.identity,
                _yaw_steps = 0,
                _status = 0,
                name = $"{baseName}_0_12_0",
                _cell_type = basePrefab.GetCellType()
            };
            newData.cells.Add(baseCell);
        }
        else
        {
            Debug.LogWarning("[UI_SaveLoadSystem] В Creator.prefabs нет элементов — новая машина создастся без базовой клетки.");
        }

        string json = JsonUtility.ToJson(newData, true);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();

        LoadMachines();

        // Сразу выбрать созданную и пометить как current
        int createdIdx = machineEntries.FindIndex(e => e.key == key);
        if (createdIdx >= 0)
        {
            OnMachineItemSelected(createdIdx);
            PlayerPrefs.SetString(CURRENT_KEY_PREF, key);
            PlayerPrefs.Save();
        }

        if (MachineNameInput) MachineNameInput.text = index.ToString();
    }

    /// <summary>
    /// Создаёт копию выбранной машины в свободном слоте с суффиксом _copy.
    /// Делает копию текущей машиной.
    /// </summary>
    public void DuplicateMachine()
    {
        if (selectedIndex < 0 || selectedIndex >= machineEntries.Count)
        {
            Debug.LogWarning("[UI_SaveLoadSystem] Нет выбранной машины для дублирования");
            return;
        }

        var sourceEntry = machineEntries[selectedIndex];
        var sourceData = sourceEntry.data;

        // Находим свободный слот
        int newIndex = FindFirstEmptyIndex(1);
        string newKey = $"machine_{newIndex}";

        // Создаём копию данных
        var duplicatedData = new Creator.MachineData
        {
            machine_name = $"{sourceData.machine_name}_copy",
            cameraPivotPosition = sourceData.cameraPivotPosition,
            cameraPivotRotation = sourceData.cameraPivotRotation,
            cells = new List<io_base_serialized>()
        };

        // Копируем все клетки
        if (sourceData.cells != null)
        {
            foreach (var cell in sourceData.cells)
            {
                var newCell = new io_base_serialized
                {
                    _prefab_name = cell._prefab_name,
                    _target_world_position = cell._target_world_position,
                    _target_world_rotation = cell._target_world_rotation,
                    _yaw_steps = cell._yaw_steps,
                    _status = cell._status,
                    name = cell.name,
                    _cell_type = cell._cell_type
                };
                duplicatedData.cells.Add(newCell);
            }
        }

        // Сохраняем копию
        string json = JsonUtility.ToJson(duplicatedData, true);
        PlayerPrefs.SetString(newKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[UI_SaveLoadSystem] Создана копия машины '{sourceData.machine_name}' как '{duplicatedData.machine_name}' в слоте {newIndex}");

        // Обновляем список и выбираем новую машину
        LoadMachines();
        
        int newEntryIndex = machineEntries.FindIndex(e => e.key == newKey);
        if (newEntryIndex >= 0)
        {
            OnMachineItemSelected(newEntryIndex);
            PlayerPrefs.SetString(CURRENT_KEY_PREF, newKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Удаляет машину по имени (ожидается индекс: "0", "1", ...).
    /// Если удаляем текущую — сбрасываем current на machine_0 при наличии.
    /// </summary>
    public void DeleteMachine(string machineName)
    {
        if (string.IsNullOrEmpty(machineName)) return;

        string key = $"machine_{machineName}";
        bool wasCurrent = PlayerPrefs.GetString(CURRENT_KEY_PREF, "machine_0") == key;
        bool wasSelected = (selectedIndex >= 0 && selectedIndex < machineEntries.Count && machineEntries[selectedIndex].key == key);

        Debug.Log($"[UI_SaveLoadSystem] Удаляем машину '{machineName}' (ключ: {key})");

        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[UI_SaveLoadSystem] Машина {key} удалена из PlayerPrefs");
        }
        else
        {
            Debug.LogWarning($"[UI_SaveLoadSystem] Машина {key} не найдена в PlayerPrefs");
        }

        LoadMachines();

        if (wasCurrent || wasSelected)
        {
            // Переключаемся на автосейв, если был удалён текущий или выбранный
            if (machineEntries.Exists(e => e.key == "machine_0"))
            {
                PlayerPrefs.SetString(CURRENT_KEY_PREF, "machine_0");
                PlayerPrefs.Save();
                int idx = machineEntries.FindIndex(e => e.key == "machine_0");
                if (idx >= 0) OnMachineItemSelected(idx);
            }
            else if (machineEntries.Count > 0)
            {
                // Если нет автосейва, выбираем первую доступную
                OnMachineItemSelected(0);
            }
        }
    }

    #endregion

    #region Core

    /// <summary>
    /// Автосохраняет текущее состояние Creator в ключ machine_0.
    /// </summary>
    private void AutoSaveCurrentToSlot0()
    {
        if (Creator.instance == null) return;

        var data = Creator.instance.CreateMachineData("AutoSave");
        data.machine_name = "AutoSave";

        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString("machine_0", json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Сканирует PlayerPrefs на machine_0..machine_MAX_SCAN, десериализует и строит список.
    /// Выбор строки — моментальная загрузка через Creator.LoadMachine(key).
    /// </summary>
    public void LoadMachines()
    {
        if (MachinesLoadedInputFields == null || MachinesLoadedInputFields.Count == 0 || MachinesLoadedInputFields[0] == null)
        {
            Debug.LogError("[UI_SaveLoadSystem] Не задан шаблон InputField в MachinesLoadedInputFields[0].");
            return;
        }

        // Удаляем все ранее созданные строки (кроме шаблона [0])
        for (int i = MachinesLoadedInputFields.Count - 1; i >= 1; i--)
        {
            if (MachinesLoadedInputFields[i] != null)
                Destroy(MachinesLoadedInputFields[i].gameObject);
            MachinesLoadedInputFields.RemoveAt(i);
        }
        machineEntries.Clear();
        loadedMachines.Clear();
        selectedIndex = -1;

        // Сначала — нулевой индекс (автосейв)
        TryAddMachineRow(0);

        // Затем — остальные
        for (int i = 1; i <= MAX_SCAN; i++)
            TryAddMachineRow(i);

        // Прокрутка к началу
        if (ScrollView) ScrollView.normalizedPosition = new Vector2(0, 1);
    }

    private void TryAddMachineRow(int i)
    {
        string key = $"machine_{i}";
        if (!PlayerPrefs.HasKey(key))
            return;

        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json))
            return;

        Creator.MachineData data = null;
        try
        {
            data = JsonUtility.FromJson<Creator.MachineData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UI_SaveLoadSystem] Ошибка парсинга {key}: {e.Message}");
            return;
        }
        if (data == null) return;

        // Имя для отображения
        if (string.IsNullOrEmpty(data.machine_name))
            data.machine_name = (i == 0) ? "AutoSave" : i.ToString();

        loadedMachines.Add(data);

        // Создаём UI-строку из шаблона
        var template = MachinesLoadedInputFields[0];
        var field = Instantiate(template, template.transform.parent);
        field.gameObject.SetActive(true);

        // Оставляем редактируемым
        field.readOnly = false;
        field.interactable = true;

        field.text = data.machine_name;

        // Обработчик выбора
        var handler = field.gameObject.GetComponent<RowSelectHandler>();
        if (handler == null) handler = field.gameObject.AddComponent<RowSelectHandler>();

        int entryIndex = machineEntries.Count;
        handler.Init(this, entryIndex);

        // При окончании редактирования — просто обновим локальное имя (сохранение — отдельно)
        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(newName =>
        {
            if (entryIndex >= 0 && entryIndex < machineEntries.Count)
            {
                machineEntries[entryIndex].data.machine_name = newName;
            }
        });

        MachinesLoadedInputFields.Add(field);
        machineEntries.Add(new MachineEntry
        {
            key = key,
            data = data,
            uiField = field
        });
    }

    // Выбор строки -> загрузка машины + запомнить текущую
    private void OnMachineItemSelected(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= machineEntries.Count) return;

        // Сбрасываем цвет всех записей на белый
        foreach (var machineEntry in machineEntries)
        {
            if (machineEntry.uiField != null)
            {
                var colors = machineEntry.uiField.colors;
                colors.normalColor = Color.white;
                machineEntry.uiField.colors = colors;
            }
        }

        selectedIndex = entryIndex;

        var entry = machineEntries[entryIndex];

        // Устанавливаем зеленый цвет для выбранной записи
        if (entry.uiField != null)
        {
            var colors = entry.uiField.colors;
            colors.normalColor = Color.green;
            entry.uiField.colors = colors;
        }

        // Обновим вспомогательное поле
        if (MachineNameInput) MachineNameInput.text = entry.data.machine_name;

        // Грузим
        if (Creator.instance != null)
            Creator.instance.LoadMachine(entry.key); // "machine_#"

        // Запоминаем как текущую для перезапуска игры
        PlayerPrefs.SetString(CURRENT_KEY_PREF, entry.key);
        PlayerPrefs.Save();
    }

    // Выбор по ключу (если есть)
    private void SelectByKey(string key)
    {
        int idx = machineEntries.FindIndex(e => e.key == key);
        if (idx >= 0)
        {
            OnMachineItemSelected(idx);
        }
        else if (machineEntries.Count > 0)
        {
            // Фолбек — первый элемент списка
            OnMachineItemSelected(0);
        }
    }

    // Выбор по умолчанию при открытии меню
    private void SelectDefaultOnOpen()
    {
        string lastKey = PlayerPrefs.GetString(CURRENT_KEY_PREF, "machine_0");
        if (string.IsNullOrEmpty(lastKey))
            lastKey = "machine_0";

        if (machineEntries.Count == 0)
            return;

        // Если сохранённый ключ существует в списке — выбираем его, иначе — machine_0, иначе — первый
        int idx = machineEntries.FindIndex(e => e.key == lastKey);
        if (idx >= 0)
        {
            OnMachineItemSelected(idx);
            return;
        }

        idx = machineEntries.FindIndex(e => e.key == "machine_0");
        if (idx >= 0)
        {
            OnMachineItemSelected(idx);
            return;
        }

        OnMachineItemSelected(0);
    }

    // Ищем первый свободный индекс начиная с startFrom (0 зарезервирован для автосейва)
    private int FindFirstEmptyIndex(int startFrom)
    {
        int idx = Mathf.Max(1, startFrom);
        for (int i = idx; i <= MAX_SCAN; i++)
        {
            string key = $"machine_{i}";
            if (!PlayerPrefs.HasKey(key))
                return i;
        }
        return MAX_SCAN + 1;
    }

    private static string ExtractIndexName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "0";
        if (!key.StartsWith("machine_")) return "0";
        var idx = key.Substring("machine_".Length);
        return string.IsNullOrEmpty(idx) ? "0" : idx;
    }

    #endregion

    #region Helper

    // Хэндлер клика/селекта на строке (вложенный компонент, чтобы всё было в одном файле)
    class RowSelectHandler : MonoBehaviour, IPointerClickHandler, ISelectHandler
    {
        private UI_SaveLoadSystem owner;
        private int index;

        public void Init(UI_SaveLoadSystem owner, int index)
        {
            this.owner = owner;
            this.index = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.OnMachineItemSelected(index);
        }

        public void OnSelect(BaseEventData eventData)
        {
            owner?.OnMachineItemSelected(index);
        }
    }

    #endregion
}
