using UnityEngine;
using System;

public class HullNode : MonoBehaviour
{
    [Header("Node Data")]
    [SerializeField] private NodeType nodeType;
    [SerializeField] private int nodeId;
    [SerializeField] private Vector3 nodePosition;
    [SerializeField] private Quaternion nodeRotation;
    [SerializeField] private float nodeLength;
    
    public enum NodeType
    {
        Point,
        Wall,
        Door
    }
    
    // Ссылки на данные
    private HullPoint pointData;
    private HullWall wallData;
    private HullDoor doorData;
    
    // Свойства для доступа к данным
    public NodeType Type => nodeType;
    public int Id => nodeId;
    public Vector3 Position => nodePosition;
    public Quaternion Rotation => nodeRotation;
    public float Length => nodeLength;
    
    public HullPoint PointData => pointData;
    public HullWall WallData => wallData;
    public HullDoor DoorData => doorData;
    
    // Инициализация для точки
    public void Initialize(HullPoint point)
    {
        nodeType = NodeType.Point;
        nodeId = point.id;
        nodePosition = point.position;
        nodeRotation = Quaternion.identity;
        nodeLength = 0f;
        
        pointData = point;
        
        // Устанавливаем позицию объекта
        transform.position = point.position;
        transform.rotation = Quaternion.identity;
        
        Debug.Log($"[HullNode] Инициализирована точка {point.id} в позиции {point.position}");
    }
    
    // Инициализация для стены
    public void Initialize(HullWall wall)
    {
        nodeType = NodeType.Wall;
        nodeId = wall.startPointId; // Используем ID начальной точки как ID стены
        nodePosition = (wall.startPosition + wall.endPosition) * 0.5f;
        nodeRotation = wall.rotation;
        nodeLength = wall.length;
        
        wallData = wall;
        
        // Устанавливаем позицию и поворот объекта
        transform.position = nodePosition;
        transform.rotation = nodeRotation;
        
        // Настраиваем размер стены (если есть компонент для изменения размера)
        SetupWallSize();
        
        Debug.Log($"[HullNode] Инициализирована стена от точки {wall.startPointId} к точке {wall.endPointId}, длина: {wall.length}");
    }
    
    // Инициализация для двери
    public void Initialize(HullDoor door)
    {
        nodeType = NodeType.Door;
        nodeId = door.startPointId; // Используем ID начальной точки как ID двери
        nodePosition = door.position;
        nodeRotation = door.rotation;
        nodeLength = 1f; // Стандартная ширина двери
        
        doorData = door;
        
        // Устанавливаем позицию и поворот объекта
        transform.position = door.position;
        transform.rotation = door.rotation;
        
        Debug.Log($"[HullNode] Инициализирована дверь между точками {door.startPointId} и {door.endPointId}");
    }
    
    private void SetupWallSize()
    {
        // Пытаемся найти компоненты для изменения размера стены
        var lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, wallData.startPosition);
            lineRenderer.SetPosition(1, wallData.endPosition);
        }
        
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Если есть меш рендерер, пытаемся изменить размер через скейл
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Получаем размеры меша
                Bounds bounds = meshFilter.sharedMesh.bounds;
                float meshLength = bounds.size.z; // Предполагаем, что Z - это длина
                
                if (meshLength > 0)
                {
                    float scale = nodeLength / meshLength;
                    transform.localScale = new Vector3(1f, 1f, scale);
                }
            }
        }
    }
    
    // Методы для получения информации о соединениях
    public int[] GetConnectedPointIds()
    {
        switch (nodeType)
        {
            case NodeType.Wall:
                return new int[] { wallData.startPointId, wallData.endPointId };
            case NodeType.Door:
                return new int[] { doorData.startPointId, doorData.endPointId };
            case NodeType.Point:
                return new int[] { pointData.id };
            default:
                return new int[0];
        }
    }
    
    public bool IsConnectedToPoint(int pointId)
    {
        var connectedIds = GetConnectedPointIds();
        return Array.Exists(connectedIds, id => id == pointId);
    }
    
    // Методы для сериализации
    public string SerializeNode()
    {
        var nodeData = new
        {
            type = nodeType.ToString(),
            id = nodeId,
            position = nodePosition,
            rotation = nodeRotation,
            length = nodeLength
        };
        
        return JsonUtility.ToJson(nodeData, true);
    }
    
    public void DeserializeNode(string jsonData)
    {
        var nodeData = JsonUtility.FromJson<NodeData>(jsonData);
        
        if (nodeData != null)
        {
            nodeType = (NodeType)System.Enum.Parse(typeof(NodeType), nodeData.type);
            nodeId = nodeData.id;
            nodePosition = nodeData.position;
            nodeRotation = nodeData.rotation;
            nodeLength = nodeData.length;
            
            transform.position = nodePosition;
            transform.rotation = nodeRotation;
        }
    }
    
    // Вспомогательный класс для сериализации
    [Serializable]
    private class NodeData
    {
        public string type;
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public float length;
    }
    
    // Методы для отладки
    private void OnDrawGizmosSelected()
    {
        switch (nodeType)
        {
            case NodeType.Point:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.1f);
                break;
            case NodeType.Wall:
                Gizmos.color = Color.blue;
                if (wallData != null)
                {
                    Gizmos.DrawLine(wallData.startPosition, wallData.endPosition);
                }
                break;
            case NodeType.Door:
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
                break;
        }
    }
} 