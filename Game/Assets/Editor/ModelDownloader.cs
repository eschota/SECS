using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Linq;

// GeneratedModel script reference
// GeneratedModelImporter reference

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
            "• Версия 100K: текстуры + модель + иконка (высокое качество)\n" +
            "• Скачиваются готовые URP текстуры для всех версий\n" +
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
        string targetFolder = Path.Combine(Application.dataPath, "Meshes", objectName);

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
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_AO.png",
                localName = objectName + "_1k_AO.png",
                description = "AO карта (1k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_1k/" + baseName + "_unity_metallic_smoothness.png",
                localName = objectName + "_1k_urp.png",
                description = "URP текстура (1k)"
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
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_AO.png",
                localName = objectName + "_10k_AO.png",
                description = "AO карта (10k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_10k/" + baseName + "_unity_metallic_smoothness.png",
                localName = objectName + "_10k_urp.png",
                description = "URP текстура (10k)"
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
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_AO.png",
                localName = objectName + "_100k_AO.png",
                description = "AO карта (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_unity_metallic_smoothness.png",
                localName = objectName + "_100k_urp.png",
                description = "URP текстура (100k)"
            },
            new { 
                url = baseUrl + "/" + baseName + "_100k/" + baseName + "_icon.png",
                localName = objectName + "_100k_icon.png",
                description = "иконка (100k)"
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
        
        downloadStatus = "Настраиваем URP текстуры...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        SetupURPTextures();
        
        downloadStatus = "Настраиваем иконку...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        SetupIconTexture();
        
        downloadStatus = "Создаем материалы и назначаем на модели...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        CreateAndAssignMaterials();
        
        downloadStatus = "Создаем префаб с мешами...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        CreatePrefabWithMeshes();
        
        downloadStatus = "Создаем ScriptableObject...";
        Repaint();
        yield return new WaitForSeconds(0.2f);
        CreateScriptableObject();

        downloadStatus = "Готово! Все файлы скачаны, префаб создан и настроен.";
        Repaint();

        Debug.Log($"Скачивание завершено. Файлы сохранены в: {targetFolder}");
    }

    private void SetupURPTextures()
    {
        // Настраиваем URP текстуры для всех версий (1k, 10k и 100k)
        SetupURPTextureForResolution("1k");
        SetupURPTextureForResolution("10k");
        SetupURPTextureForResolution("100k");
        
        // Настраиваем нормалмапы для всех версий
        SetupNormalMaps();
    }

    private void SetupURPTextureForResolution(string resolution)
    {
        string urpPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}_urp.png";

        TextureImporter importer = AssetImporter.GetAtPath(urpPath) as TextureImporter;
        if (importer != null)
        {
            // Настраиваем как Linear для metallic/smoothness
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
            
            Debug.Log($"Настроена URP текстура: {objectName}_{resolution}_urp.png (Linear)");
        }
        else
        {
            Debug.LogWarning($"Не удалось найти URP текстуру {resolution}: {urpPath}");
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

    private void SetupIconTexture()
    {
        string iconPath = $"Assets/Meshes/{objectName}/{objectName}_100k_icon.png";
        
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
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
            
            Debug.Log($"Иконка настроена как Sprite 512x512: {objectName}_100k_icon.png");
        }
        else
        {
            Debug.LogWarning($"Не удалось найти иконку: {iconPath}");
        }
    }

    private void SetupFBXImportSettings()
    {
        Debug.Log("=== Настройка FBX импорта ===");
        
        // Настраиваем все модели (1k, 10k и 100k)
        SetupFBXForResolution("1k");
        SetupFBXForResolution("10k");
        SetupFBXForResolution("100k");
        
        Debug.Log("=== Настройка FBX импорта завершена ===");
    }

    private void SetupFBXForResolution(string resolution)
    {
        string fbxPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.fbx";
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
            
            // Принудительно обновляем настройки после реимпорта
            AssetDatabase.Refresh();
            
            // Небольшая задержка для завершения реимпорта
            System.Threading.Thread.Sleep(100);
            
            // Проверяем, что настройки применились
            ModelImporter reimportedImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (reimportedImporter != null)
            {
                Debug.Log($"FBX импорт настроен для {objectName}_{resolution}.fbx");
                Debug.Log($"  Convert Units: {!reimportedImporter.useFileUnits}");
                Debug.Log($"  Scale Factor: {reimportedImporter.globalScale}");
                
                // Если настройки не применились, применяем еще раз
                if (reimportedImporter.useFileUnits != false)
                {
                    Debug.LogWarning($"Convert Units не применился, применяем еще раз...");
                    reimportedImporter.useFileUnits = false;
                    reimportedImporter.globalScale = 1f;
                    reimportedImporter.SaveAndReimport();
                }
            }
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
        string materialPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.mat";
        
        // Создаем материал со стандартным URP Lit шейдером
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = $"{objectName}_{resolution}";

        // Загружаем текстуры для конкретной детализации
        string texturePath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}_texture.png";
        string normalPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}_normal.png";
        string urpPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}_urp.png";

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
            material.SetFloat("_SmoothnessTextureChannel", 1); // 1 = Alpha channel (Metallic Alpha)
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
        string fbxPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.fbx";
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
    
    private void SetupNormalMaps()
    {
        // Настраиваем нормалмапы для всех версий (1k, 10k, 100k)
        SetupNormalMapForResolution("1k");
        SetupNormalMapForResolution("10k");
        SetupNormalMapForResolution("100k");
    }
    
    private void SetupNormalMapForResolution(string resolution)
    {
        string normalPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}_normal.png";
        
        TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (importer != null)
        {
            // Настраиваем как Normal Map
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            
            Debug.Log($"Нормалмапа настроена: {objectName}_{resolution}_normal.png (NormalMap)");
        }
        else
        {
            Debug.LogWarning($"Не удалось найти нормалмапу {resolution}: {normalPath}");
        }
    }
    
    private void CreatePrefabWithMeshes()
    {
        // Создаем корневой GameObject для префаба
        GameObject rootPrefab = new GameObject($"prefab_{objectName}");
        
        // Добавляем Rigidbody на корень
        Rigidbody rb = rootPrefab.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        
        // Добавляем компонент io_base
        var ioBaseType = System.Type.GetType("io_base, Assembly-CSharp");
        if (ioBaseType != null)
        {
            rootPrefab.AddComponent(ioBaseType);
            Debug.Log($"Компонент io_base добавлен на префаб {objectName}");
        }
        else
        {
            Debug.LogWarning($"Компонент io_base не найден для префаба {objectName}");
        }
        
        // Создаем контейнер для мешей
        GameObject modelContainer = new GameObject("Model");
        modelContainer.transform.SetParent(rootPrefab.transform);
        modelContainer.transform.localPosition = Vector3.zero;
        modelContainer.transform.localRotation = Quaternion.identity;
        modelContainer.transform.localScale = Vector3.one;
        
        // Создаем контейнер для клеток
        GameObject cellsContainer = new GameObject("Cells");
        cellsContainer.transform.SetParent(rootPrefab.transform);
        cellsContainer.transform.localPosition = Vector3.zero;
        cellsContainer.transform.localRotation = Quaternion.identity;
        cellsContainer.transform.localScale = Vector3.one;
        
        // Создаем дочерние объекты для каждого меша в контейнере
        CreateMeshChild(modelContainer, "1k", false);
        CreateMeshChild(modelContainer, "10k", false);
        CreateMeshChild(modelContainer, "100k", true); // Только 100k активен
        
        // Добавляем GeneratedModel на Model контейнер
        AddGeneratedModelToContainer(modelContainer, cellsContainer);
        
        // Сохраняем префаб
        string prefabPath = $"Assets/Meshes/{objectName}/prefab_{objectName}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootPrefab, prefabPath);
        
        // Удаляем временный объект из сцены
        Object.DestroyImmediate(rootPrefab);
        
        Debug.Log($"✅ Префаб создан: {prefabPath}");
    }
    
    private void CreateMeshChild(GameObject parent, string resolution, bool isActive)
    {
        // Загружаем FBX модель
        string fbxPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.fbx";
        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        
        if (fbxModel != null)
        {
            // Создаем дочерний объект
            GameObject child = new GameObject($"{objectName}_{resolution}");
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            
            // Копируем MeshRenderer и MeshFilter из FBX
            MeshRenderer originalRenderer = fbxModel.GetComponent<MeshRenderer>();
            MeshFilter originalFilter = fbxModel.GetComponent<MeshFilter>();
            
            if (originalRenderer != null && originalFilter != null)
            {
                MeshRenderer newRenderer = child.AddComponent<MeshRenderer>();
                MeshFilter newFilter = child.AddComponent<MeshFilter>();
                
                // Копируем меш
                newFilter.sharedMesh = originalFilter.sharedMesh;
                
                // Назначаем материал
                string materialPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    newRenderer.material = material;
                    Debug.Log($"Материал назначен на {resolution}: {material.name}");
                }
                else
                {
                    Debug.LogWarning($"Материал не найден для {resolution}: {materialPath}");
                }
                

                
                // Сериализуем MeshRenderer
                SerializedObject serializedRenderer = new SerializedObject(newRenderer);
                serializedRenderer.Update();
                serializedRenderer.ApplyModifiedProperties();
                
                Debug.Log($"GeneratedModel добавлен на меш {resolution}");
            }
            
            // Устанавливаем активность
            child.SetActive(isActive);
            
            Debug.Log($"Создан меш {resolution} (активен: {isActive})");
        }
        else
        {
            Debug.LogError($"Не удалось загрузить FBX модель: {fbxPath}");
        }
    }
    
    private void AddGeneratedModelToContainer(GameObject modelContainer, GameObject cellsContainer)
    {
        // Добавляем GeneratedModel на Model контейнер
        var generatedModel = modelContainer.AddComponent<GeneratedModel>();
        
        // Устанавливаем параметры GeneratedModel напрямую
        generatedModel.modelName = objectName;
        generatedModel.resolution = "100k"; // Основное разрешение
        generatedModel.cellsContainer = cellsContainer;
        
        // Получаем все MeshRenderer из дочерних объектов
        MeshRenderer[] meshRenderers = modelContainer.GetComponentsInChildren<MeshRenderer>();
        generatedModel.meshRenderers = meshRenderers;
        
        Debug.Log($"GeneratedModel добавлен на Model контейнер");
        Debug.Log($"  modelName: {objectName}");
        Debug.Log($"  resolution: 100k");
        Debug.Log($"  cellsContainer: {cellsContainer.name}");
        Debug.Log($"  meshRenderers: {meshRenderers.Length} рендереров");
        
        // Принудительно вызываем инициализацию
        generatedModel.ini();
    }
    
    private void CreateScriptableObject()
    {
        // Создаем ScriptableObject напрямую
        item_SO itemSO = ScriptableObject.CreateInstance<item_SO>();
        
        // Загружаем иконку
        string iconPath = $"Assets/Meshes/{objectName}/{objectName}_100k_icon.png";
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        
        // Загружаем префаб
        string prefabPath = $"Assets/Meshes/{objectName}/prefab_{objectName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        // Назначаем значения напрямую
        if (icon != null)
        {
            itemSO.icon = icon;
            Debug.Log($"Иконка назначена в item_SO: {icon.name}");
        }
        else
        {
            Debug.LogWarning($"Иконка не найдена: {iconPath}");
        }
        
        if (prefab != null)
        {
            // Получаем компонент io_base из префаба
            var ioBaseComponent = prefab.GetComponent<io_base>();
            if (ioBaseComponent != null)
            {
                itemSO.prefab = ioBaseComponent;
                Debug.Log($"io_base компонент назначен в item_SO: {ioBaseComponent.name}");
            }
            else
            {
                Debug.LogWarning($"Компонент io_base не найден в префабе {prefab.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Префаб не найден: {prefabPath}");
        }
        
        // Устанавливаем заголовок
        itemSO.Title = objectName;
        Debug.Log($"Заголовок установлен: {objectName}");
        
        // Сохраняем ScriptableObject
        string soPath = $"Assets/Meshes/{objectName}/item_SO_{objectName}.asset";
        AssetDatabase.CreateAsset(itemSO, soPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ ScriptableObject создан: {soPath}");
    }
    
    private void AddGeneratedModelScripts()
    {
        // Добавляем скрипт GeneratedModel на все модели (1k, 10k, 100k)
        AddGeneratedModelScriptForResolution("1k");
        AddGeneratedModelScriptForResolution("10k");
        AddGeneratedModelScriptForResolution("100k");
    }
    
    private void AddGeneratedModelScriptForResolution(string resolution)
    {
        string fbxPath = $"Assets/Meshes/{objectName}/{objectName}_{resolution}.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        
        if (prefab != null)
        {
            // Проверяем, есть ли уже скрипт GeneratedModel
            var existingScript = prefab.GetComponent<GeneratedModel>();
            if (existingScript == null)
            {
                // Добавляем скрипт напрямую
                var script = prefab.AddComponent<GeneratedModel>();
                
                // Устанавливаем параметры через SerializedObject
                SerializedObject serializedScript = new SerializedObject(script);
                var modelNameProperty = serializedScript.FindProperty("modelName");
                var resolutionProperty = serializedScript.FindProperty("resolution");
                
                if (modelNameProperty != null)
                {
                    modelNameProperty.stringValue = objectName;
                    Debug.Log($"Установлено modelName: {objectName}");
                }
                
                if (resolutionProperty != null)
                {
                    resolutionProperty.stringValue = resolution;
                    Debug.Log($"Установлено resolution: {resolution}");
                }
                
                serializedScript.ApplyModifiedProperties();
                
                // Сохраняем изменения
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"✅ Скрипт GeneratedModel успешно добавлен на {objectName}_{resolution}");
            }
            else
            {
                Debug.Log($"Скрипт GeneratedModel уже существует на {objectName}_{resolution}");
            }
        }
        else
        {
            Debug.LogError($"Не удалось загрузить префаб: {fbxPath}");
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
