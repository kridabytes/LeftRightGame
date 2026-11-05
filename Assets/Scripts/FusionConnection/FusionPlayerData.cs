using Fusion;
using UnityEngine;

public class FusionPlayerData : NetworkBehaviour
{
    [Networked, Capacity(24)]
    public string PlayerName { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(PlayerPrefs.GetString("UserName", "Player"));
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_SetPlayerName(string playerName)
    {
        PlayerName = playerName;
    }
}
