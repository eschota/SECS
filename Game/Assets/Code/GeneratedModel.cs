using UnityEngine;

public class GeneratedModel : MonoBehaviour
{
    [Header("Generated Model Settings")]
    [SerializeField] public string modelName;
    [SerializeField] public string resolution = "10k"; // 1k, 10k, 100k
    [SerializeField] public MeshRenderer[] meshRenderers; // Массив всех меш рендереров
    [SerializeField] public io_base io_base;
    [SerializeField] public GameObject cellsContainer; // Контейнер для клеток

    
#if UNITY_EDITOR
    [ExecuteInEditMode]

    void Awake()
    {
        if(!Application.isEditor)return;
       ini();
    }
        void OnValidate()
    {
        if (!Application.isEditor) return;
        
        AssignMaterialToMeshRenderer();
        
        // Обновляем клетки при изменении scale
        if (io_base != null && cellsContainer != null)
        {
            UpdateIOCells();
        }
    }
    public void ini(){
         AssignMaterialToMeshRenderer();
        if (io_base == null) io_base = transform.parent.gameObject.GetComponent<io_base>();
        if (meshRenderers == null || meshRenderers.Length == 0) 
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }
        
        // Находим Cells контейнер если он не назначен
        if (cellsContainer == null)
        {
            var cellsTransform = transform.parent.Find("Cells");
            if (cellsTransform != null)
            {
                cellsContainer = cellsTransform.gameObject;
            }
            else
            {
                Debug.LogWarning("Cells контейнер не найден! Создаем новый...");
                // Создаем новый контейнер
                GameObject newCellsContainer = new GameObject("Cells");
                newCellsContainer.transform.SetParent(transform.parent);
                cellsContainer = newCellsContainer;
            }
        }
        
        if (io_base != null)
        {
            // 0 - 1k, 1 - 10k, 2 - 100k
            if (modelName.EndsWith("_1k"))
                io_base.generated_models[0] = this;
            else if (modelName.EndsWith("_10k"))
                io_base.generated_models[1] = this;
            else if (modelName.EndsWith("_100k"))
                io_base.generated_models[2] = this;
        }
        
        Debug.Log($"ini: io_base={io_base}, cellsContainer={cellsContainer}, meshRenderers.Length={meshRenderers?.Length}");
        UpdateIOCells();
    }
    void Update()
    {
        // Убираем Update - клетки обновляются только в OnValidate при изменении scale
    }
    
    // Метод для ручного обновления клеток (можно вызывать из редактора)
    [ContextMenu("Обновить клетки")]
    public void UpdateCells()
    {
        if (io_base != null && cellsContainer != null)
        {
            UpdateIOCells();
        }
    }
    
    private void UpdateIOCells()
    {
        if (io_base == null || cellsContainer == null) 
        {
            Debug.LogWarning($"UpdateIOCells: io_base или cellsContainer null. io_base: {io_base}, cellsContainer: {cellsContainer}");
            return;
        }
        
        // Получаем размеры модели и округляем вверх
        Vector3 modelSize = transform.localScale;
        int cellsX = Mathf.CeilToInt(modelSize.x);
        int cellsY = Mathf.CeilToInt(modelSize.y);
        int cellsZ = Mathf.CeilToInt(modelSize.z);
        
        Debug.Log($"UpdateIOCells: Размер модели: {modelSize}, Размер клетки: {cellsX}x{cellsY}x{cellsZ}");
        
        // Создаем или находим одну клетку
        io_cell singleCell = null;
        
        if (io_base.target_cells == null || io_base.target_cells.Length == 0)
        {
            // Создаем новую клетку
            singleCell = CreateNewIOCell();
            io_base.target_cells = new io_cell[] { singleCell };
        }
        else
        {
            // Используем существующую клетку
            singleCell = io_base.target_cells[0];
        }
        
        if (singleCell != null)
        {
            // Масштабируем клетку под размер модели
            singleCell.transform.localScale = new Vector3(cellsX, cellsY, cellsZ);
            singleCell.transform.localPosition = Vector3.zero;
            
            Debug.Log($"UpdateIOCells: Клетка масштабирована до {cellsX}x{cellsY}x{cellsZ}");
        }
    }
    
    private io_cell CreateNewIOCell()
    {
        // Создаем новую клетку
        GameObject newCell = new GameObject("io_cell");
        newCell.transform.SetParent(cellsContainer.transform);
        
        // Добавляем компонент io_cell
        var ioCell = newCell.AddComponent<io_cell>();
        
        // Создаем TargetCollider - Box Collider размером 1
        var targetCollider = newCell.AddComponent<BoxCollider>();
        targetCollider.size = Vector3.one; // Размер 1x1x1
        targetCollider.isTrigger = true; // Делаем триггером
        
        Debug.Log($"Создана новая io_cell с TargetCollider: {newCell.name}");
        return ioCell;
    }
    

#endif
    private void Start()
    {
        // Также назначаем материал при старте игры
        AssignMaterialToMeshRenderer();
    }
    
    private void AssignMaterialToMeshRenderer()
    {
        if (string.IsNullOrEmpty(modelName))
        {
            // Пытаемся получить имя из имени объекта
            modelName = gameObject.name;
            
            // Убираем суффикс разрешения если есть
            if (modelName.EndsWith("_1k") || modelName.EndsWith("_10k") || modelName.EndsWith("_100k"))
            {
                modelName = modelName.Substring(0, modelName.LastIndexOf('_'));
            }
        }
        
        // Определяем разрешение из имени объекта если не задано
        if (string.IsNullOrEmpty(resolution))
        {
            if (gameObject.name.EndsWith("_1k"))
                resolution = "1k";
            else if (gameObject.name.EndsWith("_100k"))
                resolution = "100k";
            else
                resolution = "10k"; // По умолчанию
        }
        
        // Загружаем материал
        string materialPath = $"Assets/Meshes/{modelName}/{modelName}_{resolution}.mat";
        Material material = null;
        
        // Сначала пробуем через AssetDatabase (для редактора)
        #if UNITY_EDITOR
        material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        #endif
        
        // Если не найден, пробуем через Resources
        if (material == null)
        {
            string resourcesPath = $"Meshes/{modelName}/{modelName}_{resolution}";
            material = Resources.Load<Material>(resourcesPath);
        }
        

        
        if (material != null && meshRenderers != null)
        {
            // Назначаем материал на все меш рендереры
            foreach (var meshRenderer in meshRenderers)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.material = material;
                    Debug.Log($"✅ Материал {material.name} назначен на {meshRenderer.name} (путь: {materialPath})");
                }
            }
        }
        else
        {
            Debug.LogWarning($"❌ Материал не найден по пути: {materialPath}");
            Debug.LogWarning($"   modelName: {modelName}, resolution: {resolution}");
        }
    }
    
    // Метод для ручного обновления материала
    [ContextMenu("Обновить материал")]
    public void UpdateMaterial()
    {
        AssignMaterialToMeshRenderer();
    }
    
    // Метод для установки имени модели
    public void SetModelName(string name)
    {
        modelName = name;
        AssignMaterialToMeshRenderer();
    }
    
    // Метод для установки разрешения
    public void SetResolution(string res)
    {
        resolution = res;
        AssignMaterialToMeshRenderer();
    }
}
