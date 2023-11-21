using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static ServerSearcher;

public class NetworkGameState : NetworkBehaviour
{

    [NonSerialized] public NetworkVariable<int> targetLocationIndex = new(0);
    [NonSerialized] public TargetLocation targetLocation;
    [NonSerialized] public NetworkVariable<bool> isInGame = new(false);

    private BDama firstBDama;

    void Awake()
    {
        targetLocationIndex.OnValueChanged += TargetLocationIndex_OnValueChanged;
        isInGame.OnValueChanged += IsInGame_OnValueChanged;
    }

    private void IsInGame_OnValueChanged(bool previousValue, bool newValue)
    {
        GameManager.Instance.goal.SetActive(newValue);
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
    /// </summary>
    /// <param name="bdama"></param>
    internal void OnGoal(BDama bdama)
    {
        if (!isInGame.Value)
        {
            Debug.Log("ゲーム中ではありません。");
            return;
        }

        if (firstBDama != null)
        {
            Debug.Log("すでにゴールしたビー玉がいるため、無視します。");
            return;
        }

        // そいつが初めてのゴールなら、スコアを加算する。
        firstBDama = bdama;
        bdama.player.score.Value++;

        // 全クライアントにゴール処理を通知する。
        isInGame.Value = false;
        OnGoalClientRpc(bdama.OwnerClientId);

        // ある程度たったら次のステージを開始する。
        StartCoroutine(WaitAndStartStage(bdama));
    }
    IEnumerator WaitAndStartStage(BDama winner)
    {
        yield return new WaitForSeconds(3f + 0.5f);
        // 全ビー玉を現在のゴールに集める。
        for (int i = 0; i < GameManager.Instance.bdamas.Count; i++)
        {
            var b = GameManager.Instance.bdamas[i];
            if (winner == b) continue;
            b.rb.velocity = Vector3.zero;
            b.transform.position = winner.transform.position + new Vector3(
                UnityEngine.Random.value * 100,
                200 + UnityEngine.Random.value * 100,
                UnityEngine.Random.value * 100);
        }

        // ステージを開始する。
        yield return new WaitForSeconds(3f);
        OnStageStart();
    }

    [ClientRpc]
    private void OnGoalClientRpc(ulong winnerClientId)
    {
        StartCoroutine(OnGoalCoroutine(winnerClientId));
    }

    private IEnumerator OnGoalCoroutine(ulong winnerClientId)
    {
        var ui = UIManager.Instance;
        // ゴール表示を行う。
        var isWinner = GameManager.Instance.LocalPlayer.OwnerClientId == winnerClientId;
        ui.textGoalPlayer.color = ui.playerColors[GameManager.Instance.FindPlayer(winnerClientId).colorIndex.Value].color;
        ui.textGoalPlayerCongraturation.color = ui.textGoalPlayer.color;
        ui.panelGoal.SetActive(true);
        ui.textGoalPlayerCongraturation.gameObject.SetActive(isWinner);
        // 少し待つ。
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            ui.panelGoal.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            ui.panelGoal.SetActive(true);
        }
        yield return new WaitForSeconds(3.5f);
        ui.panelGoal.SetActive(false);

        // 次の目的地を表示する。
        ui.textNextDestination.gameObject.SetActive(false);
        ui.textNextDestinationType.gameObject.SetActive(false);
        ui.panelNext.SetActive(true);
        // あとはサーバー側の通知に任せる。
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
        isInGame.Value = true;
        firstBDama = null;
    }

    private int goalAnimationId = 0;

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
        var currentGoalAnimationId = ++goalAnimationId;
        Debug.Log($"TargetLocationIndex_OnValueChanged {prevIndex} -> {newIndex}");
        var target = TargetLocation.Data[newIndex];
        targetLocation = target;
        UIManager.Instance.UpdateTargetLocation(targetLocation);
        GameManager.Instance.goal.SetActive(true);
        GameManager.Instance.goal.transform.position = targetLocation.position;

        // 次の目的地を表示する。
        var ui = UIManager.Instance;
        ui.textNextDestination.gameObject.SetActive(true);
        ui.textNextDestinationType.gameObject.SetActive(true);
        ui.panelNext.SetActive(true);
        ui.textNextDestination.text = targetLocation.name;
        ui.textNextDestinationType.text = $"({targetLocation.type})";
        StartCoroutine(WaitAndHidePanel());
        IEnumerator WaitAndHidePanel()
        {
            yield return new WaitForSeconds(4.0f);
            if (currentGoalAnimationId != goalAnimationId) yield break;
            ui.panelNext.SetActive(false);
        }
    }

}
