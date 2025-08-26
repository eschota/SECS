using UnityEngine;
using Fusion;

public class io_weapon : io_base
{
    public Weapon_SO weapon_SO;
    public Transform[] bullet_pivots;

    [Header("Weapon State")]
    [SerializeField] private float currentAmmo;
    [SerializeField] private float maxAmmo;
    [SerializeField] private float lastFireTime;
    [SerializeField] private bool  canFire = true;

    private NetworkRunner _runner;
    // поля
private Rigidbody _carrierRb;  // именно РБ машины, не локальный

void Awake()
{
    // Берём РБ с объекта Machine (корня корабля)
    var machine = GetComponentInParent<Machine>();
    _carrierRb = machine ? machine.GetComponent<Rigidbody>() : null;

    if (_carrierRb == null)
        _carrierRb = transform.root.GetComponent<Rigidbody>(); // запасной вариант
}


    void Start() => InitializeAmmo();

    public void InitializeWeapon(NetworkRunner runner)
    {
        _runner = runner;
        InitializeAmmo();
    }

    private void InitializeAmmo()
    {
        if (weapon_SO == null)
        {
            Debug.LogError($"io_weapon: {name} не имеет weapon_SO!");
            return;
        }
        currentAmmo = weapon_SO.maxAmmo;
        maxAmmo     = weapon_SO.maxAmmo;

        if (weapon_SO.bullet_SO == null)
            Debug.LogError($"io_weapon: {name} - weapon_SO.bullet_SO = null!");
    }

    public bool CanFire()
    {
        if (weapon_SO == null || weapon_SO.bullet_SO == null) return false;
        if (currentAmmo <= 0) return false;
        if (Time.time - lastFireTime < 1f / weapon_SO.fireRate) return false;
        return canFire;
    }

    public void Fire(NetworkRunner runner, Vector3 _)
    {
        if (!CanFire()) return;

        lastFireTime = Time.time;
        currentAmmo = Mathf.Max(0, currentAmmo - 1);

        if (bullet_pivots == null || bullet_pivots.Length == 0) return;
        foreach (var pivot in bullet_pivots)
            if (pivot) SpawnBullet(runner, pivot); // <— только pivot
    }

    // ====== ВАЖНО: одна сигнатура с pivot ======
    // единственный вызов спавна — сигнатура с pivot
private void SpawnBullet(NetworkRunner runner, Transform pivot)
{
    var so = weapon_SO?.bullet_SO;
    if (so == null || so.prefab == null)
    {
        Debug.LogError($"[{name}] bullet_SO/prefab не задан");
        return;
    }

    // направление Z=>Z
    Vector3 dir = pivot.TransformDirection(Vector3.forward);
    Vector3 up  = pivot.TransformDirection(Vector3.up);

    // НАСЛЕДУЕМ СКОРОСТЬ КОРАБЛЯ (именно корневой RB!)
    Vector3 inheritVel = _carrierRb ? _carrierRb.linearVelocity : Vector3.zero;

    // so.prefab — io_base → используем overload Spawn(GameObject,...)
    runner.Spawn(
        so.prefab.gameObject,
        pivot.position,
        pivot.rotation,
        inputAuthority: null,
        onBeforeSpawned: (r, obj) =>
        {
            if (obj.TryGetComponent(out BulletNet net))
                net.Setup(so, dir, inheritVel, up);
        });

    // Диагностика (можно удалить после проверки)
     Debug.Log($"[{name}] fire dir={dir} inheritVel={inheritVel}");
}


    public void Reload() => currentAmmo = maxAmmo;
    public float GetAmmoPercentage() => maxAmmo > 0 ? currentAmmo / maxAmmo : 0f;
    public float GetCurrentAmmo() => currentAmmo;
    public float GetMaxAmmo()     => maxAmmo;

    void OnDrawGizmos()
    {
        if (bullet_pivots == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in bullet_pivots)
        {
            if (!p) continue;
            Gizmos.DrawWireSphere(p.position, 0.05f);
            Gizmos.DrawLine(p.position, p.position + p.forward * 0.5f);
        }
    }
}
