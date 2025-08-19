using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class ModelDownloader : EditorWindow
{
    private string downloadUrl = "";
    private string objectName = "";
    private bool isDownloading = false;
    private string downloadStatus = "";
    private float downloadProgress = 0f;

    [MenuItem("Tools/Model Downloader")]
    public static void ShowWindow()
    {
        GetWindow<ModelDownloader>("Model Downloader");
    }

    void OnGUI()
    {
        GUILayout.Label("Model Downloader", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // URL input field
        GUILayout.Label("GLB URL:");
        downloadUrl = EditorGUILayout.TextField(downloadUrl);
        GUILayout.Space(5);

        // Object name input field
        GUILayout.Label("Object Name:");
        objectName = EditorGUILayout.TextField(objectName);
        GUILayout.Space(10);

        // Download button
        GUI.enabled = !isDownloading && !string.IsNullOrEmpty(downloadUrl) && !string.IsNullOrEmpty(objectName);
        if (GUILayout.Button("СКАЧАТЬ", GUILayout.Height(30)))
        {
            StartDownload();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Status display
        if (isDownloading)
        {
            EditorGUILayout.LabelField("Статус:", downloadStatus);
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), downloadProgress, $"{(downloadProgress * 100):F1}%");
        }
        else if (!string.IsNullOrEmpty(downloadStatus))
        {
            EditorGUILayout.LabelField("Статус:", downloadStatus);
            
            if (downloadStatus.StartsWith("Готово!"))
            {
                GUILayout.Space(5);
                if (GUILayout.Button("Очистить статус", GUILayout.Height(20)))
                {
                    downloadStatus = "";
                    downloadProgress = 0f;
                }
            }
        }

        GUILayout.Space(10);

        // Instructions
        EditorGUILayout.HelpBox(
            "Инструкция:\n" +
            "1. Вставьте ссылку на GLB файл\n" +
            "2. Введите имя объекта для папки\n" +
            "3. Нажмите СКАЧАТЬ\n\n" +
            "Скачаются файлы из трех папок:\n" +
            "• Версия 1K: текстуры + модель (низкое качество)\n" +
            "• Версия 10K: текстуры + модель (основное качество)\n" +
            "• Версия 100K: текстуры + модель + VRay камера (высокое качество)\n" +
            "• Автоматически создаются URP текстуры для всех версий\n" +
            "• Материал создается с текстурами 10K по умолчанию", 
            MessageType.Info);
    }

    private void StartDownload()
    {
        if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(objectName))
        {
            EditorUtility.DisplayDialog("Ошибка", "Заполните все поля!", "OK");
            return;
        }

        string baseName = ExtractBaseName(downloadUrl);
        if (string.IsNullOrEmpty(baseName))
        {
            EditorUtility.DisplayDialog("Ошибка", "Не удается извлечь имя файла из URL!", "OK");
            return;
        }

        isDownloading = true;
        downloadProgress = 0f;
        downloadStatus = "Начинаем скачивание...";

        EditorCoroutineUtility.StartCoroutine(DownloadAllFiles(baseName), this);
    }

    private string ExtractBaseName(string url)
    {
        try
        {
            // Извлекаем имя файла из URL (без расширения .glb)
            string fileName = Path.GetFileNameWithoutExtension(url);
            return fileName;
        }
        catch
        {
            return "";
        }
    }

    private IEnumerator DownloadAllFiles(string baseName)
    {
        // Убираем имя файла .glb и получаем базовый URL без слэша в конце
        string baseUrl = downloadUrl.Replace("/" + baseName + ".glb", "");
        string targetFolder = Path.Combine(Application.dataPath, "Resources", "Meshes", objectName);

        // Создаем папку если не существует
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // Список файлов для скачивания
        var filesToDownload = new[]
        {
            // Файлы из папки _1k
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_texture.png",
                localName = objectName + "_1k_texture.png",
                description = "диффузная текстура (1k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_texture_NormalGL.png",
                localName = objectName + "_1k_normal.png",
                description = "нормал карта (1k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_Metallic.png",
                localName = objectName + "_1k_Metallic.png",
                description = "металлик карта (1k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_Roughness.png",
                localName = objectName + "_1k_Roughness.png",
                description = "рафнесс карта (1k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + ".fbx",
                localName = objectName + "_1k.fbx",
                description = "3D модель (1k)"
            },
            
            // Файлы из папки _10k (основные)
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_texture.png",
                localName = objectName + "_10k_texture.png",
                description = "диффузная текстура (10k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_texture_NormalGL.png",
                localName = objectName + "_10k_normal.png",
                description = "нормал карта (10k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_Metallic.png",
                localName = objectName + "_10k_Metallic.png",
                description = "металлик карта (10k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_Roughness.png",
                localName = objectName + "_10k_Roughness.png",
                description = "рафнесс карта (10k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + ".fbx",
                localName = objectName + "_10k.fbx",
                description = "3D модель (10k)"
            },
            
            // Файлы из папки _100k
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_texture.png",
                localName = objectName + "_100k_texture.png",
                description = "диффузная текстура (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_texture_NormalGL.png",
                localName = objectName + "_100k_normal.png",
                description = "нормал карта (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_Metallic.png",
                localName = objectName + "_100k_Metallic.png",
                description = "металлик карта (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_Roughness.png",
                localName = objectName + "_100k_Roughness.png",
                description = "рафнесс карта (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_VRayCam001_view.jpg",
                localName = objectName + "_100k_VRayCam001_view.jpg",
                description = "VRay камера (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + ".fbx",
                localName = objectName + "_100k.fbx",
                description = "3D модель (100k)"
            }
        };

        int totalFiles = filesToDownload.Length;
        int currentFile = 0;

        foreach (var file in filesToDownload)
        {
            downloadStatus = $"Скачиваем {file.description}... ({currentFile + 1}/{totalFiles})";
            downloadProgress = (float)currentFile / totalFiles;
            Repaint();

            string localPath = Path.Combine(targetFolder, file.localName);
            
            // Отладочный вывод URL
            Debug.Log($"Скачиваем: {file.url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(file.url))
            {
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    float fileProgress = request.downloadProgress;
                    downloadProgress = ((float)currentFile + fileProgress) / totalFiles;
                    Repaint();
                    yield return null;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        File.WriteAllBytes(localPath, request.downloadHandler.data);
                        Debug.Log($"Скачан файл: {file.localName}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Ошибка сохранения файла {file.localName}: {e.Message}");
                        EditorUtility.DisplayDialog("Ошибка", $"Не удалось сохранить файл {file.localName}", "OK");
                        isDownloading = false;
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"Ошибка скачивания {file.url}: {request.error}");
                    EditorUtility.DisplayDialog("Ошибка", $"Не удалось скачать {file.description}", "OK");
                    isDownloading = false;
                    yield break;
                }
            }

            currentFile++;
        }

        // Завершение
        downloadProgress = 1f;
        downloadStatus = "Скачивание завершено!";
        isDownloading = false;
        Repaint();

        // Обновляем Asset Database
        AssetDatabase.Refresh();

        // Настраиваем импорт FBX и создаем материал
        downloadStatus = "Настраиваем импорт FBX...";
        Repaint();
        yield return new WaitForSeconds(0.5f); // Даем время на обновление AssetDatabase
        SetupFBXImportSettings();
        
        downloadStatus = "Создаем URP текстуру...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        CreateURPTexture();
        
        downloadStatus = "Настраиваем VRay камеру...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        SetupVRayCamTexture();
        
        downloadStatus = "Создаем материалы и назначаем на модели...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        CreateAndAssignMaterials();

        downloadStatus = "Готово! Все файлы скачаны, материалы созданы и назначены на модели.";
        Repaint();

        Debug.Log($"Скачивание завершено. Файлы сохранены в: {targetFolder}");
    }

    private void CreateURPTexture()
    {
        // Создаем URP текстуры для всех версий (1k, 10k и 100k)
        CreateURPTextureForResolution("1k");
        CreateURPTextureForResolution("10k");
        CreateURPTextureForResolution("100k");
    }

    private void CreateURPTextureForResolution(string resolution)
    {
        string metallicPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_Metallic.png";
        string roughnessPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_Roughness.png";
        string urpPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_urp.png";

        Texture2D metallicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
        Texture2D roughnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);

        if (metallicTexture != null && roughnessTexture != null)
        {
            // Делаем текстуры читаемыми
            MakeTextureReadable(metallicPath);
            MakeTextureReadable(roughnessPath);
            
            // Перезагружаем после изменения настроек импорта
            metallicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            roughnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);

            // Создаем новую текстуру
            int width = Mathf.Max(metallicTexture.width, roughnessTexture.width);
            int height = Mathf.Max(metallicTexture.height, roughnessTexture.height);
            
            Texture2D urpTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Комбинируем текстуры
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Получаем пиксели с масштабированием если размеры разные
                    float metallicU = (float)x / width;
                    float metallicV = (float)y / height;
                    float roughnessU = (float)x / width;
                    float roughnessV = (float)y / height;

                    Color metallicPixel = metallicTexture.GetPixelBilinear(metallicU, metallicV);
                    Color roughnessPixel = roughnessTexture.GetPixelBilinear(roughnessU, roughnessV);

                    // Metallic в RGB каналы (используем grayscale значение)
                    float metallicValue = metallicPixel.grayscale;
                    
                    // Roughness конвертируем в Smoothness (инверсия) для Alpha канала
                    float roughnessValue = roughnessPixel.grayscale;
                    float smoothnessValue = 1.0f - roughnessValue;

                    // Создаем финальный пиксель: Metallic в RGB, Smoothness в Alpha
                    Color finalPixel = new Color(metallicValue, metallicValue, metallicValue, smoothnessValue);
                    urpTexture.SetPixel(x, y, finalPixel);
                }
            }

            urpTexture.Apply();

            // Сохраняем текстуру
            byte[] pngData = urpTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(urpPath.Replace("Assets/", Application.dataPath + "/").Replace("Assets\\", Application.dataPath + "\\"), pngData);
            
            AssetDatabase.Refresh();
            
            // Настраиваем импорт для URP текстуры
            TextureImporter urpImporter = AssetImporter.GetAtPath(urpPath) as TextureImporter;
            if (urpImporter != null)
            {
                urpImporter.sRGBTexture = false; // Linear для metallic/smoothness
                urpImporter.SaveAndReimport();
            }

            Debug.Log($"Создана URP текстура: {objectName}_{resolution}_urp.png (Metallic RGB + Smoothness Alpha)");
            
            // Очищаем память
            DestroyImmediate(urpTexture);
        }
        else
        {
            Debug.LogWarning($"Не удалось найти Metallic или Roughness текстуры {resolution} для создания URP текстуры");
        }
    }

    private void MakeTextureReadable(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private void SetupVRayCamTexture()
    {
        string vrayCamPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_100k_VRayCam001_view.jpg";
        
        TextureImporter importer = AssetImporter.GetAtPath(vrayCamPath) as TextureImporter;
        if (importer != null)
        {
            // Настраиваем как Sprite
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            
            // Устанавливаем размер 512x512
            importer.maxTextureSize = 512;
            
            // Настройки качества
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 100;
            
            // Sprite настройки
            importer.spritePivot = new Vector2(0.5f, 0.5f); // Center pivot
            importer.spritePixelsPerUnit = 100;
            
            // Filter и Wrap настройки
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            
            // Применяем изменения
            importer.SaveAndReimport();
            
            Debug.Log($"VRay камера настроена как Sprite 512x512: {objectName}_100k_VRayCam001_view.jpg");
        }
        else
        {
            Debug.LogWarning($"Не удалось найти VRay камеру: {vrayCamPath}");
        }
    }

    private void SetupFBXImportSettings()
    {
        // Настраиваем все модели (1k, 10k и 100k)
        SetupFBXForResolution("1k");
        SetupFBXForResolution("10k");
        SetupFBXForResolution("100k");
    }

    private void SetupFBXForResolution(string resolution)
    {
        string fbxPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        
        if (importer != null)
        {
            // Scene settings
            importer.globalScale = 1f;
            importer.useFileUnits = false; // Convert Units = false
            importer.importBlendShapes = true;
            importer.importVisibility = true;
            importer.importCameras = true;
            importer.importLights = true;
            importer.preserveHierarchy = false;
            importer.sortHierarchyByName = true;

            // Meshes settings
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.addCollider = true;

            // Geometry settings
            importer.keepQuads = false;
            importer.weldVertices = true;
            importer.indexFormat = ModelImporterIndexFormat.Auto;

            // Normals settings
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
            importer.normalSmoothingAngle = 60f;

            // Tangents settings
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            // UV settings
            importer.swapUVChannels = false;
            importer.generateSecondaryUV = false;

            // Materials settings
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            // Apply settings
            importer.SaveAndReimport();
            Debug.Log($"FBX импорт настроен для {objectName}_{resolution}.fbx");
        }
        else
        {
            Debug.LogError($"Не удалось найти ModelImporter для {fbxPath}");
        }
    }

    private void CreateAndAssignMaterials()
    {
        // Создаем материалы для каждой детализации
        CreateAndAssignMaterialForResolution("1k");
        CreateAndAssignMaterialForResolution("10k");
        CreateAndAssignMaterialForResolution("100k");
    }

    private void CreateAndAssignMaterialForResolution(string resolution)
    {
        string materialPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}.mat";
        
        // Создаем материал со стандартным URP Lit шейдером
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = $"{objectName}_{resolution}";

        // Загружаем текстуры для конкретной детализации
        string texturePath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_texture.png";
        string normalPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_normal.png";
        string urpPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}_urp.png";

        Texture2D albedoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        Texture2D urpTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(urpPath);

        // Назначаем текстуры на материал
        if (albedoTexture != null)
        {
            material.SetTexture("_BaseMap", albedoTexture);
            Debug.Log($"Назначена диффузная текстура {resolution}: {objectName}_{resolution}_texture.png");
        }

        if (normalTexture != null)
        {
            material.SetTexture("_BumpMap", normalTexture);
            Debug.Log($"Назначена нормал карта {resolution}: {objectName}_{resolution}_normal.png");
        }

        if (urpTexture != null)
        {
            material.SetTexture("_MetallicGlossMap", urpTexture);
            // Включаем использование альфа канала для smoothness
            material.SetFloat("_SmoothnessTextureChannel", 1); // 1 = Alpha channel
            material.SetFloat("_Smoothness", 1.0f); // Максимальное значение, так как smoothness теперь в текстуре
            Debug.Log($"Назначена URP текстура {resolution}: {objectName}_{resolution}_urp.png (Metallic RGB + Smoothness Alpha)");
        }

        // Сохраняем материал
        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();

        // Назначаем материал на соответствующий меш
        AssignMaterialToMeshForResolution(materialPath, resolution);

        Debug.Log($"Материал {objectName}_{resolution}.mat создан и назначен на модель {resolution}");
    }

    private void AssignMaterialToMeshForResolution(string materialPath, string resolution)
    {
        string fbxPath = $"Assets/Resources/Meshes/{objectName}/{objectName}_{resolution}.fbx";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material != null)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                // Настраиваем импорт материалов
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                
                // Переназначаем материалы
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
                
                // Получаем все внешние объекты (включая материалы)
                var externalObjects = importer.GetExternalObjectMap();
                
                foreach (var kvp in externalObjects)
                {
                    if (kvp.Key.type == typeof(Material))
                    {
                        // Переназначаем материал через AddRemap
                        importer.AddRemap(kvp.Key, material);
                        Debug.Log($"Переназначаем материал {resolution}: {kvp.Key.name} -> {material.name}");
                    }
                }
                
                importer.SaveAndReimport();
                Debug.Log($"Материал {material.name} назначен на модель {objectName}_{resolution} через ModelImporter");
            }
        }
        else
        {
            Debug.LogError($"Не удалось загрузить материал {resolution}: {materialPath}");
        }
    }
}

// Helper class for Editor Coroutines
public static class EditorCoroutineUtility
{
    public static EditorCoroutine StartCoroutine(IEnumerator routine, object owner)
    {
        return EditorCoroutineRunner.StartCoroutine(routine);
    }
}

public class EditorCoroutine
{
    public IEnumerator routine;
    public bool isDone = false;
}

[InitializeOnLoad]
public static class EditorCoroutineRunner
{
    private static System.Collections.Generic.List<EditorCoroutine> coroutines = new System.Collections.Generic.List<EditorCoroutine>();

    static EditorCoroutineRunner()
    {
        EditorApplication.update += Update;
    }

    public static EditorCoroutine StartCoroutine(IEnumerator routine)
    {
        EditorCoroutine coroutine = new EditorCoroutine { routine = routine };
        coroutines.Add(coroutine);
        return coroutine;
    }

    private static void Update()
    {
        for (int i = coroutines.Count - 1; i >= 0; i--)
        {
            EditorCoroutine coroutine = coroutines[i];
            
            if (!coroutine.routine.MoveNext())
            {
                coroutine.isDone = true;
                coroutines.RemoveAt(i);
            }
        }
    }
}
