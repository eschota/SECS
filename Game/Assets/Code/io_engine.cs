using UnityEngine;

public class io_engine : io_base
{
    [Header("Engine Configuration")]
    [SerializeField] public Engine_SO engineSettings;
    
    [Header("Runtime State")]
    [SerializeField] private float currentPower = 0f;
    [SerializeField] private float engineStartTime = 0f;
    [SerializeField] private bool isEngineRunning = false;
    [SerializeField] private float lastInputTime = 0f;
    [SerializeField] private float powerRampTime = 0f; // Время нарастания мощности
    [SerializeField] private bool isPowerRamping = false; // Флаг нарастания мощности
    
    // Компоненты
    private ParticleSystem engineParticles;
    private AudioSource engineAudio;
    private ParticleSystem.MainModule particleMain;
    private ParticleSystem.EmissionModule particleEmission;

    // Переопределяем методы сериализации
    public override string GetCellType() => "io_engine";
    
    public override void SerializeToData(io_base_serialized data)
    {
        // Сначала сериализуем базовые данные
        base.SerializeToData(data);
        
        // Затем добавляем специфичные для двигателя данные
        if (data is io_engine_serialized engineData && engineSettings != null)
        {
            engineData.force_power = engineSettings.force_power;
            engineData.force_type = (int)engineSettings.force_type;
            engineData.force_vector_local = engineSettings.force_vector_local;
            engineData.fuel_per_second = engineSettings.fuel_per_second;
            engineData.electricity_per_second = engineSettings.electricity_per_second;
        }
    }
    
    public override void DeserializeFromData(io_base_serialized data)
    {
        // Сначала десериализуем базовые данные
        base.DeserializeFromData(data);
        
        // Затем добавляем специфичные для двигателя данные
        if (data is io_engine_serialized engineData && engineSettings != null)
        {
            // Обновляем настройки из SO
            engineSettings.force_power = engineData.force_power;
            engineSettings.force_type = (ForceMode)engineData.force_type;
            engineSettings.force_vector_local = engineData.force_vector_local;
            engineSettings.fuel_per_second = engineData.fuel_per_second;
            engineSettings.electricity_per_second = engineData.electricity_per_second;
        }
    }
    
    private void Awake()
    {
        InitializeEngine();
    }
    
    private void Start()
    {
        if (engineSettings == null)
        {
            Debug.LogError($"Engine {name} has no Engine_SO assigned!");
            return;
        }
        
        SetupParticleSystem();
        SetupAudioSource();
    }
    
    private void InitializeEngine()
    {
        // Инициализируем компоненты
        engineParticles = GetComponentInChildren<ParticleSystem>();
        engineAudio = GetComponent<AudioSource>();
        
        if (engineParticles != null)
        {
            particleMain = engineParticles.main;
            particleEmission = engineParticles.emission;
        }
    }
    
    private void SetupParticleSystem()
    {
        if (engineParticles == null || engineSettings == null) return;
        
        // Настраиваем основные параметры частиц
        particleMain.startLifetime = engineSettings.particleLifetime;
        particleMain.startColor = engineSettings.particleColor;
        
        // Настраиваем эмиссию
        particleEmission.rateOverTime = 0f; // Начинаем с выключенной эмиссии
        
        // Настраиваем форму эмиссии (конус в направлении силы)
        var shape = engineParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        
        // Ориентируем конус в направлении силы
        shape.rotation = Quaternion.LookRotation(engineSettings.force_vector_local).eulerAngles;
    }
    
    private void SetupAudioSource()
    {
        if (engineAudio == null || engineSettings == null) return;
        
        engineAudio.volume = engineSettings.engineVolume;
        engineAudio.pitch = engineSettings.enginePitch;
        engineAudio.loop = true;
        engineAudio.playOnAwake = false;
    }
    
    /// <summary>
    /// Обновляет состояние двигателя на основе входных данных
    /// </summary>
    public void UpdateEngineState(float inputPower, float deltaTime)
    {
        if (engineSettings == null) return;
        
        lastInputTime = Time.time;
        
        if (inputPower > 0.01f)
        {
            // Запускаем двигатель
            if (!isEngineRunning)
            {
                StartEngine();
            }
            
            // Начинаем нарастание мощности
            if (!isPowerRamping)
            {
                isPowerRamping = true;
                powerRampTime = 0f;
            }
            
            // Обновляем время нарастания мощности
            powerRampTime += deltaTime;
            
            // Вычисляем нормализованное время нарастания (0-1)
            float normalizedRampTime = Mathf.Clamp01(powerRampTime / engineSettings.force_max_timer);
            
            // Применяем кривую нарастания мощности
            float rampMultiplier = engineSettings.powerCurve.Evaluate(normalizedRampTime);
            
            // Вычисляем целевую мощность с учетом нарастания
            float targetPower = engineSettings.force_power * rampMultiplier * inputPower;
            
            // Плавно интерполируем к целевой мощности
            currentPower = Mathf.Lerp(currentPower, targetPower, deltaTime / engineSettings.engineStartupTime);
            
            // Обновляем визуализацию
            UpdateParticleSystem(inputPower * rampMultiplier);
            
            // Debug.Log($"Engine {name}: rampTime={powerRampTime:F2}s, normalizedRamp={normalizedRampTime:F2}, power={currentPower:F0}");
        }
        else
        {
            // Останавливаем нарастание мощности
            isPowerRamping = false;
            powerRampTime = 0f;
            
            // Останавливаем двигатель
            if (isEngineRunning)
            {
                StopEngine();
            }
            
            // Плавно снижаем мощность
            currentPower = Mathf.Lerp(currentPower, 0f, deltaTime / engineSettings.engineShutdownTime);
            
            // Обновляем визуализацию
            UpdateParticleSystem(0f);
        }
    }
    
