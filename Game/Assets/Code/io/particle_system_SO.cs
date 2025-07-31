using UnityEngine;

[CreateAssetMenu(fileName = "ParticleSystem_SO", menuName = "IO/Particle System Settings")]
public class particle_system_SO : ScriptableObject
{
    [Header("Particle System Type")]
    public io_base.io_base_cell_type cellType;
    
    [Header("State Multipliers")]
    [Range(0f, 1f)] public float offMultiplier = 0f;
    [Range(0f, 1f)] public float onMultiplier = 1f;
    [Range(0f, 1f)] public float toggleMultiplier = 0.5f;
    [Range(0f, 1f)] public float mouseOverMultiplier = 0.7f;
    [Range(0f, 1f)] public float selectedMultiplier = 0.8f;
    [Range(0f, 1f)] public float clickedMultiplier = 1f;
    [Range(0f, 1f)] public float deselectedMultiplier = 0.3f;
    [Range(0f, 1f)] public float dragMultiplier = 0.6f;
    [Range(0f, 1f)] public float floorUpMultiplier = 0.9f;
    [Range(0f, 1f)] public float floorDownMultiplier = 0.9f;
    [Range(0f, 1f)] public float toRemoveMultiplier = 0f;
    
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 5f; // Скорость интерполяции между состояниями
    public bool useSmoothInterpolation = true; // Использовать плавную интерполяцию
    
    [Header("Particle System Settings")]
    public bool playOnStart = false;
    public bool loop = true;
    public Vector2 startLifetime = new Vector2(2f, 2f); // Min, Max
    public Vector2 startSpeed = new Vector2(5f, 5f); // Min, Max
    public Vector2 startSize = new Vector2(1f, 1f); // Min, Max
    public int maxParticles = 100;
    public bool useWorldSpace = false;
    public bool playOnAwake = true;
    public bool autoSimulation = true;
    
    [Header("Emission Settings")]
    public Vector2 rateOverTime = new Vector2(10f, 10f); // Min, Max
    public Vector2 rateOverDistance = new Vector2(0f, 0f); // Min, Max
    public bool burstEnabled = false;
    public int burstCount = 1;
    public float burstTime = 0f;
    public Vector2 burstInterval = new Vector2(0.1f, 0.1f); // Min, Max
    
    [Header("Shape Settings")]
    public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Sphere;
    public float shapeRadius = 1f;
    public Vector3 shapeScale = Vector3.one;
    public bool useColliderMeshAsShape = false; // Новое поле
    public ParticleSystemMeshShapeType meshShapeType = ParticleSystemMeshShapeType.Vertex;
    public bool useMeshColors = true;
    
    [Header("Color Settings")]
    public Color startColor = Color.white;
    public Color endColor = Color.white;
    public Gradient colorOverLifetime;
    public bool useGradient = false;
    public Vector2 colorIntensity = new Vector2(1f, 1f); // Min, Max
    
    [Header("Size Settings")]
    public Vector2 sizeOverLifetime = new Vector2(1f, 1f); // Min, Max
    public AnimationCurve sizeOverLifetimeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public bool sizeOverLifetimeEnabled = true;
    public Vector2 sizeBySpeed = new Vector2(0f, 0f); // Min, Max
    public bool sizeBySpeedEnabled = false;
    
    [Header("Velocity Settings")]
    public Vector3 startVelocity = Vector3.zero;
    public Vector2 velocityOverLifetime = new Vector2(0f, 0f); // Min, Max
    public bool velocityOverLifetimeEnabled = false;
    public Vector2 velocityBySpeed = new Vector2(0f, 0f); // Min, Max
    public bool velocityBySpeedEnabled = false;
    public Vector3 velocityInheritance = Vector3.one;
    
    [Header("Force Settings")]
    public Vector2 gravityModifier = new Vector2(0f, 0f); // Min, Max
    public Vector2 drag = new Vector2(0f, 0f); // Min, Max
    public Vector3 externalForces = Vector3.zero;
    public bool useGravity = true;
    
    [Header("Collision Settings")]
    public bool enableCollision = false;
    public LayerMask collisionLayers = -1;
    
    [Header("Renderer Settings")]
    public Material particleMaterial;
    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
    public float sortingFudge = 0f;
    public Vector2 lengthScale = new Vector2(1f, 1f); // Min, Max
    public Vector2 speedScale = new Vector2(1f, 1f); // Min, Max
    public bool useMesh = false;
    public Mesh particleMesh;
    
    [Header("Trails Settings")]
    public bool trailsEnabled = false;
    public Material trailMaterial;
    public Vector2 trailLifetime = new Vector2(1f, 1f); // Min, Max
    public Vector2 trailWidth = new Vector2(0.1f, 0.1f); // Min, Max
    public AnimationCurve trailWidthOverLifetime = AnimationCurve.Linear(0, 1, 1, 0);
    public bool trailWidthOverLifetimeEnabled = true;
    public Vector2 trailMinVertexDistance = new Vector2(0.1f, 0.1f); // Min, Max
    public bool trailDieWithParticles = true;
    public bool trailRibbon = false;
    public Vector2 trailRatio = new Vector2(1f, 1f); // Min, Max
    public bool trailModeWorldSpace = false;
    public bool trailGenerateLightingData = false;
    
