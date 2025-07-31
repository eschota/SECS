using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class dev_tools : MonoBehaviour
{
    private bool showDevTools = false;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    
    void Start()
    {
        // Загружаем состояние dev_tools из PlayerPrefs
        showDevTools = PlayerPrefs.GetInt("DevToolsEnabled", 0) == 1;
    }

    void Update()
    {
        // Переключение dev_tools по F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDevTools = !showDevTools;
            PlayerPrefs.SetInt("DevToolsEnabled", showDevTools ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    void CreateGUIStyles()
    {
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 56;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;
        
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 64;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.yellow;
    }
    
    void OnGUI()
    {
        if (!showDevTools) return;
        
        // Создаем стили для GUI (только когда нужно)
        if (labelStyle == null || headerStyle == null)
        {
            CreateGUIStyles();
        }
        
        // Статистика клеток по типам
        DisplayCellStatistics();
        
        // Информация о выбранном объекте
        DisplaySelectedObjectInfo();
        
        // Информация о объекте под мышкой
        DisplayMouseOverObjectInfo();
    }
    
    void DisplayCellStatistics()
    {
        if (io_system.instance == null || io_system.instance.io_list == null) return;
        
        var cells = io_system.instance.io_list;
        
        // Дополнительная проверка на null для каждого элемента
        int cellCount = cells.Count(io => io != null && io.cell_type == io_base.io_base_cell_type.cell);
        int stairCount = cells.Count(io => io != null && io.cell_type == io_base.io_base_cell_type.stair);
        int spaceCount = cells.Count(io => io != null && io.cell_type == io_base.io_base_cell_type.space);
        int totalCount = cells.Count(io => io != null);
        
        string stats = $"Всего: {totalCount} (Пол: {cellCount}, Лестница: {stairCount}, Космос: {spaceCount})";
        
        GUI.Label(new Rect(10, 10, 1200, 100), "СТАТИСТИКА КЛЕТОК:", headerStyle);
        GUI.Label(new Rect(10, 120, 1200, 100), stats, labelStyle);
    }
    
    void DisplaySelectedObjectInfo()
    {
        if (UnityEditor.Selection.activeGameObject == null) return;
        
        var io = UnityEditor.Selection.activeGameObject.GetComponent<io_base>();
        if (io == null) return;
        
        GUI.Label(new Rect(10, 240, 1200, 100), "ВЫБРАННЫЙ ОБЪЕКТ:", headerStyle);
        
        string cellType = GetCellTypeName(io.cell_type);
        GUI.Label(new Rect(10, 350, 1200, 100), $"Тип: {cellType}", labelStyle);
        
        // Последние 3 состояния из стека с проверкой на null
        string states = "Нет состояний";
        if (io.stack != null && io.stack.Count > 0)
        {
            var lastStates = io.stack.TakeLast(3).ToArray();
            states = string.Join(" → ", lastStates.Select(s => s.ToString()));
        }
        GUI.Label(new Rect(10, 460, 1200, 100), $"Состояния: {states}", labelStyle);
        
        GUI.Label(new Rect(10, 570, 1200, 100), $"Этаж: {io.floor}", labelStyle);
    }
    
    void DisplayMouseOverObjectInfo()
    {
        // Получаем объект под мышкой через raycast
        if (Camera.main == null) return;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (!Physics.Raycast(ray, out hit)) return;
        
        var io = hit.collider.GetComponent<io_base>();
        if (io == null) return;
        
        GUI.Label(new Rect(10, 690, 1200, 100), "ОБЪЕКТ ПОД МЫШКОЙ:", headerStyle);
        
        string cellType = GetCellTypeName(io.cell_type);
        GUI.Label(new Rect(10, 800, 1200, 100), $"Тип: {cellType}", labelStyle);
        
        // Последние 3 состояния из стека с проверкой на null
        string states = "Нет состояний";
        if (io.stack != null && io.stack.Count > 0)
        {
            var lastStates = io.stack.TakeLast(3).ToArray();
            states = string.Join(" → ", lastStates.Select(s => s.ToString()));
        }
        GUI.Label(new Rect(10, 910, 1200, 100), $"Состояния: {states}", labelStyle);
        
        GUI.Label(new Rect(10, 1020, 1200, 100), $"Этаж: {io.floor}", labelStyle);
    }
    
    string GetCellTypeName(io_base.io_base_cell_type cellType)
    {
        switch (cellType)
        {
            case io_base.io_base_cell_type.cell:
                return "Пол";
            case io_base.io_base_cell_type.stair:
                return "Лестница";
            case io_base.io_base_cell_type.space:
                return "Космос";
            default:
                return "Неизвестно";
        }
    }
}
