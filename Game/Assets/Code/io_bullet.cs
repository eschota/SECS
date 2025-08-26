using UnityEngine;

public class io_bullet : io_base
{
    [Header("Bullet Configuration")]
    [SerializeField] public Bullet_SO bullet_SO;

    public enum Type_bullet
    {
        bullet,
        rocket,
        laser,
        mine
    }
    [SerializeField] public Type_bullet type_bullet;  
}
