using UnityEngine;

[CreateAssetMenu(fileName = "Engine_SO", menuName = "Game/Engine Settings")]
public class Engine_SO : ScriptableObject
{
    [Header("Engine Physics")]
    [SerializeField] public float force_power = 1000f;
    [SerializeField] public ForceMode force_type = ForceMode.Force;
    [SerializeField] public Vector3 force_vector_local = Vector3.forward;
    [SerializeField] public float fuel_per_second = 10f;
    [SerializeField] public float electricity_per_second = 5f;
    
    [Header("Particle System Settings")]
    [SerializeField] public GameObject particlePrefab;
    [SerializeField] public float minEmissionRate = 10f;
    [SerializeField] public float maxEmissionRate = 100f;
    [SerializeField] public float minStartSpeed = 2f;
    [SerializeField] public float maxStartSpeed = 8f;
    [SerializeField] public float minStartSize = 0.1f;
    [SerializeField] public float maxStartSize = 0.5f;
    [SerializeField] public Color particleColor = Color.orange;
    [SerializeField] public float particleLifetime = 2f;
    [SerializeField] public float particleFadeInTime = 0.1f;
    [SerializeField] public float particleFadeOutTime = 0.5f;
    
    [Header("Engine Response")]
    [SerializeField] public float engineStartupTime = 0.2f;
    [SerializeField] public float engineShutdownTime = 0.3f;
    [SerializeField] public float force_max_timer = 2f; // Время полного нарастания мощности
    [SerializeField] public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] public AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Audio")]
    [SerializeField] public AudioClip engineStartSound;
    [SerializeField] public AudioClip engineLoopSound;
    [SerializeField] public AudioClip engineStopSound;
    [SerializeField] public float engineVolume = 0.7f;
    [SerializeField] public float enginePitch = 1f;
    
    /// <summary>
    /// Получает текущую мощность двигателя на основе времени работы
    /// </summary>
    public float GetCurrentPower(float normalizedTime)
    {
        return powerCurve.Evaluate(normalizedTime) * force_power;
    }
    
    /// <summary>
    /// Получает текущую интенсивность эмиссии частиц на основе мощности
    /// </summary>
    public float GetCurrentEmissionRate(float normalizedPower)
    {
        return Mathf.Lerp(minEmissionRate, maxEmissionRate, emissionCurve.Evaluate(normalizedPower));
    }
    
    /// <summary>
    /// Получает текущую скорость частиц на основе мощности
    /// </summary>
    public float GetCurrentParticleSpeed(float normalizedPower)
    {
        return Mathf.Lerp(minStartSpeed, maxStartSpeed, normalizedPower);
    }
    
    /// <summary>
    /// Получает текущий размер частиц на основе мощности
    /// </summary>
    public float GetCurrentParticleSize(float normalizedPower)
    {
        return Mathf.Lerp(minStartSize, maxStartSize, normalizedPower);
    }
}
