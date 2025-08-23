using UnityEngine;
using UnityEditor;
using System.IO;

public class GeneratedModelImporter : AssetPostprocessor
{
    private static string currentObjectName = "";
    
    // Устанавливаем имя объекта для текущего импорта
    public static void SetCurrentObjectName(string objectName)
    {
        currentObjectName = objectName;
    }
    
    // Вызывается после импорта FBX файла
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".fbx") && assetPath.Contains("/Meshes/"))
            {
                ProcessFBXAsset(assetPath);
            }
        }
    }
    
    private static void ProcessFBXAsset(string assetPath)
    {
        // Извлекаем имя объекта из пути
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string objectName = ExtractObjectName(fileName);
        
        // Если имя объекта не задано, используем текущее
        if (string.IsNullOrEmpty(objectName) && !string.IsNullOrEmpty(currentObjectName))
        {
            objectName = currentObjectName;
        }
        
        if (!string.IsNullOrEmpty(objectName))
        {
            // Загружаем префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                // Проверяем, есть ли уже скрипт GeneratedModel
                var existingScript = prefab.GetComponent<GeneratedModel>();
                if (existingScript == null)
                {
                    // Добавляем скрипт
                    var script = prefab.AddComponent<GeneratedModel>();
                    
                    // Устанавливаем параметры
                    SerializedObject serializedScript = new SerializedObject(script);
                    var modelNameProperty = serializedScript.FindProperty("modelName");
                    var resolutionProperty = serializedScript.FindProperty("resolution");
                    
                    if (modelNameProperty != null)
                        modelNameProperty.stringValue = objectName;
                    
                    if (resolutionProperty != null)
                    {
                        // Определяем разрешение из имени файла
                        string resolution = ExtractResolution(fileName);
                        resolutionProperty.stringValue = resolution;
                    }
                    
                    serializedScript.ApplyModifiedProperties();
                    
                    // Сохраняем изменения
                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log($"✅ Скрипт GeneratedModel добавлен на {fileName} (Object: {objectName})");
                }
            }
        }
    }
    
    private static string ExtractObjectName(string fileName)
    {
        // Убираем суффикс разрешения (_1k, _10k, _100k)
        if (fileName.EndsWith("_1k") || fileName.EndsWith("_10k") || fileName.EndsWith("_100k"))
        {
            return fileName.Substring(0, fileName.LastIndexOf('_'));
        }
        return fileName;
    }
    
    private static string ExtractResolution(string fileName)
    {
        if (fileName.EndsWith("_1k"))
            return "1k";
        else if (fileName.EndsWith("_100k"))
            return "100k";
        else
            return "10k"; // По умолчанию
    }
}