    [Header("Runtime Control")]
    public bool autoPlay = true;
    public bool pauseOnDisable = true;
    public float emissionRateMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float sizeMultiplier = 1f;
    
    // Методы для получения множителей по состоянию
    public float GetStateMultiplier(io_base.io_type stateType)
    {
        switch (stateType)
        {
            case io_base.io_type.off: return offMultiplier;
            case io_base.io_type.on: return onMultiplier;
            case io_base.io_type.toggle: return toggleMultiplier;
            case io_base.io_type.mouseOver: return mouseOverMultiplier;
            case io_base.io_type.selected: return selectedMultiplier;
            case io_base.io_type.clicked: return clickedMultiplier;
            case io_base.io_type.deselected: return deselectedMultiplier;
            case io_base.io_type.drag: return dragMultiplier;
            case io_base.io_type.floor_up: return floorUpMultiplier;
            case io_base.io_type.floor_down: return floorDownMultiplier;
            case io_base.io_type.ToRemove: return toRemoveMultiplier;
            default: return 0f;
        }
    }
    
    public float GetInterpolatedMultiplier(float currentMultiplier, float targetMultiplier, float deltaTime)
    {
        if (!useSmoothInterpolation)
            return targetMultiplier;
            
        return Mathf.Lerp(currentMultiplier, targetMultiplier, interpolationSpeed * deltaTime);
    }
    
    // Методы для управления в реальном времени
    public void PlayParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Play();
            ApplyRuntimeSettings(ps);
        }
    }
    
    public void StopParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Stop();
        }
    }
    
    public void PauseParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Pause();
        }
    }
    
    public void SetEmissionRate(ParticleSystem ps, Vector2 rate)
    {
        if (ps != null)
        {
            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate.x * emissionRateMultiplier, rate.y * emissionRateMultiplier);
        }
    }
    
    public void SetSpeed(ParticleSystem ps, Vector2 speed)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * speedMultiplier, speed.y * speedMultiplier);
        }
    }
    
    public void SetSize(ParticleSystem ps, Vector2 size)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x * sizeMultiplier, size.y * sizeMultiplier);
        }
    }
    
    public void SetLifetime(ParticleSystem ps, Vector2 lifetime)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        }
    }
    
    public void SetTrails(ParticleSystem ps)
    {
        if (ps != null && trailsEnabled)
        {
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = new ParticleSystem.MinMaxCurve(trailLifetime.x, trailLifetime.y);
            trails.widthOverTrail = trailWidthOverLifetimeEnabled
                ? new ParticleSystem.MinMaxCurve(1f, trailWidthOverLifetime)
                : new ParticleSystem.MinMaxCurve(trailWidth.x, trailWidth.y);
            trails.minVertexDistance = trailMinVertexDistance.x;
            trails.dieWithParticles = trailDieWithParticles;
            trails.ratio = trailRatio.x;
            trails.generateLightingData = trailGenerateLightingData;
            
            if (trailMaterial != null)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.trailMaterial = trailMaterial;
                }
            }
        }
        else if (ps != null)
        {
            var trails = ps.trails;
            trails.enabled = false;
        }
    }
    
    public void SetColor(ParticleSystem ps, Color color)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
    
    public void SetMaxParticles(ParticleSystem ps, int maxParticles)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.maxParticles = maxParticles;
        }
    }
    
    public void ApplyRuntimeSettings(ParticleSystem ps)
    {
        if (ps == null) return;
        
        // Применяем множители
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rateOverTime.x * emissionRateMultiplier, rateOverTime.y * emissionRateMultiplier);
        
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed.x * speedMultiplier, startSpeed.y * speedMultiplier);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize.x * sizeMultiplier, startSize.y * sizeMultiplier);
        main.startLifetime = new ParticleSystem.MinMaxCurve(startLifetime.x, startLifetime.y);
        
        // Применяем трейлы
        SetTrails(ps);
        // --- Новый блок для shapeType = Mesh ---
        var shape = ps.shape;
        shape.shapeType = shapeType;
        shape.radius = shapeRadius;
        shape.scale = shapeScale;
        if (useColliderMeshAsShape && shapeType == ParticleSystemShapeType.Mesh)
        {
            var io = ps.GetComponent<io_base>();
            if (io != null && io.target_collider is MeshCollider meshCol && meshCol.sharedMesh != null)
            {
                shape.mesh = meshCol.sharedMesh;
                shape.meshShapeType = meshShapeType;
                shape.useMeshColors = useMeshColors;
            }
        }
        else if (shapeType == ParticleSystemShapeType.Mesh)
        {
            shape.meshShapeType = meshShapeType;
            shape.useMeshColors = useMeshColors;
        }
    }
} 