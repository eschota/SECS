using UnityEngine;

public class io_engine : io_base
{
    [Header("Engine Properties")]
    [SerializeField] public float force_power = 1000f;
    [SerializeField] public ForceMode force_type = ForceMode.Force;
    [SerializeField] public Vector3 force_vector_local = Vector3.forward;
    [SerializeField] public float fuel_per_second = 10f;
    [SerializeField] public float electricity_per_second = 5f;

    // Переопределяем методы сериализации
    public override string GetCellType() => "io_engine";
    
    public override void SerializeToData(io_base_serialized data)
    {
        // Сначала сериализуем базовые данные
        base.SerializeToData(data);
        
        // Затем добавляем специфичные для двигателя данные
        if (data is io_engine_serialized engineData)
        {
            engineData.force_power = force_power;
            engineData.force_type = (int)force_type; // ForceMode приводится к int
            engineData.force_vector_local = force_vector_local;
            engineData.fuel_per_second = fuel_per_second;
            engineData.electricity_per_second = electricity_per_second;
        }
    }
    
    public override void DeserializeFromData(io_base_serialized data)
    {
        // Сначала десериализуем базовые данные
        base.DeserializeFromData(data);
        
        // Затем добавляем специфичные для двигателя данные
        if (data is io_engine_serialized engineData)
        {
            force_power = engineData.force_power;
            force_type = (ForceMode)engineData.force_type; // int приводится к ForceMode
            force_vector_local = engineData.force_vector_local;
            fuel_per_second = engineData.fuel_per_second;
            electricity_per_second = engineData.electricity_per_second;
        }
    }
    
    /// <summary>
    /// Применяет силу к Rigidbody согласно настройкам двигателя
    /// </summary>
    public void ApplyForce(Rigidbody targetRigidbody)
    {
        if (targetRigidbody == null) return;
        
        // Преобразуем локальный вектор силы в мировые координаты
        Vector3 worldForce = transform.TransformDirection(force_vector_local) * force_power;
        
        // Применяем силу с выбранным типом
        targetRigidbody.AddForce(worldForce, force_type);
        
        Debug.Log($"Engine {name} applied force: {worldForce} with mode: {force_type}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Рисуем стрелку направления силы
        Gizmos.color = Color.red;
        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.TransformDirection(force_vector_local) * 2f;
        
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
    }
#endif
}
