using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private BDama bdamaPrefab;
    [NonSerialized] public NetworkVariable<int> score = new(0);
    [NonSerialized] public NetworkVariable<int> colorIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);
    [NonSerialized] public NetworkVariable<NetworkPlayerMode> mode = new(NetworkPlayerMode.None);

    public IEnumerable<BDama> BDamas => GameManager.Instance.FindBDamasAll(OwnerClientId);

    private void Awake()
    {
        colorIndex.OnValueChanged += ColorIndex_OnValueChanged;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("NetworkPlayer Spawned!");
        GameManager.Instance.OnPlayerSpawned(this);
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("NetworkPlayer Bye!");
        GameManager.Instance.OnPlayerDespawned(this);
    }

    [ServerRpc]
    public void StartBDamaGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (mode.Value != NetworkPlayerMode.None)
        {
            Debug.LogWarning($"ビー玉モード開始できません ({mode.Value})");
            return;
        }
        mode.Value = NetworkPlayerMode.BDama;

        var bdama = Instantiate(bdamaPrefab);
        bdama.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    public void EndBDamaGameServerRpc()
    {
        if (mode.Value != NetworkPlayerMode.BDama)
        {
            Debug.LogWarning("ビー玉モードではありません");
            return;
        }
        mode.Value = NetworkPlayerMode.None;
        foreach (var bdama in BDamas.ToList())
        {
            bdama.GetComponent<NetworkObject>().Despawn(true);
        }
    }


    private void ColorIndex_OnValueChanged(int previousValue, int newValue)
    {
        var mat = UIManager.Instance.playerColors[colorIndex.Value];
        foreach (var bdama in BDamas)
        {
            bdama.SetMaterial(mat);
        }
    }

    internal void OnBDamaSpawned(BDama bdama)
    {
        var mat = UIManager.Instance.playerColors[colorIndex.Value];
        bdama.SetMaterial(mat);
    }
}

public enum NetworkPlayerMode : byte
{
    None = 0,
    BDama,
    Watch,
}