using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private MeshRenderer[] meshRenderers;
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;
    [SerializeField] private Color playerColor;

    private Material[] originalMaterials;
    private Material[] coloredMaterials;
    
    // Система управления двигателями
    private List<io_engine> engines = new List<io_engine>();
    private Machine currentMachine;
    private Rigidbody machineRigidbody;

    public override void Spawned()
    {
        ValidateAndSerializeMeshRenderers();
        GenerateRandomBrightColor();
        ApplyEmissiveColor();
        
        // Подписываемся на изменение состояния игры
        Play.OnPlayStateChange += OnPlayStateChange;
    }

    // ДВИЖЕНИЕ ТОЛЬКО У ВЛАДЕЛЬЦА!
    private void FixedUpdate()
    {
        if (!Object || !Object.HasInputAuthority) return;

        if (Camera.main != null)
        {
            transform.SetPositionAndRotation(
                Camera.main.transform.position,
                Camera.main.transform.rotation
            );
        }
        
        // Управление двигателями
        HandleEngineControl();
    }
    
    private void Update()
    {
        if (!Object || !Object.HasInputAuthority) return;
        
        // Поиск машины и двигателей при переходе в режим симуляции
        if (Play.i?.currentState == Play.State.SimulateOnline && engines.Count == 0)
        {
            FindMachineAndEngines();
        }
    }
    
    private void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.SimulateOnline)
        {
            // При переходе в режим симуляции ищем машину и двигатели
            FindMachineAndEngines();
        }
    }

    [ContextMenu("Validate Mesh Renderers")]
    void ValidateAndSerializeMeshRenderers()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        var origMats = new List<Material>();
        foreach (var mr in meshRenderers) origMats.AddRange(mr.materials);
        foreach (var smr in skinnedMeshRenderers) origMats.AddRange(smr.materials);
        originalMaterials = origMats.ToArray();
    }

    void GenerateRandomBrightColor()
    {
        float hue = Random.Range(0f, 1f);
        float saturation = Random.Range(0.7f, 1f);
        float value = Random.Range(0.8f, 1f);
        playerColor = Color.HSVToRGB(hue, saturation, value);
    }

    void ApplyEmissiveColor()
    {
        foreach (var r in meshRenderers) ApplyColorToRenderer(r);
        foreach (var r in skinnedMeshRenderers) ApplyColorToSkinnedRenderer(r);
    }

    void ApplyColorToRenderer(MeshRenderer renderer)
    {
        var mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i]; if (m == null) continue;
            var nm = new Material(m);
            if (nm.HasProperty("_BaseColor")) nm.SetColor("_BaseColor", playerColor);
            else if (nm.HasProperty("_Color")) nm.SetColor("_Color", playerColor);
            if (nm.HasProperty("_EmissionColor"))
            {
                nm.SetColor("_EmissionColor", playerColor * 0.5f);
                nm.EnableKeyword("_EMISSION");
            }
            mats[i] = nm;
        }
        renderer.materials = mats;
    }

    void ApplyColorToSkinnedRenderer(SkinnedMeshRenderer renderer)
    {
        var mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i]; if (m == null) continue;
            var nm = new Material(m);
            if (nm.HasProperty("_BaseColor")) nm.SetColor("_BaseColor", playerColor);
            else if (nm.HasProperty("_Color")) nm.SetColor("_Color", playerColor);
            if (nm.HasProperty("_EmissionColor"))
            {
                nm.SetColor("_EmissionColor", playerColor * 0.5f);
                nm.EnableKeyword("_EMISSION");
            }
            mats[i] = nm;
        }
        renderer.materials = mats;
    }

    public void SetPlayerColor(Color c) { playerColor = c; ApplyEmissiveColor(); }
    public Color GetPlayerColor() => playerColor;
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        Play.OnPlayStateChange -= OnPlayStateChange;
    }
    
    // ========== СИСТЕМА УПРАВЛЕНИЯ ДВИГАТЕЛЯМИ ==========
    
    private void FindMachineAndEngines()
    {
        // Ищем свою машину
        var machines = FindObjectsOfType<Machine>();
        foreach (var machine in machines)
        {
            var networkObject = machine.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.InputAuthority == Object.InputAuthority)
            {
                currentMachine = machine;
                machineRigidbody = machine.GetComponent<Rigidbody>();
                Debug.Log($"PlayerController: Найдена машина {machine.name}");
                break;
            }
        }
        
        if (currentMachine == null)
        {
            Debug.LogWarning("PlayerController: Машина не найдена!");
            return;
        }
        
        // Ищем все двигатели в машине
        engines.Clear();
        var engineComponents = currentMachine.GetComponentsInChildren<io_engine>(true);
        engines.AddRange(engineComponents);
        
        Debug.Log($"PlayerController: Найдено {engines.Count} двигателей в машине");
        foreach (var engine in engines)
        {
            Debug.Log($"PlayerController: Двигатель {engine.name} - направление: {engine.force_vector_local}, мощность: {engine.force_power}");
        }
    }
    
    private void HandleEngineControl()
    {
        if (engines.Count == 0 || machineRigidbody == null) return;
        
        Vector3 targetDirection = Vector3.zero;
        
        // Определяем направление движения на основе нажатых клавиш
        if (Input.GetKey(KeyCode.E)) // Вверх
        {
            targetDirection += Vector3.up;
        }
        if (Input.GetKey(KeyCode.C)) // Вниз
        {
            targetDirection += Vector3.down;
        }
        if (Input.GetKey(KeyCode.W)) // Вперед (относительно камеры)
        {
            if (Camera.main != null)
            {
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0; // Проецируем на горизонтальную плоскость
                cameraForward.Normalize();
                targetDirection += cameraForward;
            }
        }
        if (Input.GetKey(KeyCode.S)) // Назад (относительно камеры)
        {
            if (Camera.main != null)
            {
                Vector3 cameraBack = -Camera.main.transform.forward;
                cameraBack.y = 0; // Проецируем на горизонтальную плоскость
                cameraBack.Normalize();
                targetDirection += cameraBack;
            }
        }
        if (Input.GetKey(KeyCode.A)) // Влево (относительно камеры)
        {
            if (Camera.main != null)
            {
                Vector3 cameraLeft = -Camera.main.transform.right;
                cameraLeft.y = 0; // Проецируем на горизонтальную плоскость
                cameraLeft.Normalize();
                targetDirection += cameraLeft;
            }
        }
        if (Input.GetKey(KeyCode.D)) // Вправо (относительно камеры)
        {
            if (Camera.main != null)
            {
                Vector3 cameraRight = Camera.main.transform.right;
                cameraRight.y = 0; // Проецируем на горизонтальную плоскость
                cameraRight.Normalize();
                targetDirection += cameraRight;
            }
        }
        
        if (targetDirection != Vector3.zero)
        {
            targetDirection.Normalize();
            ApplyEnginesForce(targetDirection);
        }
    }
    
    private void ApplyEnginesForce(Vector3 targetDirection)
    {
        foreach (var engine in engines)
        {
            // Получаем мировое направление двигателя
            Vector3 engineWorldDirection = engine.transform.TransformDirection(engine.force_vector_local);
            
            // Вычисляем угол между направлением двигателя и целевым направлением
            float angle = Vector3.Angle(engineWorldDirection, targetDirection);
            
            // Если угол меньше 90 градусов, двигатель может помочь в движении
            if (angle < 90f)
            {
                // Вычисляем эффективность двигателя (1.0 = полная эффективность, 0.0 = неэффективен)
                float effectiveness = Mathf.Cos(angle * Mathf.Deg2Rad);
                
                // Применяем силу через AddForceAtPosition
                Vector3 force = engineWorldDirection * engine.force_power * effectiveness;
                machineRigidbody.AddForceAtPosition(force, engine.transform.position, engine.force_type);
                
                Debug.Log($"Engine {engine.name}: направление {engineWorldDirection}, эффективность {effectiveness:F2}, сила {force}");
            }
        }
    }
}
