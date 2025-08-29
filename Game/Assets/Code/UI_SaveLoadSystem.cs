using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class UI_SaveLoadSystem : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button saveButton;
    [SerializeField] Button loadButton;
    [SerializeField] Button deleteButton;
    [SerializeField] Button exitButton; 
    [SerializeField] TMP_InputField MachineNameInput;
    [SerializeField] Image MachinePreview;
    [SerializeField] TextMeshProUGUI [] MachineDetailsTexts;
    [SerializeField] ScrollRect ScrollView;
    [SerializeField] List<TextMeshProUGUI> MachineItemNameText;

    // Приватные поля для работы с системой
    private List<MachineSaveData> availableMachines = new List<MachineSaveData>();
    private MachineSaveData selectedMachine;
    private TextMeshProUGUI templateItem;
    private Transform scrollContent;
    private List<GameObject> createdItems = new List<GameObject>();

    [System.Serializable]
    public class MachineSaveData
    {
        public string saveKey;
        public string machineName;
        public string jsonData;
        public System.DateTime saveTime;
    }

    void Start()
    {
        // Находим шаблон и контент скролла
        if (MachineItemNameText.Count > 0)
        {
            templateItem = MachineItemNameText[0];
            scrollContent = ScrollView.content;
            
            // Скрываем шаблон
            templateItem.gameObject.SetActive(false);
        }

        // Подписываемся на события кнопок
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadButtonClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
        if (MachineNameInput != null) MachineNameInput.onEndEdit.AddListener(OnMachineNameChanged);

        // Скрываем меню по умолчанию
        HideMenu();
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (saveButton != null) saveButton.onClick.RemoveListener(OnSaveButtonClicked);
        if (loadButton != null) loadButton.onClick.RemoveListener(OnLoadButtonClicked);
        if (deleteButton != null) deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitButtonClicked);
        if (MachineNameInput != null) MachineNameInput.onEndEdit.RemoveListener(OnMachineNameChanged);
    }

    // Публичные методы для внешнего вызова
    public void ShowMenu()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        RefreshMachineList();
    }

    public void HideMenu()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Обработчики кнопок
    private void OnSaveButtonClicked()
    {
        if (selectedMachine != null)
        {
            // Перезаписываем существующее сохранение
            SaveMachine(selectedMachine.machineName, selectedMachine.saveKey);
        }
        else
        {
            string machineName = MachineNameInput.text;
            if (string.IsNullOrEmpty(machineName))
            {
                machineName = "New Machine " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }
            SaveMachine(machineName);
        }
    }

    private void OnLoadButtonClicked()
    {
        if (selectedMachine != null)
        {
            LoadMachine(selectedMachine);
            HideMenu();
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (selectedMachine != null)
        {
            DeleteMachine(selectedMachine);
        }
    }

    private void OnExitButtonClicked()
    {
        HideMenu();
    }

    private void OnMachineNameChanged(string newName)
    {
        if (selectedMachine != null && !string.IsNullOrEmpty(newName))
        {
            selectedMachine.machineName = newName;
            // Сохраняем новое имя в PlayerPrefs
            PlayerPrefs.SetString(selectedMachine.saveKey + "_name", newName);
            PlayerPrefs.Save();
            // Обновляем отображение в списке
            RefreshMachineList();
        }
    }

    // Основные методы работы с машинами
    private void SaveMachine(string machineName, string existingKey = null)
    {
        if (Creator.instance == null) return;

        string saveKey;
        
        if (!string.IsNullOrEmpty(existingKey))
        {
            // Перезаписываем существующее сохранение
            saveKey = existingKey;
        }
        else
        {
            // Создаем новое сохранение
            int saveCount = PlayerPrefs.GetInt("machine_save_count", 0);
            saveCount++;
            saveKey = "machine_save_" + saveCount;
            PlayerPrefs.SetInt("machine_save_count", saveCount);
        }
        
        // Получаем данные машины от Creator
        var machineData = Creator.instance.CreateMachineData(machineName);
        string json = JsonUtility.ToJson(machineData, true);
        
        // Сохраняем в PlayerPrefs
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetString(saveKey + "_name", machineName);
        PlayerPrefs.SetString(saveKey + "_time", System.DateTime.Now.ToString("O"));
        PlayerPrefs.Save();

        Debug.Log($"Machine saved: {machineName} with key: {saveKey}");
        
        // Обновляем список
        RefreshMachineList();
    }

    private void LoadMachine(MachineSaveData machineData)
    {
        if (Creator.instance == null) return;

        try
        {
            var machineDataObj = JsonUtility.FromJson<Creator.MachineData>(machineData.jsonData);
            Creator.instance.CreateMachineFromData(machineDataObj);
            Debug.Log($"Machine creation started: {machineData.machineName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading machine: {e.Message}");
        }
    }

    private void DeleteMachine(MachineSaveData machineData)
    {
        // Удаляем из PlayerPrefs
        PlayerPrefs.DeleteKey(machineData.saveKey);
        PlayerPrefs.DeleteKey(machineData.saveKey + "_name");
        PlayerPrefs.DeleteKey(machineData.saveKey + "_time");
        
        // Обновляем счетчик если нужно
        if (machineData.saveKey.StartsWith("machine_save_"))
        {
            string numberStr = machineData.saveKey.Replace("machine_save_", "");
            if (int.TryParse(numberStr, out int saveNumber))
            {
                int currentCount = PlayerPrefs.GetInt("machine_save_count", 0);
                if (saveNumber == currentCount)
                {
                    // Если удаляем последнее сохранение, уменьшаем счетчик
                    PlayerPrefs.SetInt("machine_save_count", currentCount - 1);
                }
            }
        }
        
        PlayerPrefs.Save();

        Debug.Log($"Machine deleted: {machineData.machineName}");
        
        // Обновляем список
        RefreshMachineList();
    }

    // Методы для работы со списком машин
    private void RefreshMachineList()
    {
        LoadAvailableMachines();
        CreateMachineListItems();
        UpdateUI();
    }

    private void LoadAvailableMachines()
    {
        availableMachines.Clear();
        
        // Получаем все ключи PlayerPrefs
        var allKeys = new List<string>();
        
        // Ищем все ключи, которые начинаются с "machine_save_"
        int saveCount = PlayerPrefs.GetInt("machine_save_count", 0);
        for (int i = 1; i <= saveCount; i++) // Проверяем все сохранения от 1 до saveCount
        {
            string key = "machine_save_" + i;
            if (PlayerPrefs.HasKey(key))
            {
                allKeys.Add(key);
            }
        }

        // Также ищем автосейвы
        for (int i = 1; i <= 5; i++)
        {
            string key = "auto_save_" + i;
            if (PlayerPrefs.HasKey(key))
            {
                allKeys.Add(key);
            }
        }

        // Загружаем данные машин
        foreach (string key in allKeys)
        {
            string json = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(json))
            {
                string name = PlayerPrefs.GetString(key + "_name", "Unknown Machine");
                string timeStr = PlayerPrefs.GetString(key + "_time", System.DateTime.Now.ToString("O"));
                
                if (System.DateTime.TryParse(timeStr, out System.DateTime saveTime))
                {
                    availableMachines.Add(new MachineSaveData
                    {
                        saveKey = key,
                        machineName = name,
                        jsonData = json,
                        saveTime = saveTime
                    });
                }
            }
        }

        // Сортируем по времени сохранения (новые сверху)
        availableMachines = availableMachines.OrderByDescending(m => m.saveTime).ToList();
    }

    private void CreateMachineListItems()
    {
        // Удаляем старые элементы
        foreach (var item in createdItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        createdItems.Clear();

        // Создаем новые элементы
        for (int i = 0; i < availableMachines.Count; i++)
        {
            var machineData = availableMachines[i];
            
            // Клонируем шаблон
            GameObject newItem = Instantiate(templateItem.gameObject, scrollContent);
            TextMeshProUGUI itemText = newItem.GetComponent<TextMeshProUGUI>();
            
            if (itemText != null)
            {
                itemText.text = machineData.machineName;
                
                // Добавляем обработчик клика
                Button itemButton = newItem.GetComponent<Button>();
                if (itemButton == null)
                {
                    itemButton = newItem.AddComponent<Button>();
                }
                
                int index = i; // Замыкание
                itemButton.onClick.AddListener(() => OnMachineItemClicked(index));
            }
            
            newItem.SetActive(true);
            createdItems.Add(newItem);
        }
    }

    private void OnMachineItemClicked(int index)
    {
        if (index >= 0 && index < availableMachines.Count)
        {
            selectedMachine = availableMachines[index];
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Обновляем имя в инпут поле
        if (MachineNameInput != null && selectedMachine != null)
        {
            MachineNameInput.text = selectedMachine.machineName;
        }

        // Обновляем детали машины
        if (MachineDetailsTexts != null && selectedMachine != null)
        {
            UpdateMachineDetails();
        }

        // Обновляем состояние кнопок
        bool hasSelection = selectedMachine != null;
        if (loadButton != null) loadButton.interactable = hasSelection;
        if (deleteButton != null) deleteButton.interactable = hasSelection;
    }

    private void UpdateMachineDetails()
    {
        if (selectedMachine == null || MachineDetailsTexts == null) return;

        try
        {
            var machineData = JsonUtility.FromJson<Creator.MachineData>(selectedMachine.jsonData);
            
            if (MachineDetailsTexts.Length > 0)
                MachineDetailsTexts[0].text = $"Name: {selectedMachine.machineName}";
            
            if (MachineDetailsTexts.Length > 1)
                MachineDetailsTexts[1].text = $"Cells: {machineData.cells?.Count ?? 0}";
            
            if (MachineDetailsTexts.Length > 2)
                MachineDetailsTexts[2].text = $"Saved: {selectedMachine.saveTime.ToString("yyyy-MM-dd HH:mm")}";
            
            if (MachineDetailsTexts.Length > 3)
                MachineDetailsTexts[3].text = $"Key: {selectedMachine.saveKey}";
                
            // Обновляем превью если есть
            if (MachinePreview != null)
            {
                UpdateMachinePreview(machineData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating machine details: {e.Message}");
        }
    }
    
    private void UpdateMachinePreview(Creator.MachineData machineData)
    {
        // Пока просто устанавливаем цвет в зависимости от количества клеток
        if (MachinePreview != null)
        {
            int cellCount = machineData.cells?.Count ?? 0;
            Color previewColor = Color.gray;
            
            if (cellCount > 0)
            {
                // Градиент от красного к зеленому в зависимости от количества клеток
                float intensity = Mathf.Clamp01(cellCount / 50f); // Нормализуем к 50 клеткам
                previewColor = Color.Lerp(Color.red, Color.green, intensity);
            }
            
            MachinePreview.color = previewColor;
        }
    }
}
