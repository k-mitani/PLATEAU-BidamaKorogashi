using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

    /// <summary>
    /// サバクラ両用
    /// サーバーなら、ステート生成時
    /// クライアントなら、接続完了時に呼ばれます。
    /// (この時点で、NetworkVariableはもう同期済み)
    /// </summary>
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnNetworkGameStateSpawned(this);
        if (IsServer)
        {
            OnStageStart();
        }
        else
        {
            TargetLocationIndex_OnValueChanged(0, targetLocationIndex.Value);
        }
    }

    /// <summary>
    /// サーバー専用
    /// ステージを開始します。
    /// </summary>
    public void OnStageStart()
    {
        var newIndex = UnityEngine.Random.Range(0, TargetLocation.Data.Count);
        while (newIndex == targetLocationIndex.Value)
        {
            newIndex = UnityEngine.Random.Range(0, TargetLocation.Data.Count);
        }
        Debug.Log($"OnStageStart {targetLocationIndex.Value} -> {newIndex}");
        targetLocationIndex.Value = newIndex;

    }

    /// <summary>
    /// サバクラ両用
    /// 目的地が変更されたときに呼ばれます。
    /// 
    /// サーバーの場合は、自身が目的地をセットしたとき
    /// クライアントの場合は、サーバーが目的地をセットしたときに呼ばれます。
    /// </summary>
    /// <param name="prevIndex"></param>
    /// <param name="newIndex"></param>
    private void TargetLocationIndex_OnValueChanged(int prevIndex, int newIndex)
    {
        Debug.Log($"TargetLocationIndex_OnValueChanged {prevIndex} -> {newIndex}");
        var target = TargetLocation.Data[newIndex];
        targetLocation = target;
        UIManager.Instance.UpdateTargetLocation(targetLocation);
        GameManager.Instance.goal.transform.position = targetLocation.position;
    }
}
