using Fusion;
using UnityEngine;

public class BulletNet : NetworkBehaviour
{
    [Networked] public Vector3 Direction { get; set; }
    [Networked] public float   Speed     { get; set; }
    [Networked] public float   Range     { get; set; }
    [Networked] public Vector3 StartPos  { get; set; }

    private io_bullet _bullet; // ваш компонент для локальной логики/данных

    private void Awake()
    {
        _bullet = GetComponent<io_bullet>();
    }

    // вызывать из onBeforeSpawned
    public void Setup(Bullet_SO so, Vector3 worldDir)
    {
        Direction = worldDir.sqrMagnitude > 0.0001f ? worldDir.normalized : Vector3.forward;
        Speed     = (so != null && so.speed  > 0f) ? so.speed  : 10f;
        Range     = (so != null && so.range  > 0f) ? so.range  : 50f;
        StartPos  = transform.position;

        // повернём визуал по направлению
        transform.forward = Direction;

        // если нужно — дайте знать io_bullet про SO (без движения)
        if (_bullet != null && so != null)
        {
            _bullet.bullet_SO = so;
            // здесь можно вызвать ваш InitializeBullet, но без перемещения/Time.deltaTime
        }
    }

    public override void FixedUpdateNetwork()
    {
        // только обладатель state authority двигает
        if (!Object.HasStateAuthority) return;

        transform.position += Direction * Speed * Runner.DeltaTime;

        if ((transform.position - StartPos).sqrMagnitude >= Range * Range)
            Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        Runner.Despawn(Object);
    }
}
