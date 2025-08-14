using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrefabEditEnvironmentLoader
{
    // Путь к сцене фона в папке Editor
    private const string EnvironmentScenePath = "Assets/Editor/PrefabEditEnvironment.unity";

    static PrefabEditEnvironmentLoader()
    {
        // Подписываемся на событие открытия префаба
        UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageOpened;
    }

    private static void OnPrefabStageOpened(UnityEditor.SceneManagement.PrefabStage stage)
    {
        // Проверяем, что сцена существует
        if (System.IO.File.Exists(EnvironmentScenePath))
        {
            // Загружаем сцену как additive, чтобы оставить префаб для редактирования
            EditorSceneManager.OpenScene(EnvironmentScenePath, OpenSceneMode.Additive);
        }
        else
        {
            Debug.LogWarning($"Prefab Edit Environment scene not found at {EnvironmentScenePath}");
        }
    }
}
