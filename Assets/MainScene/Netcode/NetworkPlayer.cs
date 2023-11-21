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

    [NonSerialized] public WatchMode watchMode = WatchMode.None;
    [NonSerialized] public int watchPlayerIndex = 0;
    public enum WatchMode
    {
        None,
        DividedDisplay,
        FreeCamera,
        Player,
    }

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

    [ServerRpc]
    public void StartWatchServerRpc()
    {
        if (mode.Value != NetworkPlayerMode.None)
        {
            Debug.LogWarning($"観戦モード開始できません ({mode.Value})");
            return;
        }
        mode.Value = NetworkPlayerMode.Watch;
    }

    public void EndWatch()
    {
        GameManager.Instance.vcam.enabled = true;
        EndWatchServerRpc();
        GameManager.Instance.UpdateDividedDisplay();
    }

    [ServerRpc]
    private void EndWatchServerRpc()
    {
        if (mode.Value != NetworkPlayerMode.Watch)
        {
            Debug.LogWarning("観戦モードではありません");
            return;
        }
        mode.Value = NetworkPlayerMode.None;
    }

    private void ColorIndex_OnValueChanged(int previousValue, int newValue)
    {
        var mat = UIManager.Instance.playerColors[colorIndex.Value];
        foreach (var bdama in BDamas)
        {
            bdama.SetMaterial(mat);
        }
        GameManager.Instance.UpdateDividedDisplay();
    }

    internal void OnBDamaSpawned(BDama bdama)
    {
        var mat = UIManager.Instance.playerColors[colorIndex.Value];
        bdama.SetMaterial(mat);
    }

    internal void WatchPlayer(int index)
    {
        watchMode = WatchMode.Player;
        watchPlayerIndex = index;
        GameManager.Instance.UpdateDividedDisplay();
        GameManager.Instance.vcam.enabled = true;
        GameManager.Instance.UpdateWatchPlayer();
    }

    internal void WatchByDividedDisplay()
    {
        watchMode = WatchMode.DividedDisplay;
        GameManager.Instance.vcam.enabled = true;
        GameManager.Instance.UpdateDividedDisplay();
    }

    internal void WatchByFreeCamera()
    {
        watchMode = WatchMode.FreeCamera;
        GameManager.Instance.UpdateDividedDisplay();
        GameManager.Instance.vcam.enabled = false;

    }
}

public enum NetworkPlayerMode : byte
{
    None = 0,
    BDama,
    Watch,
}