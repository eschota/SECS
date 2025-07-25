using UnityEngine;
using System.Collections.Generic;

public class combine_collider : MonoBehaviour
{
    [Header("Настройки коллайдера")]
    [SerializeField] private List<GameObject> targetObjects = new List<GameObject>();
    [SerializeField] private bool autoGenerateOnStart = false;
    [SerializeField] private bool isTrigger = false;
    [SerializeField] private PhysicsMaterial physicMaterial;
    
    [Header("Настройки генерации")]
    [SerializeField] private bool useConvexHull = true;
    [SerializeField] private bool includeChildren = true;
    
    private MeshCollider generatedCollider;
    
    void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateCombinedCollider();
        }
    }
    
    [ContextMenu("Сгенерировать коллайдер")]
    public void GenerateCombinedCollider()
    {
        if (targetObjects.Count == 0)
        {
            Debug.LogWarning("Нет выбранных объектов для генерации коллайдера!");
            return;
        }
        
        // Очищаем все старые коллайдеры и меши из памяти
        ClearAllCollidersFromMemory();
        
        // Создаем новый MeshCollider
        generatedCollider = gameObject.AddComponent<MeshCollider>();
        generatedCollider.isTrigger = isTrigger;
        generatedCollider.convex = useConvexHull;
        
        if (physicMaterial != null)
        {
            generatedCollider.material = physicMaterial;
        }
        
        // Генерируем меш из выбранных объектов
        Mesh combinedMesh = CreateCombinedMesh();
        if (combinedMesh != null)
        {
            generatedCollider.sharedMesh = combinedMesh;
            Debug.Log($"Коллайдер успешно сгенерирован из {targetObjects.Count} объектов!");
        }
        else
        {
            Debug.LogError("Не удалось создать меш для коллайдера!");
        }
    }
    
    private Mesh CreateCombinedMesh()
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        
        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;
            
            // Получаем все меши из объекта и его дочерних элементов
            MeshFilter[] meshFilters = includeChildren ? 
                obj.GetComponentsInChildren<MeshFilter>() : 
                obj.GetComponents<MeshFilter>();
            
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = meshFilter.sharedMesh;
                    combineInstance.transform = meshFilter.transform.localToWorldMatrix;
                    combineInstances.Add(combineInstance);
                }
            }
        }
        
        if (combineInstances.Count == 0)
        {
            Debug.LogWarning("Не найдено мешей в выбранных объектах!");
            return null;
        }
        
        // Создаем комбинированный меш
        Mesh combinedMesh = new Mesh();
        combinedMesh.name = "CombinedColliderMesh";
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
        
        return combinedMesh;
    }
    
    [ContextMenu("Очистить коллайдер")]
    public void ClearCollider()
    {
        if (generatedCollider != null)
        {
            DestroyImmediate(generatedCollider);
            generatedCollider = null;
            Debug.Log("Коллайдер удален!");
        }
    }
    
    [ContextMenu("Очистить все коллайдеры из памяти")]
    public void ClearAllCollidersFromMemory()
    {
        // Очищаем коллайдер на текущем объекте
        ClearCollider();
        
        // Находим и удаляем все MeshCollider компоненты в сцене
        MeshCollider[] allMeshColliders = FindObjectsOfType<MeshCollider>();
        int removedCount = 0;
        
        foreach (MeshCollider collider in allMeshColliders)
        {
            if (collider != null)
            {
                DestroyImmediate(collider);
                removedCount++;
            }
        }
        
        // Очищаем все созданные меши из памяти
        Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
        int meshRemovedCount = 0;
        
        foreach (Mesh mesh in allMeshes)
        {
            if (mesh != null && mesh.name.Contains("CombinedColliderMesh"))
            {
                DestroyImmediate(mesh);
                meshRemovedCount++;
            }
        }
        
        // Принудительно очищаем память
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        Debug.Log($"Очищено {removedCount} коллайдеров и {meshRemovedCount} мешей из памяти сцены!");
    }
    
    [ContextMenu("Добавить выбранные объекты")]
    public void AddSelectedObjects()
    {
        GameObject[] selectedObjects = UnityEditor.Selection.gameObjects;
        foreach (GameObject obj in selectedObjects)
        {
            if (!targetObjects.Contains(obj))
            {
                targetObjects.Add(obj);
            }
        }
        Debug.Log($"Добавлено {selectedObjects.Length} объектов в список!");
    }
    
    [ContextMenu("Очистить список объектов")]
    public void ClearObjectList()
    {
        targetObjects.Clear();
        Debug.Log("Список объектов очищен!");
    }
    
    void OnValidate()
    {
        // Обновляем коллайдер при изменении настроек в инспекторе
        if (Application.isPlaying && generatedCollider != null)
        {
            generatedCollider.isTrigger = isTrigger;
            generatedCollider.convex = useConvexHull;
            if (physicMaterial != null)
            {
                generatedCollider.material = physicMaterial;
            }
        }
    }
}
