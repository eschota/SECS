using UnityEngine;

public class HullPointPrefab : MonoBehaviour
{
    [Header("Point Settings")]
    [SerializeField] private float pointRadius = 0.1f;
    [SerializeField] private Color pointColor = Color.green;
    
    private HullNode hullNode;
    
    void Start()
    {
        // Добавляем компонент HullNode если его нет
        hullNode = GetComponent<HullNode>();
        if (hullNode == null)
        {
            hullNode = gameObject.AddComponent<HullNode>();
        }
        
        // Создаем простую сферу для визуализации
        CreatePointVisual();
    }
    
    private void CreatePointVisual()
    {
        // Создаем дочерний объект для визуализации
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * pointRadius * 2f;
        
        // Настраиваем материал
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = pointColor;
            renderer.material = material;
        }
        
        // Удаляем коллайдер, чтобы не мешать строительству
        DestroyImmediate(visual.GetComponent<Collider>());
        
        visual.name = "PointVisual";
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = pointColor;
        Gizmos.DrawWireSphere(transform.position, pointRadius);
    }
} 