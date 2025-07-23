using UnityEngine;

public class HullDoorPrefab : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Color doorColor = Color.yellow;
    [SerializeField] private float doorWidth = 1f;
    [SerializeField] private float doorHeight = 2f;
    [SerializeField] private float doorThickness = 0.1f;
    
    private HullNode hullNode;
    
    void Start()
    {
        // Добавляем компонент HullNode если его нет
        hullNode = GetComponent<HullNode>();
        if (hullNode == null)
        {
            hullNode = gameObject.AddComponent<HullNode>();
        }
        
        // Создаем визуализацию двери
        CreateDoorVisual();
    }
    
    private void CreateDoorVisual()
    {
        // Создаем дочерний объект для визуализации двери
        GameObject doorVisual = new GameObject("DoorVisual");
        doorVisual.transform.SetParent(transform);
        doorVisual.transform.localPosition = Vector3.zero;
        
        // Добавляем компоненты для меша
        MeshFilter meshFilter = doorVisual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = doorVisual.AddComponent<MeshRenderer>();
        
        // Создаем простой куб как основу для двери
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshFilter.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        
        // Настраиваем материал
        Material material = new Material(Shader.Find("Standard"));
        material.color = doorColor;
        meshRenderer.material = material;
        
        // Устанавливаем размеры двери
        doorVisual.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
        
        // Удаляем коллайдер
        DestroyImmediate(doorVisual.GetComponent<Collider>());
        
        // Создаем рамку двери
        CreateDoorFrame();
    }
    
    private void CreateDoorFrame()
    {
        // Создаем рамку двери как отдельный объект
        GameObject doorFrame = new GameObject("DoorFrame");
        doorFrame.transform.SetParent(transform);
        doorFrame.transform.localPosition = Vector3.zero;
        
        // Добавляем компоненты для меша рамки
        MeshFilter frameMeshFilter = doorFrame.AddComponent<MeshFilter>();
        MeshRenderer frameMeshRenderer = doorFrame.AddComponent<MeshRenderer>();
        
        // Создаем простой куб для рамки
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frameMeshFilter.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        
        // Настраиваем материал рамки (темнее чем дверь)
        Material frameMaterial = new Material(Shader.Find("Standard"));
        frameMaterial.color = new Color(doorColor.r * 0.5f, doorColor.g * 0.5f, doorColor.b * 0.5f);
        frameMeshRenderer.material = frameMaterial;
        
        // Рамка немного больше двери
        float frameSize = Mathf.Max(doorWidth, doorHeight) + 0.1f;
        doorFrame.transform.localScale = new Vector3(frameSize, frameSize, doorThickness * 2f);
        
        // Удаляем коллайдер
        DestroyImmediate(doorFrame.GetComponent<Collider>());
    }
    
    public void UpdateDoorVisual(Vector3 startPos, Vector3 endPos)
    {
        // Обновляем позицию и поворот двери
        Vector3 center = (startPos + endPos) * 0.5f;
        Vector3 direction = (endPos - startPos).normalized;
        
        transform.position = center;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    void OnDrawGizmos()
    {
        if (hullNode != null && hullNode.Type == HullNode.NodeType.Door)
        {
            HullDoor doorData = hullNode.DoorData;
            if (doorData != null)
            {
                Gizmos.color = doorColor;
                Gizmos.DrawWireCube(transform.position, new Vector3(doorWidth, doorHeight, doorThickness));
            }
        }
    }
} 