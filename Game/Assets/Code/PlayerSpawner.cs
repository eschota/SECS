using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player != Runner.LocalPlayer)
        {
            return;
        }

        Debug.Log($"[PlayerSpawner] PlayerJoined: {player.PlayerId}");
        
        // Проверяем что PlayerPrefab содержит MachineSpawnClient
        if (PlayerPrefab != null)
        {
            var spawnClient = PlayerPrefab.GetComponent<MachineSpawnClient>();
            if (spawnClient == null)
            {
                Debug.LogError("[PlayerSpawner] PlayerPrefab не содержит компонент MachineSpawnClient!");
            }
            else
            {
                Debug.Log("[PlayerSpawner] PlayerPrefab содержит MachineSpawnClient");
            }
        }
        else
        {
            Debug.LogError("[PlayerSpawner] PlayerPrefab is null!");
        }

        int teamId = (player.PlayerId % 2 == 0) ? 0 : 1; // 0 = left/red, 1 = right/blue
        float x = teamId == 0 ? -5f : 5f;
        Vector3 spawnPos = new Vector3(x, 1f, 0f);

        var spawnedPlayer = Runner.Spawn(
            PlayerPrefab,
            spawnPos,
            Quaternion.identity,
            inputAuthority: player
        );
        
        Debug.Log($"[PlayerSpawner] Spawned player at {spawnPos}, Object: {spawnedPlayer?.name}");
    }
}