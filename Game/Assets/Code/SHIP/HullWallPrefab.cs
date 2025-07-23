using UnityEngine;

public class HullWallPrefab : MonoBehaviour
{
    [Header("Wall Settings")]
    [SerializeField] private Color wallColor = Color.blue;
    [SerializeField] private float wallHeight = 2f;
    [SerializeField] private float wallThickness = 0.1f;
    
    private HullNode hullNode;
    private LineRenderer lineRenderer;
    
    void Start()
    {
        // Добавляем компонент HullNode если его нет
        hullNode = GetComponent<HullNode>();
        if (hullNode == null)
        {
            hullNode = gameObject.AddComponent<HullNode>();
        }
        
        // Создаем LineRenderer для визуализации стены
        CreateWallVisual();
    }
    
    private void CreateWallVisual()
    {
        // Создаем LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = wallColor;
        lineRenderer.startWidth = wallThickness;
        lineRenderer.endWidth = wallThickness;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        
        // Создаем также 3D меш для более реалистичной стены
        CreateWallMesh();
    }
    
    private void CreateWallMesh()
    {
        // Создаем дочерний объект для 3D меша стены
        GameObject wallMesh = new GameObject("WallMesh");
        wallMesh.transform.SetParent(transform);
        wallMesh.transform.localPosition = Vector3.zero;
        
        // Добавляем компоненты для меша
        MeshFilter meshFilter = wallMesh.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wallMesh.AddComponent<MeshRenderer>();
        
        // Создаем простой куб как основу для стены
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshFilter.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        
        // Настраиваем материал
        Material material = new Material(Shader.Find("Standard"));
        material.color = wallColor;
        meshRenderer.material = material;
        
        // Удаляем коллайдер
        DestroyImmediate(wallMesh.GetComponent<Collider>());
    }
    
    public void UpdateWallVisual(Vector3 startPos, Vector3 endPos)
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }
        
        // Обновляем 3D меш
        UpdateWallMesh(startPos, endPos);
        
        // Обновляем позицию и поворот объекта
        Vector3 center = (startPos + endPos) * 0.5f;
        Vector3 direction = (endPos - startPos).normalized;
        
        transform.position = center;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    private void UpdateWallMesh(Vector3 startPos, Vector3 endPos)
    {
        Transform wallMesh = transform.Find("WallMesh");
        if (wallMesh != null)
        {
            Vector3 center = (startPos + endPos) * 0.5f;
            Vector3 direction = (endPos - startPos).normalized;
            float length = Vector3.Distance(startPos, endPos);
            
            wallMesh.position = center;
            wallMesh.rotation = Quaternion.LookRotation(direction);
            wallMesh.localScale = new Vector3(wallThickness, wallHeight, length);
        }
    }
    
    void OnDrawGizmos()
    {
        if (hullNode != null && hullNode.Type == HullNode.NodeType.Wall)
        {
            HullWall wallData = hullNode.WallData;
            if (wallData != null)
            {
                Gizmos.color = wallColor;
                Gizmos.DrawLine(wallData.startPosition, wallData.endPosition);
            }
        }
    }
} 