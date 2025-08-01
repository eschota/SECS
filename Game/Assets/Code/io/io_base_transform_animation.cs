using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class io_base_transform_animation : MonoBehaviour
{
    
    [Header("Animation Settings")]
    [SerializeField] public io_animation_SO animationSettings;
    
    [Header("Direction Control")]
    [SerializeField] public int currentDirection = 0; // Текущее направление (0-3)
    [SerializeField] public float directionLerpDuration = 0.25f; // Длительность анимации вращения
    [SerializeField] public AnimationCurve directionLerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Кривая анимации вращения
    
    private float directionLerpTimer = 0f; // Таймер для анимации вращения
    private Quaternion startRotation; // Начальное вращение для лерпа
    private Quaternion targetDirectionRotation; // Целевое вращение для лерпа
    private bool isDirectionLerping = false; // Флаг активной анимации вращения
    
    // Свойства для обратной совместимости
    public io_base.io_type animation_type_current => animationSettings != null ? animationSettings.animation_type_current : io_base.io_type.off;
    public AnimationCurve curve => animationSettings != null ? animationSettings.curve : AnimationCurve.Linear(0, 0, 1, 1);
    public Vector3 targetScale => animationSettings != null ? animationSettings.targetScale : Vector3.one;
    public Vector3 targetPosition => animationSettings != null ? animationSettings.targetPosition : Vector3.zero;
    public Quaternion targetRotation => animationSettings != null ? animationSettings.targetRotation : Quaternion.identity;
    public Color targetColor => animationSettings != null ? animationSettings.targetColor : Color.white;
    public Color32 targetEmissionColor => animationSettings != null ? animationSettings.targetEmissionColor : Color.black;
     
    
    // Метод для изменения направления
    public void ChangeDirection(int directionDelta)
    {
        Debug.Log($"ChangeDirection вызван с параметром: {directionDelta}");
        
        if (isDirectionLerping) 
        {
            Debug.Log("Анимация уже активна, игнорирую запрос");
            return; // Не позволяем изменять направление во время анимации
        }
        
        int newDirection = (currentDirection + directionDelta + 4) % 4; // Обеспечиваем циклическое изменение 0-3
        Debug.Log($"Текущее направление: {currentDirection}, новое направление: {newDirection}");
        
        if (newDirection == currentDirection) 
        {
            Debug.Log("Направление не изменилось, игнорирую");
            return; // Если направление не изменилось
        }
        
        // Начинаем анимацию вращения
        startRotation = transform.rotation;
        targetDirectionRotation = Quaternion.Euler(0, newDirection * 90f, 0); // 90 градусов на направление
        directionLerpTimer = 0f;
        isDirectionLerping = true;
        currentDirection = newDirection;
        
        // Синхронизируем с полем direction в io_base
        var ioBase = GetComponent<io_base>();
        if (ioBase != null)
        {
            ioBase.direction = currentDirection;
        }
        
        Debug.Log($"Анимация вращения начата. Начальный угол: {startRotation.eulerAngles.y}, целевой угол: {targetDirectionRotation.eulerAngles.y}");
    }
    
    // Метод для обновления анимации вращения
    public void UpdateDirectionLerp()
    {
        if (!isDirectionLerping) return;
        
        directionLerpTimer += Time.deltaTime;
        float progress = directionLerpTimer / directionLerpDuration;
        
        if (progress >= 1f)
        {
            // Анимация завершена
            transform.rotation = targetDirectionRotation;
            isDirectionLerping = false;
            
            // Синхронизируем с полем direction в io_base при завершении
            var ioBase = GetComponent<io_base>();
            if (ioBase != null)
            {
                ioBase.direction = currentDirection;
            }
            
            Debug.Log($"Анимация вращения завершена для объекта: {gameObject.name}");
        }
        else
        {
            // Продолжаем анимацию
            float curveValue = directionLerpCurve.Evaluate(progress);
            transform.rotation = Quaternion.Slerp(startRotation, targetDirectionRotation, curveValue);
            
            // Логируем каждые 10 кадров для отладки
            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"Анимация вращения: {progress * 100:F1}% завершена для объекта: {gameObject.name}");
            }
        }
    }
    
    // Метод для получения текущего направления в градусах
    public float GetCurrentDirectionAngle()
    {
        return currentDirection * 90f;
    }
    
    // Метод для проверки, активна ли анимация вращения
    public bool IsDirectionLerping()
    {
        return isDirectionLerping;
    }
    
    // Статический метод для получения или создания компонента
    public static io_base_transform_animation GetOrAddComponent(GameObject obj)
    {
        var component = obj.GetComponent<io_base_transform_animation>();
        if (component == null)
        {
            component = obj.AddComponent<io_base_transform_animation>();
            Debug.Log($"Добавлен компонент io_base_transform_animation на объект: {obj.name}");
        }
        return component;
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // Автоматически загружаем SO по типу анимации
        if (animationSettings == null)
        {
            LoadSOByType();
        }
    }
    
    private void LoadSOByType()
    {
        // Пытаемся определить тип по имени компонента или GameObject
        string componentName = this.name;
        string gameObjectName = gameObject.name;
        
        // Ищем тип в имени
        foreach (io_base.io_type type in System.Enum.GetValues(typeof(io_base.io_type)))
        {
            string typeName = type.ToString();
            if (componentName.Contains(typeName) || gameObjectName.Contains(typeName))
            {
                string soName = $"Animation_{typeName}_SO";
                io_animation_SO so = Resources.Load<io_animation_SO>($"Settings/{soName}");
                if (so != null)
                {
                    animationSettings = so;
                    Debug.Log($"Автоматически загружен SO для {typeName} в {gameObject.name}");
                    break;
                }
            }
        }
    }
#endif
}
