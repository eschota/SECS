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
    
    [Header("Bullet State")]
    [SerializeField] private Vector3 moveDirection;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float range;
    [SerializeField] private float distanceTraveled;
    
    private Vector3 startPosition;
    
    void Start()
    {
        startPosition = transform.position;
    }
    
    public void InitializeBullet(Bullet_SO bulletSettings, Vector3 direction)
    {
        if (bulletSettings == null) return;
        
        bullet_SO = bulletSettings;
        type_bullet = (Type_bullet)bulletSettings.type_bullet;
        damage = bulletSettings.damage;
        moveSpeed = bulletSettings.speed;
        range = bulletSettings.range;
        moveDirection = direction.normalized;
        distanceTraveled = 0f;
        
        Debug.Log($"io_bullet: Инициализирована пуля {name}, Bullet_SO.speed: {bulletSettings.speed}, moveSpeed: {moveSpeed}, направление: {moveDirection}, позиция: {transform.position}");
        
        // Проверяем что скорость не нулевая
        if (moveSpeed <= 0)
        {
            Debug.LogError($"io_bullet: {name} - скорость равна {moveSpeed}! Установим скорость по умолчанию 10");
            moveSpeed = 10f;
        }
        
        // Проверяем что направление не нулевое
        if (moveDirection.magnitude < 0.1f)
        {
            Debug.LogError($"io_bullet: {name} - направление нулевое! Установим направление вперед");
            moveDirection = Vector3.forward;
        }
    }
    
    void Update()
    {
        if (bullet_SO == null) 
        {
            Debug.LogWarning($"io_bullet: {name} - bullet_SO равен null!");
            return;
        }
        
        // Проверяем что скорость и направление правильные
        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"io_bullet: {name} - скорость равна {moveSpeed}, не двигаемся");
            return;
        }
        
        if (moveDirection.magnitude < 0.1f)
        {
            Debug.LogWarning($"io_bullet: {name} - направление нулевое, не двигаемся");
            return;
        }
        
        // Двигаем пулю локально
        Vector3 oldPosition = transform.position;
        Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        transform.position = newPosition;
        
        // Проверяем дистанцию
        distanceTraveled += moveSpeed * Time.deltaTime;
        
        // Отладка движения пули каждые 30 кадров
        if (Time.frameCount % 30 == 0)
        {
            Vector3 movement = transform.position - oldPosition;
            Debug.Log($"io_bullet: {name} - старая позиция: {oldPosition}, новая позиция: {transform.position}, движение: {movement}, направление: {moveDirection}, скорость: {moveSpeed}, дистанция: {distanceTraveled:F1}/{range:F1}");
        }
        
        if (distanceTraveled >= range)
        {
            Debug.Log($"io_bullet: {name} достигла максимальной дистанции, уничтожаем");
            DestroyBullet();
        }
    }
    
    private void DestroyBullet()
    {
        // Просто уничтожаем пулю локально
        Destroy(gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Здесь можно добавить логику попадания
        // Пока просто уничтожаем пулю при столкновении
        DestroyBullet();
    }
}
