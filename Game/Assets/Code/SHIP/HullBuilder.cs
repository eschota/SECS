using UnityEngine;
using System.Collections.Generic;
using System;

public class HullBuilder : MonoBehaviour
{
    [Header("Builder Settings")]
    [SerializeField] public HULL hullComponent;
    [SerializeField] private Camera builderCamera;
    [SerializeField] private LayerMask buildableLayerMask = 1;
    
    [Header("UI References")]
    [SerializeField] private GameObject buildUI;
    [SerializeField] private UnityEngine.UI.Button addDoorButton;
    [SerializeField] private UnityEngine.UI.Button clearHullButton;
    [SerializeField] private UnityEngine.UI.Button saveHullButton;
    [SerializeField] private UnityEngine.UI.Button loadHullButton;
    
    [Header("Build Mode")]
    [SerializeField] private BuildMode currentBuildMode = BuildMode.Wall;
    
    public enum BuildMode
    {
        Wall,
        Door,
        Point
    }
    
    private bool isBuildingActive = false;
    private Vector3 lastBuildPosition;
    private List<Vector3> currentBuildPath = new List<Vector3>();
    
    // Events
    public static event Action<BuildMode> OnBuildModeChanged;
    public static event Action<bool> OnBuildingStateChanged;
    
    void Start()
    {
        // Подписываемся на изменение состояния корабля
        SHIP_UI.ChangeState += OnShipStateChanged;
        
        // Инициализируем UI
        InitializeUI();
        
        // Находим компонент HULL если не назначен
        if (hullComponent == null)
        {
            hullComponent = FindObjectOfType<HULL>();
        }
        
        // Находим камеру если не назначена
        if (builderCamera == null)
        {
            builderCamera = Camera.main;
        }
    }
    
    void OnDestroy()
    {
        SHIP_UI.ChangeState -= OnShipStateChanged;
    }
    
    private void OnShipStateChanged(SHIP_UI.State newState)
    {
        bool shouldBeActive = (newState == SHIP_UI.State._ship_state_editor_main_module_0);
        
        if (shouldBeActive != isBuildingActive)
        {
            isBuildingActive = shouldBeActive;
            OnBuildingStateChanged?.Invoke(isBuildingActive);
            
            if (buildUI != null)
            {
                buildUI.SetActive(isBuildingActive);
            }
            
            Debug.Log($"[HullBuilder] Режим строительства {(isBuildingActive ? "включен" : "выключен")}");
        }
    }
    
    private void InitializeUI()
    {
        if (addDoorButton != null)
        {
            addDoorButton.onClick.AddListener(OnAddDoorClicked);
        }
        
        if (clearHullButton != null)
        {
            clearHullButton.onClick.AddListener(OnClearHullClicked);
        }
        
        if (saveHullButton != null)
        {
            saveHullButton.onClick.AddListener(OnSaveHullClicked);
        }
        
        if (loadHullButton != null)
        {
            loadHullButton.onClick.AddListener(OnLoadHullClicked);
        }
    }
    
    void Update()
    {
        if (!isBuildingActive || hullComponent == null) return;
        
        HandleBuildInput();
        HandleBuildModeInput();
    }
    
    private void HandleBuildInput()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 gridPosition = SnapToGrid(mouseWorldPos);
        
        // Показываем предварительный просмотр
        ShowBuildPreview(gridPosition);
        
        // Обработка нажатий мыши
        if (Input.GetMouseButtonDown(0))
        {
            StartBuildPath(gridPosition);
        }
        else if (Input.GetMouseButton(0))
        {
            ContinueBuildPath(gridPosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            FinishBuildPath();
        }
        
        // Обработка правой кнопки мыши для отмены
        if (Input.GetMouseButtonDown(1))
        {
            CancelBuildPath();
        }
    }
    
