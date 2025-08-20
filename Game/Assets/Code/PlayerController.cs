using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] meshRenderers;
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;
    [SerializeField] private Color playerColor;
    
    private Material[] originalMaterials;
    private Material[] coloredMaterials;

    void Start()
    {
        ValidateAndSerializeMeshRenderers();
        GenerateRandomBrightColor();
        ApplyEmissiveColor();
    }

    void FixedUpdate()
    {
        // Синхронизируем позицию и ротацию с главной камерой
        if (Camera.main != null)
        {
            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    [ContextMenu("Validate Mesh Renderers")]
    void ValidateAndSerializeMeshRenderers()
    {
        // Находим все MeshRenderer'ы внутри игрока
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        
        Debug.Log($"Found {meshRenderers.Length} MeshRenderers and {skinnedMeshRenderers.Length} SkinnedMeshRenderers in player");
        
        // Сохраняем оригинальные материалы
        List<Material> origMats = new List<Material>();
        
        foreach (var mr in meshRenderers)
        {
            origMats.AddRange(mr.materials);
        }
        
        foreach (var smr in skinnedMeshRenderers)
        {
            origMats.AddRange(smr.materials);
        }
        
        originalMaterials = origMats.ToArray();
    }

    void GenerateRandomBrightColor()
    {
        // Генерируем случайный яркий цвет
        float hue = Random.Range(0f, 1f);
        float saturation = Random.Range(0.7f, 1f); // Высокая насыщенность для яркости
        float value = Random.Range(0.8f, 1f); // Высокая яркость
        
        playerColor = Color.HSVToRGB(hue, saturation, value);
        
        Debug.Log($"Generated bright color: {playerColor}");
    }

    void ApplyEmissiveColor()
    {
        // Применяем emissive цвет ко всем MeshRenderer'ам
        foreach (var meshRenderer in meshRenderers)
        {
            ApplyColorToRenderer(meshRenderer);
        }
        
        // Применяем emissive цвет ко всем SkinnedMeshRenderer'ам
        foreach (var skinnedRenderer in skinnedMeshRenderers)
        {
            ApplyColorToSkinnedRenderer(skinnedRenderer);
        }
    }

    void ApplyColorToRenderer(MeshRenderer renderer)
    {
        Material[] materials = renderer.materials;
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null) continue;
            
            // Создаем копию материала, чтобы не изменить оригинал
            Material newMat = new Material(mat);
            
            // Устанавливаем базовый цвет
            if (newMat.HasProperty("_BaseColor"))
            {
                newMat.SetColor("_BaseColor", playerColor);
            }
            else if (newMat.HasProperty("_Color"))
            {
                newMat.SetColor("_Color", playerColor);
            }
            
            // Устанавливаем emissive цвет
            if (newMat.HasProperty("_EmissionColor"))
            {
                newMat.SetColor("_EmissionColor", playerColor * 0.5f); // Немного приглушенный emissive
                newMat.EnableKeyword("_EMISSION");
            }
            
            materials[i] = newMat;
        }
        
        renderer.materials = materials;
    }

    void ApplyColorToSkinnedRenderer(SkinnedMeshRenderer renderer)
    {
        Material[] materials = renderer.materials;
        
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null) continue;
            
            // Создаем копию материала, чтобы не изменить оригинал
            Material newMat = new Material(mat);
            
            // Устанавливаем базовый цвет
            if (newMat.HasProperty("_BaseColor"))
            {
                newMat.SetColor("_BaseColor", playerColor);
            }
            else if (newMat.HasProperty("_Color"))
            {
                newMat.SetColor("_Color", playerColor);
            }
            
            // Устанавливаем emissive цвет
            if (newMat.HasProperty("_EmissionColor"))
            {
                newMat.SetColor("_EmissionColor", playerColor * 0.5f); // Немного приглушенный emissive
                newMat.EnableKeyword("_EMISSION");
            }
            
            materials[i] = newMat;
        }
        
        renderer.materials = materials;
    }

    // Метод для изменения цвета игрока извне (если понадобится)
    public void SetPlayerColor(Color newColor)
    {
        playerColor = newColor;
        ApplyEmissiveColor();
    }
    
    // Геттер для получения текущего цвета игрока
    public Color GetPlayerColor()
    {
        return playerColor;
    }
}
