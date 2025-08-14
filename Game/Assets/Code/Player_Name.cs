using Fusion;
using UnityEngine;

public class Player_Name : NetworkBehaviour
{
    [Networked]
    public Fusion.NetworkString<_32> NetName { get; set; }

    public string PlayerName => NetName.Value;

    public override void Spawned()
    {
        if (string.IsNullOrEmpty(NetName.Value))
        {
            NetName = $"Player_{Object.InputAuthority.PlayerId}";
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (name.Length > 32) name = name.Substring(0, 32);
        NetName = name.Trim();
    }

    public void SetPlayerName(string newName)
    {
        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(newName);
        }
    }
}


