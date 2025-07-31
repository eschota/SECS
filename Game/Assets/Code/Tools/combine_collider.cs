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
    
    [Header("Сохранение в префаб")]
    [SerializeField] private bool saveToPrefab = true;
    [SerializeField] private Mesh savedCombinedMesh;
    
    private MeshCollider generatedCollider;
    
    void Start()
    {
        // Если есть сохраненный меш, используем его
        if (saveToPrefab && savedCombinedMesh != null)
        {
            CreateColliderFromSavedMesh();
        }
        else if (autoGenerateOnStart)
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
            
            // Сохраняем меш если включена опция сохранения в префаб
            if (saveToPrefab)
            {
                savedCombinedMesh = combinedMesh;
                Debug.Log($"Коллайдер успешно сгенерирован и сохранен в префаб из {targetObjects.Count} объектов!");
            }
            else
            {
                Debug.Log($"Коллайдер успешно сгенерирован из {targetObjects.Count} объектов!");
            }
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
    
    private void CreateColliderFromSavedMesh()
    {
        if (savedCombinedMesh == null)
        {
            Debug.LogWarning("Сохраненный меш не найден!");
            return;
        }
        
        // Очищаем старый коллайдер если есть
        ClearCollider();
        
        // Создаем новый MeshCollider с сохраненным мешем
        generatedCollider = gameObject.AddComponent<MeshCollider>();
        generatedCollider.isTrigger = isTrigger;
        generatedCollider.convex = useConvexHull;
        generatedCollider.sharedMesh = savedCombinedMesh;
        
        if (physicMaterial != null)
        {
            generatedCollider.material = physicMaterial;
        }
        
        Debug.Log("Коллайдер создан из сохраненного меша!");
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
    
    [ContextMenu("Сохранить меш в префаб")]
    public void SaveMeshToPrefab()
    {
        if (generatedCollider != null && generatedCollider.sharedMesh != null)
        {
            savedCombinedMesh = generatedCollider.sharedMesh;
            Debug.Log("Меш сохранен в префаб!");
        }
        else
        {
            Debug.LogWarning("Нет активного коллайдера для сохранения!");
        }
    }
    
    [ContextMenu("Очистить сохраненный меш")]
    public void ClearSavedMesh()
    {
        savedCombinedMesh = null;
        Debug.Log("Сохраненный меш очищен!");
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
