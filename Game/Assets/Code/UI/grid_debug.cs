using UnityEngine;

public class grid_debug : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private Material gridMaterial;
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        if (gridMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                gridMaterial = renderer.material;
            }
        }
        
        // Устанавливаем базовые параметры для тестирования
        if (gridMaterial != null)
        {
            gridMaterial.SetFloat("_GridSize", 1.0f); // Маленькая сетка для тестирования
            gridMaterial.SetFloat("_LineWidth", 0.05f); // Тонкие линии
            gridMaterial.SetFloat("_BlurAmount", 0.01f); // Минимальный блюр
            gridMaterial.SetColor("_GridColor", Color.green); // Яркий зеленый
            gridMaterial.SetFloat("_UseMouseFade", 0.0f); // Отключаем затухание для тестирования
            
            Debug.Log("Grid debug: Basic parameters set");
        }
    }
    
    void Update()
    {
        if (showDebugInfo && gridMaterial != null)
        {
            // Показываем текущие параметры шейдера
            if (Time.frameCount % 120 == 0) // Каждые 2 секунды
            {
                float gridSize = gridMaterial.GetFloat("_GridSize");
                float lineWidth = gridMaterial.GetFloat("_LineWidth");
                Color gridColor = gridMaterial.GetColor("_GridColor");
                Vector4 fadeCenter = gridMaterial.GetVector("_FadeCenter");
                
                Debug.Log($"Grid Debug - GridSize: {gridSize}, LineWidth: {lineWidth}, GridColor: {gridColor}, FadeCenter: {fadeCenter}");
            }
        }
    }
    
    // Методы для тестирования
    [ContextMenu("Test Small Grid")]
    public void TestSmallGrid()
    {
        if (gridMaterial != null)
        {
            gridMaterial.SetFloat("_GridSize", 0.5f);
            gridMaterial.SetFloat("_LineWidth", 0.02f);
            Debug.Log("Test: Small grid applied");
        }
    }
    
    [ContextMenu("Test Large Grid")]
    public void TestLargeGrid()
    {
        if (gridMaterial != null)
        {
            gridMaterial.SetFloat("_GridSize", 2.0f);
            gridMaterial.SetFloat("_LineWidth", 0.1f);
            Debug.Log("Test: Large grid applied");
        }
    }
    
    [ContextMenu("Test Bright Colors")]
    public void TestBrightColors()
    {
        if (gridMaterial != null)
        {
            gridMaterial.SetColor("_GridColor", Color.red);
            gridMaterial.SetColor("_FadeColor", Color.blue);
            Debug.Log("Test: Bright colors applied");
        }
    }
} 