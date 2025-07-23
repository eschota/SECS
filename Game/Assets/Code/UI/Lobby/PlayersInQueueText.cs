using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class PlayersInQueueText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI queueText;
    
    void Start()
    {
        if (queueText == null)
            queueText = GetComponent<TextMeshProUGUI>();
        
        InvokeRepeating("UpdateQueueCount", 0f, 5f);
    }
    
    void UpdateQueueCount()
    {
        if (Lobby.Instance != null && Lobby.Instance.GetPlayerStatus() == Lobby.PlayerStatus.Idle)
        {
            StartCoroutine(GetQueueData());
        }
    }
    
    IEnumerator GetQueueData()
    {
        UnityWebRequest request = UnityWebRequest.Get(main.QueueUrl + "stats");
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        request.certificateHandler = new BypassCertificate();
        #endif
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            int playerCount = CountPlayers(response);
            queueText.text = $"Players in queue: {playerCount}";
        }
        else
        {
            queueText.text = "Queue: offline";
        }
    }
    
    int CountPlayers(string json)
    {
        try
        {
            // Parse the JSON response from /stats endpoint
            var statsResponse = JsonUtility.FromJson<QueueStatsResponse>(json);
            return statsResponse.total;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayersInQueueText] Failed to parse queue stats: {ex.Message}. JSON: {json}");
            return 0;
        }
    }
    
    [System.Serializable]
    public class QueueStatsResponse
    {
        public int oneVsOne;
        public int twoVsTwo;
        public int fourPlayerFFA;
        public int total;
    }
    
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
