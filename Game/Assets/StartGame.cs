using UnityEngine;

public class StartGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "main")
    {
        // Удаляем все объекты на сцене, кроме этого скрипта
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != this.gameObject)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        
        // Загружаем сцену main
        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
