using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class BulletNet : NetworkBehaviour
{
    // -------- Networked (только ПОСЛЕ Spawned) --------
    [Networked] public Vector3 Direction       { get; set; }
    [Networked] public float   Speed           { get; set; }
    [Networked] public float   Range           { get; set; }
    [Networked] public Vector3 StartPos        { get; set; }
    [Networked] public Vector3 InheritVelocity { get; set; }

    [SerializeField] private io_bullet _bullet;

    private Rigidbody _rb;

    // -------- Локальные кэши (доступны ДО Spawned) --------
    private Vector3 _dirCached;
    private float   _speedCached;
    private float   _rangeCached;
    private Vector3 _inheritCached;
    private Vector3 _startCached;

    private bool _setupCalled;
    private bool _spawned;
    private bool _hitProcessed;

    private const string G  = "<color=#00FF00>";
    private const string GE = "</color>";

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_bullet == null) _bullet = GetComponent<io_bullet>();

        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.None;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _dirCached     = transform.forward;
        _speedCached   = 10f;
        _rangeCached   = 50f;
        _inheritCached = Vector3.zero;
        _startCached   = transform.position;
    }

    public override void Spawned()
    {
        _spawned = true;

        if (_setupCalled)
        {
            Direction       = _dirCached;
            Speed           = _speedCached;
            Range           = _rangeCached;
            InheritVelocity = _inheritCached;
            StartPos        = _startCached;
        }

        Debug.Log($"{G}[Bullet] Spawned. dir={_dirCached} speed={_speedCached} range={_rangeCached}{GE}");
    }

    /// <summary>Вызывается из оружия перед/в момент спавна.</summary>
    public void Setup(Bullet_SO so, Vector3 worldDir, Vector3 inheritVel, Vector3 upHint)
    {
        _dirCached     = worldDir.sqrMagnitude > 1e-4f ? worldDir.normalized : Vector3.forward;
        _speedCached   = (so && so.speed > 0f) ? so.speed : 10f;
        _rangeCached   = (so && so.range > 0f) ? so.range : 50f;
        _inheritCached = inheritVel;
        _startCached   = transform.position;
        _setupCalled   = true;

        if (_spawned)
        {
            Direction       = _dirCached;
            Speed           = _speedCached;
            Range           = _rangeCached;
            InheritVelocity = _inheritCached;
            StartPos        = _startCached;
        }

        transform.rotation = Quaternion.LookRotation(_dirCached, upHint.sqrMagnitude > 0 ? upHint : Vector3.up);
        Debug.Log($"{G}[Bullet] Setup dir={_dirCached} speed={_speedCached} range={_rangeCached}{GE}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        var dir = _spawned ? Direction       : _dirCached;
        var spd = _spawned ? Speed           : _speedCached;
        var inh = _spawned ? InheritVelocity : _inheritCached;

        _rb.linearVelocity = dir * spd + inh;

        var start = _spawned ? StartPos : _startCached;
        var rng   = _spawned ? Range    : _rangeCached;

        if ((transform.position - start).sqrMagnitude >= rng * rng)
            Runner.Despawn(Object);
    }

    // ---------------- HIT HANDLERS ----------------

    private void TryProcessHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hitProcessed) return;
        if (!Object.HasStateAuthority) return;

        // проверка расстояния до старта — без доступа к Networked
        var start = _spawned ? StartPos : _startCached;
        if ((transform.position - start).sqrMagnitude <= 1f) return;

        _hitProcessed = true;

        Debug.Log($"{G}[Bullet] HIT at {hitPoint} normal={hitNormal}{GE}");

        // целевая клетка + машина
        io_cell cell = other.GetComponent<io_cell>() ?? other.GetComponentInParent<io_cell>();
        io_base baseCell = cell ? cell.target_io_base : (other ? other.GetComponentInParent<io_base>() : null);
        Machine machine = baseCell ? baseCell.GetComponentInParent<Machine>() : null;

        if (machine && baseCell)
        {
            Vector3 wpos = baseCell.target_world_position != Vector3.zero
                         ? baseCell.target_world_position
                         : baseCell.transform.position;

            string nameHint = baseCell.gameObject.name;

            Debug.Log($"{G}[Bullet] RequestDamage -> Machine '{machine.name}', cell \"{nameHint}\" at {wpos}{GE}");

            // ⚠️ Запрос урона владельцу машины (он рассылает всем применение)
            machine.RPC_RequestDamage(wpos, nameHint, hitPoint, hitNormal);
        }
        else
        {
            Debug.Log($"{G}[Bullet] No io_base/Machine found on hit target{GE}");
        }

        Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        // избегаем чтения Networked — берём направление из текущей скорости
        Vector3 p = other.ClosestPoint(transform.position);
        Vector3 n = _rb.linearVelocity.sqrMagnitude > 1e-6f ? -_rb.linearVelocity.normalized : Vector3.up;
        TryProcessHit(other, p, n);
    }

    private void OnCollisionEnter(Collision other)
    {
        ContactPoint cp = (other.contacts != null && other.contacts.Length > 0) ? other.contacts[0] : default;
        Vector3 p = cp.point != Vector3.zero ? cp.point : transform.position;
        Vector3 n = cp.normal != Vector3.zero ? cp.normal : (_rb.linearVelocity.sqrMagnitude > 1e-6f ? -_rb.linearVelocity.normalized : Vector3.up);
        TryProcessHit(other.collider, p, n);
    }
}
