using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_SO", menuName = "Game/Weapon Settings")]
public class Weapon_SO : item_SO
{
    [SerializeField] public int weapon_index = 0;
    [SerializeField] public float fireRate = 1f;
    [SerializeField] public int magazineSize = 30;

    [SerializeField] public float upload_ammo_per_second = 1f;
    [SerializeField] public int maxAmmo = 300; 
    [SerializeField] public Bullet_SO bullet_SO;
    
    
}
