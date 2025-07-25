#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class FileWatcherTrigger
{
    private static FileSystemWatcher watcher;
    private static readonly string ProjectRootPath = Path.GetDirectoryName(Application.dataPath);
    private static bool hasTriggered = false;

    static FileWatcherTrigger()
    {
        InitializeFileWatcher();
        Debug.Log("FileWatcherTrigger initialized. Watching for flag files in project root...");
    }

    private static void InitializeFileWatcher()
    {
        try
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }

            watcher = new FileSystemWatcher(ProjectRootPath);
            watcher.Filter = "*.flag";
            watcher.Created += OnFlagFileCreated;
            watcher.EnableRaisingEvents = true;

            Debug.Log($"FileWatcher initialized for directory: {ProjectRootPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize FileWatcher: {e.Message}");
        }
    }

    private static void OnFlagFileCreated(object sender, FileSystemEventArgs e)
    {
        // Выполняем обработку в главном потоке Unity
        EditorApplication.delayCall += () => ProcessFlagFile(e.FullPath);
    }

    private static void ProcessFlagFile(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            Debug.Log($"Flag file detected: {fileName}");

            if (fileName == "play.flag" && !EditorApplication.isPlaying && !hasTriggered)
            {
                hasTriggered = true;
                File.Delete(filePath);
                Debug.Log("Play triggered from file watcher. Starting Play Mode...");
                EditorApplication.EnterPlaymode();
            }
            else if (fileName == "stop_play.flag" && EditorApplication.isPlaying)
            {
                File.Delete(filePath);
                Debug.Log("Stop triggered from file watcher. Stopping Play Mode...");
                EditorApplication.ExitPlaymode();
            }
            else if (fileName == "recompile.flag" && !EditorApplication.isPlaying)
            {
                File.Delete(filePath);
                Debug.Log("Recompile triggered from file watcher. Forcing script reimport...");
                ForceRecompile();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing flag file: {e.Message}");
        }
    }

    private static void ForceRecompile()
    {
        try
        {
            AssetDatabase.ImportAsset("Assets", ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("Scripts reimported successfully via file watcher.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during recompile: {e.Message}");
        }
    }

    private static void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.Dispose();
            watcher = null;
        }
    }
}
#endif 