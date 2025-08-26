using UnityEngine;

[CreateAssetMenu(fileName = "Bullet_SO", menuName = "Game/Bullet Settings")]
public class Bullet_SO : item_SO
{
   public enum Type_bullet
   {
    bullet,
    rocket,
    laser,
    mine
   }
   [SerializeField] public Type_bullet type_bullet;
   
   [SerializeField] public float damage;
   [SerializeField] public float speed;
   [SerializeField] public float range; 
    
    
}
