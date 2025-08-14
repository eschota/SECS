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

        int teamId = (player.PlayerId % 2 == 0) ? 0 : 1; // 0 = left/red, 1 = right/blue
        float x = teamId == 0 ? -5f : 5f;
        Vector3 spawnPos = new Vector3(x, 1f, 0f);

        Runner.Spawn(
            PlayerPrefab,
            spawnPos,
            Quaternion.identity,
            inputAuthority: player,
            onBeforeSpawned: (runner, networkObject) =>
            {
                var movement = networkObject.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.team_id = teamId;
                }
            }
        );
    }
}