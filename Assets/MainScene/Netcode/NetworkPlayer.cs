using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private BDama bdamaPrefab;
    [NonSerialized] public NetworkVariable<int> score;
    [NonSerialized] public NetworkVariable<Color> color;

    private void Awake()
    {
        score = new NetworkVariable<int>(0);
        color = new NetworkVariable<Color>(Color.red);
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

}
