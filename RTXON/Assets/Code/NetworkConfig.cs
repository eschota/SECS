using UnityEngine;

public class NetworkConfig : MonoBehaviour
{
    void Awake()
    {
        // Разрешаем HTTP соединения для тестирования
        #if UNITY_EDITOR
        Application.runInBackground = true;
        #endif
        
        // Настройки для WebGL
        #if UNITY_WEBGL
        Application.runInBackground = true;
        #endif
    }
}
