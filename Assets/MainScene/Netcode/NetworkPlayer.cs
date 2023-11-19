using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private BDama bdamaPrefab;
    [NonSerialized] public NetworkVariable<int> score = new(0);
    [NonSerialized] public NetworkVariable<int> colorIndex = new(0, writePerm: NetworkVariableWritePermission.Owner);

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

    public void StartBDamaGame()
    {
        CreateBDamaServerRpc();
    }

    [ServerRpc]
    public void CreateBDamaServerRpc(ServerRpcParams rpcParams = default)
    {
        var bdama = Instantiate(bdamaPrefab);
        bdama.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
    }

    private void ColorIndex_OnValueChanged(int previousValue, int newValue)
    {
        var mat = UIManager.Instance.playerColors[colorIndex.Value];
        foreach (var bdama in GameManager.Instance.FindBDamasAll(OwnerClientId))
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
