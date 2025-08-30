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

    // Модули корабля
    private List<io_engine> engines = new List<io_engine>();
    private List<io_weapon> weapons = new List<io_weapon>();
    private Machine currentMachine;
    private Rigidbody machineRigidbody;

    // Перегрев двигателей (UI)
    private bool isEngineOverheated = false;

    // ─────────────────────────────────────────────────────────────────────────────

    public override void Spawned()
    {
        ValidateAndSerializeMeshRenderers();
        GenerateRandomBrightColor();
        ApplyEmissiveColor();

        Play.OnPlayStateChange += OnPlayStateChange;
    }

    private void OnDestroy()
    {
        Play.OnPlayStateChange -= OnPlayStateChange;
    }

    private void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.SimulateOnline)
            InitMachineModules();
    }

    // ─────────────────────────── РЕНДЕР / ВИЗУАЛ ────────────────────────────────
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

    // ──────────────────────── ПОИСК МОДУЛЕЙ МАШИНЫ ──────────────────────────────
    private void InitMachineModules()
    {
        // Ищем свою машину по InputAuthority
        var machines = FindObjectsOfType<Machine>();
        foreach (var machine in machines)
        {
            var no = machine.GetComponent<NetworkObject>();
            if (no != null && no.InputAuthority == Object.InputAuthority)
            {
                currentMachine = machine;
                machineRigidbody = machine.GetComponent<Rigidbody>();
                break;
            }
        }

        if (currentMachine == null)
        {
            Debug.LogWarning("PlayerController: Машина не найдена!");
            return;
        }

        // Двигатели
        engines.Clear();
        engines.AddRange(currentMachine.GetComponentsInChildren<io_engine>(true));

        // Оружие
        weapons.Clear();
        weapons.AddRange(currentMachine.GetComponentsInChildren<io_weapon>(true));

        // Инициализация оружий
        foreach (var weapon in weapons)
        {
            if (weapon.weapon_SO != null)
            {
                weapon.InitializeWeapon(Runner);
                // Debug.Log($"Weapon {weapon.name}: idx={weapon.weapon_SO.weapon_index} ammo {weapon.GetCurrentAmmo()}/{weapon.GetMaxAmmo()}");
            }
                         else
             {
                 Debug.LogWarning($"PlayerController: Оружие {weapon.name} не имеет Weapon_SO!");
             }
         }
     }

    /// <summary>
    /// Очищает уничтоженные двигатели из списка
    /// </summary>
    private void CleanupDestroyedEngines()
    {
        for (int i = engines.Count - 1; i >= 0; i--)
        {
            if (engines[i] == null)
            {
                engines.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Очищает уничтоженное оружие из списка
    /// </summary>
    private void CleanupDestroyedWeapons()
    {
        for (int i = weapons.Count - 1; i >= 0; i--)
        {
            if (weapons[i] == null)
            {
                weapons.RemoveAt(i);
            }
        }
    }

    // ─────────────────────────── СЕТЕВАЯ ФИЗИКА ─────────────────────────────────
    // Все силы/движение — только здесь и только на StateAuthority.
    public override void FixedUpdateNetwork()
    {
        if (!Object || !Object.HasStateAuthority) return;
        HandleEngineControlNetwork();
    }

    private void HandleEngineControlNetwork()
    {
        // Очищаем уничтоженные двигатели из списка
        CleanupDestroyedEngines();
        
        if (engines.Count == 0 || machineRigidbody == null) return;

        // Перегрев — глушим тягу
        if (isEngineOverheated)
        {
            foreach (var e in engines)
                e.UpdateEngineState(0f, Runner.DeltaTime);
            return;
        }

        // Собираем направление движения из ввода (локально у владельца state)
        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.C)) dir += Vector3.down;

        if (Camera.main != null)
        {
            if (Input.GetKey(KeyCode.W))
            {
                var f = Camera.main.transform.forward; f.y = 0f; dir += f.sqrMagnitude > 0 ? f.normalized : Vector3.zero;
            }
            if (Input.GetKey(KeyCode.S))
            {
                var b = -Camera.main.transform.forward; b.y = 0f; dir += b.sqrMagnitude > 0 ? b.normalized : Vector3.zero;
            }
            if (Input.GetKey(KeyCode.A))
            {
                var l = -Camera.main.transform.right; l.y = 0f; dir += l.sqrMagnitude > 0 ? l.normalized : Vector3.zero;
            }
            if (Input.GetKey(KeyCode.D))
            {
                var r = Camera.main.transform.right; r.y = 0f; dir += r.sqrMagnitude > 0 ? r.normalized : Vector3.zero;
            }
        }

        if (dir == Vector3.zero)
        {
            foreach (var e in engines)
                e.UpdateEngineState(0f, Runner.DeltaTime);
            return;
        }
        dir.Normalize();

        // Boost из UI (Shift + емкость)
        float powerMultiplier = 1f;
        bool shift = Input.GetKey(KeyCode.LeftShift);
        if (shift && UI_Canvas.i.engine_burst != null)
        {
            var engineCapacityField = typeof(ui_engine_burst).GetField("engine_capacity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (engineCapacityField != null)
            {
                var engineCapacity = engineCapacityField.GetValue(UI_Canvas.i.engine_burst) as UnityEngine.UI.Image;
                if (engineCapacity != null && engineCapacity.fillAmount > 0)
                    powerMultiplier = 2f;
            }
        }

        // Применяем тягу по эффективности направления
        foreach (var engine in engines)
        {
            if (engine.engineSettings == null) continue;

            Vector3 engineWorldDir = engine.transform.TransformDirection(engine.engineSettings.force_vector_local);
            float angle = Vector3.Angle(engineWorldDir, dir);
            float eff = angle < 90f ? Mathf.Cos(angle * Mathf.Deg2Rad) * powerMultiplier : 0f;

            // Апдейт состояния с сетевым дельта-таймом
            eengine_UpdateAndApply(engine, eff, Runner.DeltaTime);
        }
    }

    // Вынесено, чтобы не забывать Runner.DeltaTime и порядок вызовов
    private void eengine_UpdateAndApply(io_engine engine, float effectiveness, float dt)
    {
        engine.UpdateEngineState(effectiveness, dt);
        engine.ApplyForce(machineRigidbody); // внутри должен быть AddForce/Acceleration, без собственного deltaTime
    }

    // ─────────────────────────── ЛОКАЛЬНЫЙ ВВОД ─────────────────────────────────
    private void Update()
    {
        // Ввод/стрельба/перегрев считаем только у владельца ввода
        if (!Object || !Object.HasInputAuthority) return;

        UpdateEngineOverheatState();
        HandleWeaponControl();
    }

    private void UpdateEngineOverheatState()
    {
        if (UI_Canvas.i.engine_burst == null) { isEngineOverheated = false; return; }

        var disableTimerField = typeof(ui_engine_burst).GetField("DisableTimer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isOverheatedField = typeof(ui_engine_burst).GetField("isOverheated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (disableTimerField != null && isOverheatedField != null)
        {
            float disableTimer = (float)disableTimerField.GetValue(UI_Canvas.i.engine_burst);
            bool overheated = (bool)isOverheatedField.GetValue(UI_Canvas.i.engine_burst);
            isEngineOverheated = disableTimer > 0f || overheated;
        }
    }

    // ───────────────────────────── ОРУЖИЕ / ВВОД ────────────────────────────────
    private void HandleWeaponControl()
    {
        // Очищаем уничтоженное оружие из списка
        CleanupDestroyedWeapons();
        
        if (weapons.Count == 0) return;

        // Направление стрельбы — вперёд от камеры по горизонту (ориентация пули берётся по pivot Z в io_weapon)
        Vector3 fireDir = Vector3.forward;
        if (Camera.main != null)
        {
            fireDir = Camera.main.transform.forward;
            fireDir.y = 0f;
            fireDir = fireDir.sqrMagnitude > 0 ? fireDir.normalized : Vector3.forward;
        }

        // ЛКМ — все орудия с индексом 0 (автоогонь)
        if (Input.GetMouseButton(0))
            FireWeaponsByIndex(0, fireDir);

        // Цифры 1..5 — соответствующие группы (автоогонь)
        if (Input.GetKey(KeyCode.Alpha1)) FireWeaponsByIndex(1, fireDir);
        if (Input.GetKey(KeyCode.Alpha2)) FireWeaponsByIndex(2, fireDir);
        if (Input.GetKey(KeyCode.Alpha3)) FireWeaponsByIndex(3, fireDir);
        if (Input.GetKey(KeyCode.Alpha4)) FireWeaponsByIndex(4, fireDir);
        if (Input.GetKey(KeyCode.Alpha5)) FireWeaponsByIndex(5, fireDir);
        if (Input.GetKey(KeyCode.Alpha0)) FireWeaponsByIndex(0, fireDir); // опционально
    }

    private void FireWeaponsByIndex(int weaponIndex, Vector3 direction)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            if (w == null) continue;
            var so = w.weapon_SO;
            if (so == null || so.weapon_index != weaponIndex) continue;

            if (w.CanFire())
                w.Fire(Runner, direction);
        }
    }
}
