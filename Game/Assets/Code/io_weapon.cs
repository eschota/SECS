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
   [SerializeField] private bool canFire = true;

   private NetworkRunner runner;

   void Start()
   {
       // Инициализация патронов при старте
       InitializeAmmo();
   }

   public void InitializeWeapon(NetworkRunner networkRunner)
   {
       runner = networkRunner;
       
       // Инициализируем патроны при инициализации оружия
       InitializeAmmo();
   }
   
   private void InitializeAmmo()
   {
       if (weapon_SO != null)
       {
           // Используем maxAmmo для текущих патронов (magazineSize может быть меньше)
           currentAmmo = weapon_SO.maxAmmo;
           maxAmmo = weapon_SO.maxAmmo;
           Debug.Log($"io_weapon: {name} инициализированы патроны: {currentAmmo}/{maxAmmo} (magazineSize: {weapon_SO.magazineSize})");
           
           // Проверяем bullet_SO
           if (weapon_SO.bullet_SO != null)
           {
               Debug.Log($"io_weapon: {name} - bullet_SO: {weapon_SO.bullet_SO.name}, prefab: {(weapon_SO.bullet_SO.prefab != null ? weapon_SO.bullet_SO.prefab.name : "NULL")}");
           }
           else
           {
               Debug.LogError($"io_weapon: {name} - weapon_SO.bullet_SO равен null!");
           }
       }
       else
       {
           Debug.LogError($"io_weapon: {name} не имеет weapon_SO!");
       }
   }

   public bool CanFire()
   {
       if (weapon_SO == null || weapon_SO.bullet_SO == null) return false;
       
       // Проверяем патроны
       if (currentAmmo <= 0) return false;
       
       // Проверяем скорострельность
       if (Time.time - lastFireTime < 1f / weapon_SO.fireRate) return false;
       
       return canFire;
   }

   public void Fire(NetworkRunner runner, Vector3 direction)
   {
       Debug.Log($"io_weapon: Fire() вызван для {name}, направление: {direction}");
       
       if (!CanFire()) 
       {
           Debug.Log($"io_weapon: {name} не может стрелять!");
           return;
       }

       // Обновляем время последнего выстрела
       lastFireTime = Time.time;
       
       // Уменьшаем патроны
       currentAmmo = Mathf.Max(0, currentAmmo - 1);
       Debug.Log($"io_weapon: {name} выстрелил, осталось патронов: {currentAmmo}/{maxAmmo}");

       // Спавним пули для каждого пивота
       Debug.Log($"io_weapon: Спавним пули, количество пивотов: {bullet_pivots?.Length ?? 0}");
       
       if (bullet_pivots != null && bullet_pivots.Length > 0)
       {
           foreach (Transform pivot in bullet_pivots)
           {
               if (pivot != null)
               {
                   // Используем направление bullet_pivot (его forward в мировых координатах)
                   Vector3 bulletDirection = pivot.forward;
                   Debug.Log($"io_weapon: Спавним пулю в позиции {pivot.position}, направление: {bulletDirection}, rotation: {pivot.rotation.eulerAngles}");
                   
                   // Проверяем что направление не нулевое
                   if (bulletDirection.magnitude > 0.1f)
                   {
                       SpawnBullet(runner, pivot.position, pivot.rotation, pivot);

                   }
                   else
                   {
                       Debug.LogError($"io_weapon: {name} - bullet_pivot {pivot.name} имеет нулевое направление!");
                   }
               }
           }
       }
   }

  private void SpawnBullet(NetworkRunner runner, Vector3 position, Quaternion rotation, Transform pivot)
{
    if (weapon_SO?.bullet_SO == null || weapon_SO.bullet_SO.prefab == null)
    {
        Debug.LogError($"[{name}] bullet_SO не задан");
        return;
    }

    // мировое направление по локальной оси Z пивота
    Vector3 worldDir = pivot.TransformDirection(Vector3.forward);

    runner.Spawn(
        weapon_SO.bullet_SO.prefab.gameObject,
        position,
        rotation,
        inputAuthority: null, // или ваш владелец, если нужно
        onBeforeSpawned: (r, obj) =>
        {
            var net = obj.GetComponent<BulletNet>();
            net.Setup(weapon_SO.bullet_SO, worldDir);
        });
}

   public void Reload()
   {
       currentAmmo = maxAmmo;
   }

   public float GetAmmoPercentage()
   {
       return maxAmmo > 0 ? currentAmmo / maxAmmo : 0f;
   }

   public float GetCurrentAmmo()
   {
       return currentAmmo;
   }

   public float GetMaxAmmo()
   {
       return maxAmmo;
   }

    void OnDrawGizmos()
    {
        if(bullet_pivots == null)
        {
            return;
        }
        foreach(Transform pivot in bullet_pivots)
        {
            Gizmos.DrawWireSphere(pivot.position, 0.1f);
        }
    }
}
