using UnityEngine;

public class HullDebugTest : MonoBehaviour
{
    [Header("Debug Test")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoSwitchToBuildMode = true;
    
    private HULL hullComponent;
    private HullBuilder hullBuilder;
    private SHIP_UI shipUI;
    
    void Start()
    {
        // Находим компоненты
        hullComponent = FindObjectOfType<HULL>();
        hullBuilder = FindObjectOfType<HullBuilder>();
        shipUI = FindObjectOfType<SHIP_UI>();
        
        Debug.Log("=== HULL DEBUG TEST STARTED ===");
        Debug.Log($"HULL компонент: {(hullComponent != null ? "найден" : "НЕ НАЙДЕН")}");
        Debug.Log($"HullBuilder компонент: {(hullBuilder != null ? "найден" : "НЕ НАЙДЕН")}");
        Debug.Log($"SHIP_UI компонент: {(shipUI != null ? "найден" : "НЕ НАЙДЕН")}");
        
        if (hullComponent != null)
        {
            Debug.Log($"pointPrefab: {(hullComponent.pointPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
            Debug.Log($"wallPrefab: {(hullComponent.wallPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
            Debug.Log($"doorPrefab: {(hullComponent.doorPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
        }
        
        if (Camera.main != null)
        {
            Debug.Log($"Camera.main: найден в позиции {Camera.main.transform.position}");
        }
        else
        {
            Debug.LogError("Camera.main НЕ НАЙДЕН!");
        }
        
        // Автоматически переключаем в режим строительства
        if (autoSwitchToBuildMode && shipUI != null)
        {
            Invoke("SwitchToBuildMode", 1f);
        }
    }
    
    void SwitchToBuildMode()
    {
        if (shipUI != null)
        {
            Debug.Log("[HullDebugTest] Переключаемся в режим строительства");
            shipUI.SetState(SHIP_UI.State._ship_state_editor_main_module_6);
        }
    }
    
    void Update()
    {
        if (!showDebugInfo) return;
        
        // Показываем отладочную информацию каждые 2 секунды
        if (Time.frameCount % 120 == 0)
        {
            ShowDebugInfo();
        }
        
        // Обработка клавиш для отладки
        HandleDebugInput();
    }
    
    void ShowDebugInfo()
    {
        Debug.Log("=== HULL DEBUG INFO ===");
        
        if (hullComponent != null)
        {
            Debug.Log($"isBuilding: {hullComponent.isBuilding}");
            Debug.Log($"previewPoint: {(hullComponent.previewPoint != null ? "существует" : "НЕ СУЩЕСТВУЕТ")}");
            Debug.Log($"previewPosition: {hullComponent.previewPosition}");
            Debug.Log($"isDragging: {hullComponent.isDragging}");
            Debug.Log($"points count: {hullComponent.points?.Count ?? 0}");
            Debug.Log($"walls count: {hullComponent.walls?.Count ?? 0}");
            Debug.Log($"drag path count: {hullComponent.currentDragPath?.Count ?? 0}");
            Debug.Log($"previewWall: {(hullComponent.previewWall != null ? "существует" : "НЕ СУЩЕСТВУЕТ")}");
        }
        
        if (shipUI != null)
        {
            Debug.Log($"currentState: {shipUI.CurrentState}");
        }
        
        if (Camera.main != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            Debug.Log($"mouse world position: {mouseWorldPos}");
        }
    }
    
    void HandleDebugInput()
    {
        // Клавиши для отладки
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== FORCED BUILD MODE SWITCH ===");
            if (shipUI != null)
            {
                shipUI.SetState(SHIP_UI.State._ship_state_editor_main_module_6);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("=== FORCED SPACE MODE SWITCH ===");
            if (shipUI != null)
            {
                shipUI.SetState(SHIP_UI.State._ship_state_space);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("=== MANUAL POINT CREATION ===");
            if (hullComponent != null)
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
                hullComponent.AddPoint(pos);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("=== CLEAR HULL ===");
            if (hullComponent != null)
            {
                hullComponent.ClearHull();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("=== CREATE PREVIEW POINT ===");
            if (hullComponent != null)
            {
                hullComponent.CreatePreviewPoint();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("=== CANCEL BUILDING ===");
            if (hullComponent != null)
            {
                hullComponent.CancelDragging();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("=== DELETE NEAREST NODE ===");
            if (hullComponent != null)
            {
                hullComponent.DeleteNearestNode();
            }
        }
    }
    
    // UI для отладки
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        
        GUILayout.Label("Hull Debug Test", GUI.skin.box);
        
        GUILayout.Label("Controls:", GUI.skin.box);
        GUILayout.Label("LMB: Build points and walls");
        GUILayout.Label("RMB: Cancel building / Delete node");
        GUILayout.Label("F1: Switch to build mode");
        GUILayout.Label("F2: Switch to space mode");
        GUILayout.Label("F3: Create manual point");
        GUILayout.Label("F4: Clear hull");
        GUILayout.Label("F5: Create preview point");
        GUILayout.Label("F6: Cancel building");
        GUILayout.Label("F7: Delete nearest node");
        
        if (GUILayout.Button("Switch to Build Mode (F1)"))
        {
            SwitchToBuildMode();
        }
        
        if (GUILayout.Button("Switch to Space Mode (F2)"))
        {
            if (shipUI != null)
            {
                shipUI.SetState(SHIP_UI.State._ship_state_space);
            }
        }
        
        if (GUILayout.Button("Create Manual Point (F3)"))
        {
            if (hullComponent != null && Camera.main != null)
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
                hullComponent.AddPoint(pos);
            }
        }
        
        if (GUILayout.Button("Clear Hull (F4)"))
        {
            if (hullComponent != null)
            {
                hullComponent.ClearHull();
            }
        }
        
        if (GUILayout.Button("Force Update Preview"))
        {
            if (hullComponent != null)
            {
                hullComponent.UpdatePreview();
            }
        }
        
        if (GUILayout.Button("Create Preview Point (F5)"))
        {
            if (hullComponent != null)
            {
                hullComponent.CreatePreviewPoint();
            }
        }
        
        if (GUILayout.Button("Cancel Building (F6)"))
        {
            if (hullComponent != null)
            {
                hullComponent.CancelDragging();
            }
        }
        
        if (GUILayout.Button("Delete Nearest Node (F7)"))
        {
            if (hullComponent != null)
            {
                hullComponent.DeleteNearestNode();
            }
        }
        
        if (GUILayout.Button("Check Prefabs"))
        {
            if (hullComponent != null)
            {
                Debug.Log("=== PREFAB CHECK ===");
                Debug.Log($"pointPrefab: {(hullComponent.pointPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
                Debug.Log($"wallPrefab: {(hullComponent.wallPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
                Debug.Log($"doorPrefab: {(hullComponent.doorPrefab != null ? "назначен" : "НЕ НАЗНАЧЕН")}");
                
                if (hullComponent.pointPrefab != null)
                {
                    Debug.Log($"pointPrefab name: {hullComponent.pointPrefab.name}");
                    Debug.Log($"pointPrefab has Renderer: {(hullComponent.pointPrefab.GetComponent<Renderer>() != null ? "да" : "нет")}");
                }
                
                if (hullComponent.wallPrefab != null)
                {
                    Debug.Log($"wallPrefab name: {hullComponent.wallPrefab.name}");
                    Debug.Log($"wallPrefab has Renderer: {(hullComponent.wallPrefab.GetComponent<Renderer>() != null ? "да" : "нет")}");
                    Debug.Log($"wallPrefab has HullWallPrefab: {(hullComponent.wallPrefab.GetComponent<HullWallPrefab>() != null ? "да" : "нет")}");
                }
            }
        }
        
        // Показываем статус
        GUILayout.Label("Status:", GUI.skin.box);
        
        if (hullComponent != null)
        {
            GUILayout.Label($"Building: {hullComponent.isBuilding}");
            GUILayout.Label($"Preview Point: {(hullComponent.previewPoint != null ? "✓" : "✗")}");
            GUILayout.Label($"Points: {hullComponent.points?.Count ?? 0}");
            GUILayout.Label($"Walls: {hullComponent.walls?.Count ?? 0}");
            GUILayout.Label($"Doors: {hullComponent.doors?.Count ?? 0}");
            GUILayout.Label($"Dragging: {hullComponent.isDragging}");
            GUILayout.Label($"Drag Path: {hullComponent.currentDragPath?.Count ?? 0}");
            
            // Показываем позицию предварительного просмотра
            if (hullComponent.previewPoint != null)
            {
                Vector3 pos = hullComponent.previewPoint.transform.position;
                GUILayout.Label($"Preview Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
        }
        
        if (shipUI != null)
        {
            GUILayout.Label($"State: {shipUI.CurrentState}");
        }
        
        GUILayout.Label($"Camera: {(Camera.main != null ? "✓" : "✗")}");
        
        GUILayout.EndArea();
    }
} 