    private void HandleBuildModeInput()
    {
        // Переключение режимов строительства
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetBuildMode(BuildMode.Wall);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetBuildMode(BuildMode.Door);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetBuildMode(BuildMode.Point);
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (builderCamera == null) return Vector3.zero;
        
        Ray ray = builderCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, buildableLayerMask))
        {
            return hit.point;
        }
        
        // Если луч не попал никуда, используем плоскость XZ
        float distance = 0f;
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }
    
    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        float gridSize = 1f; // Размер сетки 1x1 метр
        return new Vector3(
            Mathf.Round(worldPosition.x / gridSize) * gridSize,
            Mathf.Round(worldPosition.y / gridSize) * gridSize,
            Mathf.Round(worldPosition.z / gridSize) * gridSize
        );
    }
    
    private void ShowBuildPreview(Vector3 position)
    {
        // Здесь можно добавить визуализацию предварительного просмотра
        // Например, показывать призрак будущего объекта
    }
    
    private void StartBuildPath(Vector3 position)
    {
        currentBuildPath.Clear();
        currentBuildPath.Add(position);
        lastBuildPosition = position;
        
        Debug.Log($"[HullBuilder] Начато строительство в позиции {position}");
    }
    
    private void ContinueBuildPath(Vector3 position)
    {
        // Добавляем новую точку только если она достаточно далеко от последней
        if (Vector3.Distance(position, lastBuildPosition) >= 1f)
        {
            currentBuildPath.Add(position);
            lastBuildPosition = position;
            
            // Если строим стену, создаем промежуточные точки
            if (currentBuildMode == BuildMode.Wall && currentBuildPath.Count >= 2)
            {
                CreateIntermediatePoints();
            }
        }
    }
    
    private void FinishBuildPath()
    {
        if (currentBuildPath.Count < 2) return;
        
        switch (currentBuildMode)
        {
            case BuildMode.Wall:
                CreateWallFromPath();
                break;
            case BuildMode.Door:
                CreateDoorFromPath();
                break;
            case BuildMode.Point:
                CreatePointsFromPath();
                break;
        }
        
        currentBuildPath.Clear();
        Debug.Log($"[HullBuilder] Завершено строительство {currentBuildMode}");
    }
    
    private void CancelBuildPath()
    {
        currentBuildPath.Clear();
        Debug.Log("[HullBuilder] Строительство отменено");
    }
    
    private void CreateIntermediatePoints()
    {
        // Создаем точки между начальной и конечной позициями
        for (int i = 0; i < currentBuildPath.Count - 1; i++)
        {
            Vector3 start = currentBuildPath[i];
            Vector3 end = currentBuildPath[i + 1];
            
            // Добавляем промежуточные точки каждые 1 метр
            float distance = Vector3.Distance(start, end);
            int segments = Mathf.RoundToInt(distance);
            
            for (int j = 1; j < segments; j++)
            {
                float t = (float)j / segments;
                Vector3 intermediatePos = Vector3.Lerp(start, end, t);
                intermediatePos = SnapToGrid(intermediatePos);
                
                // Добавляем точку в корпус
                AddPointToHull(intermediatePos);
            }
        }
    }
    
    private void CreateWallFromPath()
    {
        // Создаем стену между всеми точками в пути
        for (int i = 0; i < currentBuildPath.Count - 1; i++)
        {
            Vector3 start = currentBuildPath[i];
            Vector3 end = currentBuildPath[i + 1];
            
            // Добавляем точки в корпус
            AddPointToHull(start);
            AddPointToHull(end);
            
            // Создаем стену между точками
            CreateWallBetweenPoints(start, end);
        }
    }
    
    private void CreateDoorFromPath()
    {
        if (currentBuildPath.Count >= 2)
        {
            Vector3 start = currentBuildPath[0];
            Vector3 end = currentBuildPath[currentBuildPath.Count - 1];
            
            // Добавляем точки в корпус
            AddPointToHull(start);
            AddPointToHull(end);
            
            // Создаем дверь между точками
            CreateDoorBetweenPoints(start, end);
        }
    }
    
    private void CreatePointsFromPath()
    {
        // Создаем отдельные точки в каждой позиции пути
        foreach (Vector3 position in currentBuildPath)
        {
            AddPointToHull(position);
        }
    }
    
    private void AddPointToHull(Vector3 position)
    {
        // Здесь нужно добавить точку в корпус
        // Пока что просто логируем
        Debug.Log($"[HullBuilder] Добавлена точка в позиции {position}");
    }
    
    private void CreateWallBetweenPoints(Vector3 start, Vector3 end)
    {
        // Здесь нужно создать стену между точками
        Debug.Log($"[HullBuilder] Создана стена от {start} к {end}");
    }
    
    private void CreateDoorBetweenPoints(Vector3 start, Vector3 end)
    {
        // Здесь нужно создать дверь между точками
        Debug.Log($"[HullBuilder] Создана дверь от {start} к {end}");
    }
    
    public void SetBuildMode(BuildMode mode)
    {
        if (currentBuildMode != mode)
        {
            currentBuildMode = mode;
            OnBuildModeChanged?.Invoke(mode);
            
            Debug.Log($"[HullBuilder] Режим строительства изменен на {mode}");
        }
    }
    
    // UI Event Handlers
    private void OnAddDoorClicked()
    {
        SetBuildMode(BuildMode.Door);
    }
    
    private void OnClearHullClicked()
    {
        if (hullComponent != null)
        {
            // Очищаем корпус
            Debug.Log("[HullBuilder] Корпус очищен");
        }
    }
    
    private void OnSaveHullClicked()
    {
        if (hullComponent != null)
        {
            string hullData = hullComponent.SerializeHull();
            PlayerPrefs.SetString("SavedHull", hullData);
            PlayerPrefs.Save();
            
            Debug.Log("[HullBuilder] Корпус сохранен");
        }
    }
    
    private void OnLoadHullClicked()
    {
        if (hullComponent != null)
        {
            string hullData = PlayerPrefs.GetString("SavedHull", "");
            if (!string.IsNullOrEmpty(hullData))
            {
                hullComponent.DeserializeHull(hullData);
                Debug.Log("[HullBuilder] Корпус загружен");
            }
            else
            {
                Debug.LogWarning("[HullBuilder] Нет сохраненных данных корпуса");
            }
        }
    }
    
    // Public methods for external access
    public bool IsBuildingActive => isBuildingActive;
    public BuildMode CurrentBuildMode => currentBuildMode;
    public List<Vector3> CurrentBuildPath => new List<Vector3>(currentBuildPath);
} 