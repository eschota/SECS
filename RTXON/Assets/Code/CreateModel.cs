using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class CreateModel : MonoBehaviour
{
    [SerializeField] TMP_InputField prompt;
    [SerializeField] Button buttonStart;
    [SerializeField] GameObject model;
    [SerializeField] Button[] previewButtons;

    private string[] output_url;
    private Dictionary<int, bool> imageDownloaded;
    private Dictionary<int, Coroutine> downloadCoroutines;

    void Start()
    {
        buttonStart.onClick.AddListener(StartCreateModel);
        foreach (Button button in previewButtons)
        {
            button.onClick.AddListener(() => PreviewModel(button));
        }
        output_url = new string[previewButtons.Length];
        imageDownloaded = new Dictionary<int, bool>();
        downloadCoroutines = new Dictionary<int, Coroutine>();
        
        // Разрешаем HTTP соединения для тестирования
        #if UNITY_EDITOR
        Application.runInBackground = true;
        #endif
    }

    void StartCreateModel()
    {
        Debug.Log("StartCreateModel");
        StartCoroutine(SendCreateRequest());
    }

    IEnumerator SendCreateRequest()
    {
        string promptText = prompt.text;
        string url = "https://renderfin.com/api-render";
        Debug.Log(url);

        // Останавливаем предыдущие корутины скачивания
        foreach (var coroutine in downloadCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        downloadCoroutines.Clear();
        imageDownloaded.Clear();

        // Отправляем 5 разных запросов асинхронно
        for (int i = 0; i < previewButtons.Length; i++)
        {
            StartCoroutine(SendSingleRequest(i, promptText, url));
        }
        
        yield break; // Явно завершаем корутину
    }

    IEnumerator SendSingleRequest(int index, string promptText, string url)
    {
        // Создаем JSON для отправки с шаблоном промпта
        string template = "A high-resolution, isolated studio photograph of %model% on a pure white background. The %model% is positioned in a precise side-view perspective, well-lit with soft, diffused lighting to eliminate harsh shadows. The image is highly detailed, capturing every texture and material with precision. The composition ensures that %model% is the sole subject, with no distractions, making it ideal for product visualization, catalogs, or design references. Very Sharp, PURE FOCUS.";
        
        var requestData = new RequestData
        {
            prompt = template.Replace("%model%", promptText)
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "application/json"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            
            // Добавляем заголовки для HTTP
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"Response {index}: " + response);
                ParseSingleResponse(index, response);
            }
            else
            {
                Debug.LogError($"Error {index}: " + www.error);
            }
        }
    }

    void ParseSingleResponse(int index, string response)
    {
        try
        {
            // Парсим ответ от API для одного изображения
            var apiResponse = JsonUtility.FromJson<ApiResponse>(response);
            
            if (apiResponse != null && !string.IsNullOrEmpty(apiResponse.output_url))
            {
                output_url[index] = apiResponse.output_url;
                imageDownloaded[index] = false;
                downloadCoroutines[index] = StartCoroutine(DownloadImageWithRetry(index));
                
                Debug.Log($"Request {index} returned image URL: {apiResponse.output_url}");
            }
            else
            {
                Debug.LogWarning($"Request {index} response is null or missing output_url");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse API response {index}: {e.Message}");
            Debug.LogError($"Raw response {index}: {response}");
        }
    }

    IEnumerator DownloadImageWithRetry(int index)
    {
        float checkInterval = 5f ; // 7.5 секунд между проверками
        
        while (!imageDownloaded[index])
        {
            yield return new WaitForSeconds(checkInterval);
            
            if (!string.IsNullOrEmpty(output_url[index]))
            {
                yield return StartCoroutine(DownloadImage(index));
            }
        }
    }

    IEnumerator DownloadImage(int index)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(output_url[index]))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                
                previewButtons[index].image.sprite = sprite;
                imageDownloaded[index] = true;
                
                Debug.Log($"Image {index} downloaded successfully");
                
                // Останавливаем корутину для этого изображения
                if (downloadCoroutines.ContainsKey(index))
                {
                    StopCoroutine(downloadCoroutines[index]);
                    downloadCoroutines.Remove(index);
                }
            }
            else
            {
                Debug.LogWarning($"Failed to download image {index}: {www.error}");
            }
        }
    }

    void PreviewModel(Button button)
    {
        int index = System.Array.IndexOf(previewButtons, button);
        Debug.Log($"PreviewModel for button {index}");
        
        if (index >= 0 && index < output_url.Length)
        {
            Debug.Log($"URL for button {index}: {output_url[index]}");
        }
    }

    void Update()
    {
        // Можно добавить дополнительную логику если нужно
    }

    // Классы для сериализации JSON
    [System.Serializable]
    public class RequestData
    {
        public string prompt;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string output_url;
    }
}