    private void StartEngine()
    {
        isEngineRunning = true;
        engineStartTime = Time.time;
        
        // Запускаем звук
        if (engineAudio != null && engineSettings.engineStartSound != null)
        {
            engineAudio.clip = engineSettings.engineStartSound;
            engineAudio.Play();
        }
        
        // Запускаем частицы
        if (engineParticles != null)
        {
            engineParticles.Play();
        }
        
        // Debug.Log($"Engine {name} started");
    }
    
    private void StopEngine()
    {
        isEngineRunning = false;
        
        // Останавливаем звук
        if (engineAudio != null)
        {
            engineAudio.Stop();
            
            // Воспроизводим звук остановки
            if (engineSettings.engineStopSound != null)
            {
                AudioSource.PlayClipAtPoint(engineSettings.engineStopSound, transform.position, engineSettings.engineVolume);
            }
        }
        
        // Останавливаем частицы
        if (engineParticles != null)
        {
            engineParticles.Stop();
        }
        
        // Debug.Log($"Engine {name} stopped");
    }
    
    private void UpdateParticleSystem(float normalizedPower)
    {
        if (engineParticles == null || engineSettings == null) return;
        
        // Обновляем интенсивность эмиссии
        float emissionRate = engineSettings.GetCurrentEmissionRate(normalizedPower);
        particleEmission.rateOverTime = emissionRate;
        
        // Обновляем скорость частиц
        float particleSpeed = engineSettings.GetCurrentParticleSpeed(normalizedPower);
        particleMain.startSpeed = particleSpeed;
        
        // Обновляем размер частиц
        float particleSize = engineSettings.GetCurrentParticleSize(normalizedPower);
        particleMain.startSize = particleSize;
        
        // Обновляем цвет в зависимости от мощности
        Color currentColor = Color.Lerp(Color.gray, engineSettings.particleColor, normalizedPower);
        particleMain.startColor = currentColor;
    }
    
    /// <summary>
    /// Применяет силу к Rigidbody согласно настройкам двигателя
    /// </summary>
    public void ApplyForce(Rigidbody targetRigidbody)
    {
        if (targetRigidbody == null || engineSettings == null) return;
        
        // Преобразуем локальный вектор силы в мировые координаты
        Vector3 worldForce = transform.TransformDirection(engineSettings.force_vector_local) * currentPower;
        
        // Применяем силу с выбранным типом
        targetRigidbody.AddForce(worldForce, engineSettings.force_type);
        
        // Debug.Log($"Engine {name} applied force: {worldForce} with mode: {engineSettings.force_type}, power: {currentPower:F1}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (engineSettings == null) return;
        
        // Рисуем стрелку направления силы
        Gizmos.color = Color.red;
        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.TransformDirection(engineSettings.force_vector_local) * 2f;
        
        // Основная линия стрелки
        Gizmos.DrawLine(start, end);
        
        // Наконечник стрелки
        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        float arrowSize = 0.3f;
        Vector3 arrowTip = end;
        Vector3 arrowBase = end - direction * arrowSize;
        
        // Рисуем наконечник стрелки
        Gizmos.DrawLine(arrowTip, arrowBase + right * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase - right * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase + up * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowTip, arrowBase - up * arrowSize * 0.5f);
        
        // Рисуем основание наконечника
        Gizmos.DrawLine(arrowBase + right * arrowSize * 0.5f, arrowBase - right * arrowSize * 0.5f);
        Gizmos.DrawLine(arrowBase + up * arrowSize * 0.5f, arrowBase - up * arrowSize * 0.5f);
        
        // Рисуем информацию о мощности
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 textPos = transform.position + Vector3.up * 0.5f;
            #if UNITY_EDITOR
            string powerInfo = $"Power: {currentPower:F0}";
            if (isPowerRamping)
            {
                float normalizedRamp = Mathf.Clamp01(powerRampTime / engineSettings.force_max_timer);
                powerInfo += $"\nRamp: {normalizedRamp:F2} ({powerRampTime:F1}s)";
            }
            UnityEditor.Handles.Label(textPos, powerInfo);
            #endif
        }
    }
#endif
}
