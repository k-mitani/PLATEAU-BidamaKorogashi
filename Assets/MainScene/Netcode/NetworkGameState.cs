using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameState : NetworkBehaviour
{
    [NonSerialized] public NetworkVariable<int> targetLocationIndex;
    [NonSerialized] public TargetLocation targetLocation;

    void Awake()
    {
        targetLocationIndex = new NetworkVariable<int>(0);
        targetLocationIndex.OnValueChanged += TargetLocationIndex_OnValueChanged;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("NetworkGameState Spawned!");
        GameManager.Instance.OnNetworkGameStateSpawned(this);
        if (IsServer)
        {
            Debug.Log("Start Stage");
            OnStageStart();
        }
        else
        {
            TargetLocationIndex_OnValueChanged(0, targetLocationIndex.Value);
        }
    }

    public void OnStageStart()
    {
        var newIndex = UnityEngine.Random.Range(0, TargetLocation.Data.Count);
        while (newIndex == targetLocationIndex.Value)
        {
            newIndex = UnityEngine.Random.Range(0, TargetLocation.Data.Count);
        }
        targetLocationIndex.Value = newIndex;

    }

    private void TargetLocationIndex_OnValueChanged(int prevIndex, int newIndex)
    {
        var target = TargetLocation.Data[newIndex];
        targetLocation = target;
        UIManager.Instance.UpdateTargetLocation(targetLocation);
        GameManager.Instance.goal.transform.position = targetLocation.position;
    }
}
