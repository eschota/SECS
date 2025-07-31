using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class io_PS_controller : MonoBehaviour
{
    [SerializeField] io_base io;
    [SerializeField] public particle_system_SO ps_SO;
    [SerializeField] public ParticleSystem particleSystem;
    
    // Переменные для интерполяции состояний
    private float currentMultiplier = 0f;
    private float targetMultiplier = 0f;
    private io_base.io_type lastState = io_base.io_type.off;
    
    void OnValidate()
    {
        if(particleSystem==null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }
        if(io==null)
        {
            io = GetComponent<io_base>();
        }
        
        // Загружаем SO если его нет или если изменился тип клетки
        if (io != null && (ps_SO == null || ps_SO.cellType != io.cell_type))
        {
            LoadParticleSystemSO();
        }
    }
    
    private void LoadParticleSystemSO()
    {
        if (io == null) return;
        
        // Определяем тип клетки
        io_base.io_base_cell_type cellType = io.cell_type;
        
        // Загружаем ВСЕ SO файлы из папки
        particle_system_SO[] allSOs = Resources.LoadAll<particle_system_SO>("settings/particle_systems_SO");
        
        if (allSOs.Length == 0)
        {
            Debug.LogWarning($"Не найдено SO файлов particle system в папке settings/particle_systems_SO");
            return;
        }
        
        // Ищем соответствующий SO по типу клетки
        particle_system_SO matchingSO = null;
        foreach (var so in allSOs)
        {
            if (so != null && so.cellType == cellType)
            {
                matchingSO = so;
                break;
            }
        }
        
        if (matchingSO != null)
        {
            ps_SO = matchingSO;
            Debug.Log($"Автоматически загружен SO для particle system типа {cellType}: {matchingSO.name}");
            
            // Применяем настройки к particle system если он есть
            if (particleSystem != null)
            {
                ApplyParticleSystemSettings();
            }
        }
        else
        {
            Debug.LogWarning($"Не найден SO файл для типа клетки {cellType} среди {allSOs.Length} загруженных файлов");
        }
    }
    
    private void ApplyParticleSystemSettings()
    {
        if (ps_SO == null || particleSystem == null) return;
        
        var main = particleSystem.main;
        main.playOnAwake = ps_SO.playOnAwake;
        main.loop = ps_SO.loop;
        main.startLifetime = new ParticleSystem.MinMaxCurve(ps_SO.startLifetime.x, ps_SO.startLifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(ps_SO.startSpeed.x, ps_SO.startSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(ps_SO.startSize.x, ps_SO.startSize.y);
        main.maxParticles = ps_SO.maxParticles;
        main.startColor = ps_SO.startColor;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(ps_SO.gravityModifier.x, ps_SO.gravityModifier.y);
        main.simulationSpace = ps_SO.useWorldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(ps_SO.rateOverTime.x, ps_SO.rateOverTime.y);
        emission.rateOverDistance = new ParticleSystem.MinMaxCurve(ps_SO.rateOverDistance.x, ps_SO.rateOverDistance.y);
        
        // Настройка burst если включен
        if (ps_SO.burstEnabled)
        {
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(ps_SO.burstTime, new ParticleSystem.MinMaxCurve(ps_SO.burstCount), 
                    (int)ps_SO.burstInterval.x, 1)
            });
        }
        
        var shape = particleSystem.shape;
        shape.shapeType = ps_SO.shapeType;
        shape.radius = ps_SO.shapeRadius;
        shape.scale = ps_SO.shapeScale;
        
        // --- Новый блок для shapeType = Mesh ---
        if (ps_SO.useColliderMeshAsShape && ps_SO.shapeType == ParticleSystemShapeType.Mesh)
        {
            if (io != null && io.target_collider is MeshCollider meshCol && meshCol.sharedMesh != null)
            {
                shape.mesh = meshCol.sharedMesh;
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.meshShapeType = ps_SO.meshShapeType;
                shape.useMeshColors = ps_SO.useMeshColors;
                Debug.Log($"Установлен mesh из collider для particle system: {meshCol.sharedMesh.name}");
            }
            else if (io != null && io.target_collider != null)
            {
                Debug.LogWarning($"Collider не является MeshCollider или не имеет sharedMesh. Тип collider: {io.target_collider.GetType()}");
            }
        }
        else if (ps_SO.shapeType == ParticleSystemShapeType.Mesh)
        {
            // Если shapeType = Mesh, но не используем collider mesh, применяем настройки из SO
            shape.meshShapeType = ps_SO.meshShapeType;
            shape.useMeshColors = ps_SO.useMeshColors;
        }
        
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = ps_SO.sizeOverLifetimeEnabled;
        if (ps_SO.sizeOverLifetimeEnabled)
        {
            if (ps_SO.sizeOverLifetimeCurve != null && ps_SO.sizeOverLifetimeCurve.length > 1)
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, ps_SO.sizeOverLifetimeCurve);
            else
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(ps_SO.sizeOverLifetime.x, ps_SO.sizeOverLifetime.y);
        }
        
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = ps_SO.velocityOverLifetimeEnabled;
        if (ps_SO.velocityOverLifetimeEnabled)
        {
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(ps_SO.velocityOverLifetime.x, ps_SO.velocityOverLifetime.y);
        }
        
        var collision = particleSystem.collision;
        collision.enabled = ps_SO.enableCollision;
        collision.collidesWith = ps_SO.collisionLayers;
        
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ps_SO.renderMode;
            renderer.sortingFudge = ps_SO.sortingFudge;
            
            if (ps_SO.useMesh && ps_SO.particleMesh != null)
            {
                renderer.mesh = ps_SO.particleMesh;
            }
            
            if (ps_SO.particleMaterial != null)
            {
                renderer.material = ps_SO.particleMaterial;
            }
        }
        
        // Применяем трейлы
        ps_SO.SetTrails(particleSystem);
    }
    
    void Start()
    {
        // Убеждаемся что все ссылки установлены
        if (io == null)
            io = GetComponent<io_base>();
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
            
        // Инициализируем множители
        if (io != null && ps_SO != null)
        {
            var initialState = io.stack.Count > 0 ? io.stack.Last() : io_base.io_type.off;
            currentMultiplier = ps_SO.GetStateMultiplier(initialState);
            targetMultiplier = currentMultiplier;
            lastState = initialState;
        }
            
        // Применяем настройки при старте
        if (ps_SO != null && particleSystem != null)
        {
            ApplyParticleSystemSettings();
            
            // Применяем начальный множитель
            ApplyStateMultiplier(currentMultiplier);
            
            // Автоматически запускаем если включено и множитель больше 0
            if (ps_SO.autoPlay && currentMultiplier > 0f)
            {
                ps_SO.PlayParticleSystem(particleSystem);
            }
        }
    }

    void Update()
    {
        // Обновляем настройки в реальном времени
        if (ps_SO != null && particleSystem != null)
        {
            // Обновляем поведение частиц по состоянию
            UpdateParticleSystem();
            
            // Принудительно обновляем настройки если множитель изменился
            if (Mathf.Abs(currentMultiplier - targetMultiplier) > 0.01f)
            {
                ApplyParticleSystemSettings();
            }
        }
    }
    
    private void UpdateParticleSystem()
    {
        // Проверяем состояние io_base для управления частицами
        if (io != null && ps_SO != null)
        {
            var currentState = io.stack.Count > 0 ? io.stack.Last() : io_base.io_type.off;
            
            // Получаем целевой множитель для текущего состояния
            targetMultiplier = ps_SO.GetStateMultiplier(currentState);
            
            // Интерполируем множитель если состояние изменилось или используем плавную интерполяцию
            if (currentState != lastState)
            {
                // При смене состояния применяем множитель сразу
                currentMultiplier = targetMultiplier;
            }
            else if (ps_SO.useSmoothInterpolation)
            {
                currentMultiplier = ps_SO.GetInterpolatedMultiplier(currentMultiplier, targetMultiplier, Time.deltaTime);
            }
            
            // Применяем множитель к particle system
            ApplyStateMultiplier(currentMultiplier);
            
            lastState = currentState;
        }
    }
    
    private void ApplyStateMultiplier(float multiplier)
    {
        if (particleSystem == null || ps_SO == null) return;
        
        // Если множитель равен 0, останавливаем систему и очищаем частицы
        if (multiplier <= 0f)
        {
            if (particleSystem.isPlaying)
            {
                particleSystem.Stop();
                particleSystem.Clear(); // Принудительно очищаем все частицы
            }
            return;
        }
        
        // Если множитель больше 0 и система остановлена, запускаем её
        if (multiplier > 0f && !particleSystem.isPlaying)
        {
            particleSystem.Play();
        }
        
        // Применяем множитель к основным параметрам
        var main = particleSystem.main;
        var emission = particleSystem.emission;
        
        // Интерполируем время жизни частиц от 0 до значения из SO
        float lifetimeMultiplier = Mathf.Lerp(0f, 1f, multiplier);
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            ps_SO.startLifetime.x * lifetimeMultiplier, 
            ps_SO.startLifetime.y * lifetimeMultiplier
        );
        
        // Применяем множитель к скорости эмиссии
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(
            ps_SO.rateOverTime.x * multiplier, 
            ps_SO.rateOverTime.y * multiplier
        );
        
        // Применяем множитель к скорости частиц
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            ps_SO.startSpeed.x * multiplier, 
            ps_SO.startSpeed.y * multiplier
        );
        
        // Применяем множитель к размеру частиц
        main.startSize = new ParticleSystem.MinMaxCurve(
            ps_SO.startSize.x * multiplier, 
            ps_SO.startSize.y * multiplier
        );
        
        // Применяем множитель к максимальному количеству частиц
        main.maxParticles = Mathf.RoundToInt(ps_SO.maxParticles * multiplier);
        
        // Если множитель очень маленький, принудительно уменьшаем количество частиц
        if (multiplier < 0.1f)
        {
            main.maxParticles = 1;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0.1f);
        }
    }
    
    // Публичные методы для внешнего управления
    public void PlayParticles()
    {
        if (ps_SO != null && particleSystem != null)
        {
            ps_SO.PlayParticleSystem(particleSystem);
        }
    }
    
    public void StopParticles()
    {
        if (ps_SO != null && particleSystem != null)
        {
            ps_SO.StopParticleSystem(particleSystem);
        }
    }
    
    public void PauseParticles()
    {
        if (ps_SO != null && particleSystem != null)
        {
            ps_SO.PauseParticleSystem(particleSystem);
        }
    }
    
    public void SetParticleColor(Color color)
    {
        if (ps_SO != null && particleSystem != null)
        {
            ps_SO.SetColor(particleSystem, color);
        }
    }
    
    public void SetParticleIntensity(float intensity)
    {
        if (ps_SO != null && particleSystem != null)
        {
            ps_SO.SetEmissionRate(particleSystem, new Vector2(ps_SO.rateOverTime.x * intensity, ps_SO.rateOverTime.y * intensity));
        }
    }
    
    // Новые методы для управления множителями состояний
    public void SetStateMultiplier(io_base.io_type stateType, float multiplier)
    {
        if (ps_SO != null)
        {
            switch (stateType)
            {
                case io_base.io_type.off: ps_SO.offMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.on: ps_SO.onMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.toggle: ps_SO.toggleMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.mouseOver: ps_SO.mouseOverMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.selected: ps_SO.selectedMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.clicked: ps_SO.clickedMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.deselected: ps_SO.deselectedMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.drag: ps_SO.dragMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.floor_up: ps_SO.floorUpMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.floor_down: ps_SO.floorDownMultiplier = Mathf.Clamp01(multiplier); break;
                case io_base.io_type.ToRemove: ps_SO.toRemoveMultiplier = Mathf.Clamp01(multiplier); break;
            }
        }
    }
    
    public float GetCurrentMultiplier()
    {
        return currentMultiplier;
    }
    
    public float GetTargetMultiplier()
    {
        return targetMultiplier;
    }
    
    public io_base.io_type GetCurrentState()
    {
        return lastState;
    }
    
    public void ForceUpdateMultiplier()
    {
        if (io != null && ps_SO != null)
        {
            var currentState = io.stack.Count > 0 ? io.stack.Last() : io_base.io_type.off;
            targetMultiplier = ps_SO.GetStateMultiplier(currentState);
            currentMultiplier = targetMultiplier;
            lastState = currentState;
            ApplyStateMultiplier(currentMultiplier);
        }
    }
    
    public void ResetParticleSystem()
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
            particleSystem.Clear();
            currentMultiplier = 0f;
            targetMultiplier = 0f;
        }
    }
    
    public void SetMultiplierImmediate(float multiplier)
    {
        currentMultiplier = Mathf.Clamp01(multiplier);
        targetMultiplier = currentMultiplier;
        ApplyStateMultiplier(currentMultiplier);
    }
}
