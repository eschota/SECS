using System;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        GenerateBuildInfo();
    }

    private static void GenerateBuildInfo()
    {
        // Get current build date and time
        string buildDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        // Calculate total size of files in Assets/Code (excluding .meta)
        string assetsCodePath = Path.Combine(Application.dataPath, "Code");
        long totalBytes = 0;
        if (Directory.Exists(assetsCodePath))
        {
            var files = Directory.GetFiles(assetsCodePath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                totalBytes += new FileInfo(file).Length;
            }
        }
        float sizeMB = totalBytes / 1024f / 1024f;
        string codeSize = $"{sizeMB:0.##} MB";

        // Ensure Resources folder exists
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);

        // Write build info file
        string buildInfoPath = Path.Combine(resourcesPath, "build_info.txt");
        string content = buildDate + "|" + codeSize;
        File.WriteAllText(buildInfoPath, content);

        // Refresh AssetDatabase to include the new text asset
        AssetDatabase.Refresh();
    }
}
#endif 