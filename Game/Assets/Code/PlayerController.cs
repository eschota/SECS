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
    private List<io_weapon> weapons = new List<io_weapon>();
    private Machine currentMachine;
    private Rigidbody machineRigidbody;
    
    // Система емкости двигателей 
    private bool isEngineOverheated = false;

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

        // Управление двигателями
        HandleEngineControl();
    }
    
    private void Update()
    {
        if (!Object || !Object.HasInputAuthority) return;
        
        // Обновляем состояние перегрева двигателей
        UpdateEngineOverheatState();
        
        // Управление оружием
        HandleWeaponControl();
    }
    
    private void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.SimulateOnline)
        {
            // При переходе в режим симуляции ищем машину и модули
            InitMachineModules();
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
    
    // ========== СИСТЕМА УПРАВЛЕНИЯ МОДУЛЯМИ ==========
    
    private void InitMachineModules()
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
                // Debug.Log($"PlayerController: Найдена машина {machine.name}");
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
        
        // Ищем все оружия в машине
        weapons.Clear();
        var weaponComponents = currentMachine.GetComponentsInChildren<io_weapon>(true);
        weapons.AddRange(weaponComponents);
        
        Debug.Log($"PlayerController: Найдено {weapons.Count} оружий в машине");
        
        // Инициализируем оружия
        foreach (var weapon in weapons)
        {
            if (weapon.weapon_SO != null)
            {
                weapon.InitializeWeapon(Runner);
                Debug.Log($"PlayerController: Оружие {weapon.name} - индекс: {weapon.weapon_SO.weapon_index}, патроны: {weapon.GetCurrentAmmo()}/{weapon.GetMaxAmmo()}");
            }
            else
            {
                Debug.LogWarning($"PlayerController: Оружие {weapon.name} не имеет настроек Weapon_SO!");
            }
        }
        
        // Ищем UI компонент для емкости двигателей
        
        
        // Debug.Log($"PlayerController: Найдено {engines.Count} двигателей и {weapons.Count} оружий в машине");
        foreach (var engine in engines)
        {
            if (engine.engineSettings != null)
            {
                // Debug.Log($"PlayerController: Двигатель {engine.name} - направление: {engine.engineSettings.force_vector_local}, мощность: {engine.engineSettings.force_power}");
            }
            else
            {
                Debug.LogWarning($"PlayerController: Двигатель {engine.name} не имеет настроек Engine_SO!");
            }
        }
    }
    
    private void UpdateEngineOverheatState()
    {
        // Проверяем состояние перегрева через UI компонент
        if (UI_Canvas.i.engine_burst != null)
        {
            // Используем рефлексию для доступа к приватным полям
            var disableTimerField = typeof(ui_engine_burst).GetField("DisableTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isOverheatedField = typeof(ui_engine_burst).GetField("isOverheated", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (disableTimerField != null && isOverheatedField != null)
            {
                float disableTimer = (float)disableTimerField.GetValue(UI_Canvas.i.engine_burst);
                bool isOverheated = (bool)isOverheatedField.GetValue(UI_Canvas.i.engine_burst);
                
                isEngineOverheated = disableTimer > 0 || isOverheated;
            }
        }
    }
    
    private void HandleEngineControl()
    {
        if (engines.Count == 0 || machineRigidbody == null) return;
        
        // Если двигатели перегреты - отключаем их полностью
        if (isEngineOverheated)
        {
            foreach (var engine in engines)
            {
                engine.UpdateEngineState(0f, Time.fixedDeltaTime);
            }
            return;
        }
        
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
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        float powerMultiplier = 1f;
        
        // Проверяем емкость двигателей при зажатом Shift
        if (isShiftPressed && UI_Canvas.i.engine_burst != null)
        {
            // Используем рефлексию для доступа к приватному полю engine_capacity
            var engineCapacityField = typeof(ui_engine_burst).GetField("engine_capacity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (engineCapacityField != null)
            {
                var engineCapacity = engineCapacityField.GetValue(UI_Canvas.i.engine_burst) as UnityEngine.UI.Image;
                if (engineCapacity != null && engineCapacity.fillAmount > 0)
                {
                    powerMultiplier = 2f; // Удваиваем мощность при наличии емкости
                }
                else
                {
                    powerMultiplier = 0f; // Отключаем двигатели если емкость кончилась
                }
            }
        }
        
        foreach (var engine in engines)
        {
            if (engine.engineSettings == null) continue;
            
            // Получаем мировое направление двигателя
            Vector3 engineWorldDirection = engine.transform.TransformDirection(engine.engineSettings.force_vector_local);
            
            // Вычисляем угол между направлением двигателя и целевым направлением
            float angle = Vector3.Angle(engineWorldDirection, targetDirection);
            
            // Если угол меньше 90 градусов, двигатель может помочь в движении
            if (angle < 90f)
            {
                // Вычисляем эффективность двигателя (1.0 = полная эффективность, 0.0 = неэффективен)
                float effectiveness = Mathf.Cos(angle * Mathf.Deg2Rad);
                
                // Применяем множитель мощности
                effectiveness *= powerMultiplier;
                
                // Обновляем состояние двигателя на основе эффективности
                engine.UpdateEngineState(effectiveness, Time.fixedDeltaTime);
                
                // Применяем силу через новый метод
                engine.ApplyForce(machineRigidbody);
                
                // Debug.Log($"Engine {engine.name}: направление {engineWorldDirection}, эффективность {effectiveness:F2}, множитель {powerMultiplier}");
            }
            else
            {
                // Двигатель не эффективен в этом направлении - останавливаем его
                engine.UpdateEngineState(0f, Time.fixedDeltaTime);
            }
        }
    }
    
    // ========== СИСТЕМА УПРАВЛЕНИЯ ОРУЖИЕМ ==========
    
    private void HandleWeaponControl()
    {
        if (weapons.Count == 0) return;
        
        // Определяем направление стрельбы (вперед относительно камеры)
        Vector3 fireDirection = Vector3.forward;
        if (Camera.main != null)
        {
            fireDirection = Camera.main.transform.forward;
            fireDirection.y = 0; // Проецируем на горизонтальную плоскость
            fireDirection.Normalize();
        }
        
        // Левый клик мыши - стреляет оружием с weapon_index == 0
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"PlayerController: Левый клик мыши! Стреляем оружием 0");
            FireWeapon(0, fireDirection);
        }
        
        // Цифры 1-5 - стреляют соответствующими оружиями
        for (int i = 1; i <= 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                FireWeapon(i, fireDirection);
            }
        }
    }
    
    private void FireWeapon(int weaponIndex, Vector3 direction)
    {
        Debug.Log($"PlayerController: FireWeapon вызван для индекса {weaponIndex}");
        
        // Ищем оружие с нужным индексом
        foreach (var weapon in weapons)
        {
            if (weapon.weapon_SO != null && weapon.weapon_SO.weapon_index == weaponIndex)
            {
                Debug.Log($"PlayerController: Найдено оружие {weapon.name} с индексом {weaponIndex}");
                
                if (weapon.CanFire())
                {
                    weapon.Fire(Runner, direction);
                    Debug.Log($"PlayerController: Выстрел из оружия {weapon.name} (индекс {weaponIndex})");
                }
                else
                {
                    Debug.Log($"PlayerController: Оружие {weapon.name} не может стрелять (патроны: {weapon.GetCurrentAmmo()}/{weapon.GetMaxAmmo()})");
                }
                break; // Стреляем только из первого найденного оружия с нужным индексом
            }
        }
    }
}
