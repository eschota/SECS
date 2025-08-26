using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class BulletNet : NetworkBehaviour
{
    [Networked] public Vector3 Direction       { get; set; }  // мировое направление из pivot.forward (нормализовано)
    [Networked] public float   Speed           { get; set; }   // из SO
    [Networked] public float   Range           { get; set; }   // из SO
    [Networked] public Vector3 StartPos        { get; set; }   // позиция спавна
    [Networked] public Vector3 InheritVelocity { get; set; }   // velocity корабля на момент выстрела

    [SerializeField] private io_bullet _bullet;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_bullet == null) _bullet = GetComponent<io_bullet>();

        // пуля управляется нами, гравитация не нужна (космос)
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.None; // интерполяция делает Fusion
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>Заполняется в onBeforeSpawned из io_weapon.</summary>
public void Setup(Bullet_SO so, Vector3 worldDir, Vector3 inheritVel, Vector3 upHint)
{
    Direction       = worldDir.sqrMagnitude > 1e-4f ? worldDir.normalized : Vector3.forward;
    Speed           = (so && so.speed > 0f) ? so.speed : 10f;
    Range           = (so && so.range > 0f) ? so.range : 50f;
    InheritVelocity = inheritVel;                       // ← ВАЖНО
    StartPos        = transform.position;

    transform.rotation = Quaternion.LookRotation(Direction, upHint.sqrMagnitude>0 ? upHint : Vector3.up);
}


  public override void FixedUpdateNetwork()
{
    if (!Object.HasStateAuthority) return;

    // Собственная скорость + скорость корабля
    _rb.linearVelocity = Direction * Speed + InheritVelocity;   // ← ВАЖНО

    if ((transform.position - StartPos).sqrMagnitude >= Range * Range)
        Runner.Despawn(Object);
}


    private void OnTriggerEnter(Collider other)
    {
        // чтобы не убивать пулю коллайдером носителя сразу в месте спавна
        if ((transform.position - StartPos).sqrMagnitude <= 1f) return;
        if (!Object.HasStateAuthority) return;
        Runner.Despawn(Object);
    }

    private void OnCollisionEnter(Collision other)
    {
        if ((transform.position - StartPos).sqrMagnitude <= 1f) return;
        if (!Object.HasStateAuthority) return;
        Runner.Despawn(Object);
    }
}
