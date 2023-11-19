using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static string TAG { get; } = "[GM]";
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool needLoadMapDataScene = true;
    [SerializeField] private AssetReference refMapDataScene;

    [SerializeField] public CinemachineVirtualCamera vcam;
    private CinemachineTransposer transposer;

    [SerializeField] private NetworkGameState networkGameStatePrefab;
    private NetworkGameState state;

    [SerializeField] public GameObject goal;

    [Header("デバッグ用")]
    [SerializeField] private List<NetworkPlayer> players = new();
    [field: SerializeField] public NetworkPlayer LocalPlayer { get; private set; }
    [SerializeField] public List<BDama> bdamas = new();
    [field: SerializeField] public BDama PlayerBdama { get; private set; }

    private void Awake()
    {
        Instance = this;

        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();

        // 必要ならマップデータシーンを読み込む。
        if (needLoadMapDataScene)
        {
            Addressables.LoadSceneAsync(refMapDataScene, LoadSceneMode.Additive);
        }

        StartCoroutine(UpdateDistanceLoop());
    }

    /// <summary>
    /// サーバー専用
    /// サーバー開始時に呼ばれます。
    /// （サーバー用プレーヤーのspawnが終わってから呼ばれます）
    /// </summary>
    public void OnServerStarted()
    {
        Debug.Log($"{TAG} Server Started!");
        // 全プレーヤーで共有するゲーム状態を生成する。
        var state  = Instantiate(networkGameStatePrefab);
        state.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// サバクラ両用
    /// ゲーム状態が生成され、ネットワーク同期済みになったときに呼ばれます。
    /// </summary>
    public void OnNetworkGameStateSpawned(NetworkGameState state)
    {
        this.state = state;
    }

    public void OnPlayerSpawned(NetworkPlayer player)
    {
        players.Add(player);
        if (player.IsLocalPlayer)
        {
            Debug.Log($"{TAG} Player Spawned! (Local)");
            LocalPlayer = player;
            UIManager.Instance.UpdateLocalPlayerColor();
        }
        else
        {
            Debug.Log($"{TAG} Player Spawned! (Remote)");
        }
    }

    public void OnPlayerDespawned(NetworkPlayer player)
    {
        Debug.Log($"{TAG} Player Despawned!");
        players.Remove(player);
        if (player.IsLocalPlayer)
        {
            LocalPlayer = null;
        }
    }

    public void OnBDamaSpawned(BDama bdama)
    {
        Debug.Log($"{TAG} BDama Spawned!");
        bdama.player = FindPlayer(bdama.OwnerClientId);
        bdamas.Add(bdama);
        if (bdama.IsOwner)
        {
            PlayerBdama = bdama;
            vcam.Follow = PlayerBdama.transform;
        }
        else
        {
            if (LocalPlayer.mode.Value == NetworkPlayerMode.Watch &&
                LocalPlayer.watchMode == NetworkPlayer.WatchMode.Player)
            {
                UpdateWatchPlayer();
            }
        }
    }

    public void OnBDamaDespawned(BDama bdama)
    {
        Debug.Log($"{TAG} BDama Despawned!");
        bdamas.Remove(bdama);
        if (bdama == PlayerBdama)
        {
            PlayerBdama = null;
            vcam.Follow = null;
        }
        else if (vcam.Follow == bdama)
        {
            if (LocalPlayer.mode.Value == NetworkPlayerMode.Watch &&
                LocalPlayer.watchMode == NetworkPlayer.WatchMode.Player)
            {
                UpdateWatchPlayer();
            }
        }
    }

    internal void UpdateWatchPlayer()
    {
        if (LocalPlayer.watchPlayerIndex <= bdamas.Count)
        {
            vcam.Follow = bdamas[LocalPlayer.watchPlayerIndex].transform;
        }
    }

    public BDama FindBDama(ulong ownerId) => bdamas.Find(b => b.OwnerClientId == ownerId);
    public IEnumerable<BDama> FindBDamasAll(ulong ownerId) => bdamas.Where(b => b.OwnerClientId == ownerId);
    public NetworkPlayer FindPlayer(ulong ownerId) => players.Find(b => b.OwnerClientId == ownerId);
    public IEnumerable<NetworkPlayer> FindPlayersAll(ulong ownerId) => players.Where(b => b.OwnerClientId == ownerId);

    private IEnumerator UpdateDistanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);

            // 準備ができていないならランキング表を隠す。
            if (state == null || state.targetLocation == null)
            {
                foreach (var item in UIManager.Instance.rankingItems)
                {
                    if (item.gameObject.activeSelf) item.gameObject.SetActive(false);
                }
                continue;
            }

            // 順位表を作る。
            var targetLocation = state.targetLocation;
            var orderedBdamas = bdamas.Select(bdama => {
                var bdamaXZ = new Vector3(bdama.transform.position.x, 0, bdama.transform.position.z);
                var targetXZ = new Vector3(targetLocation.position.x, 0, targetLocation.position.z);
                var distance = Vector3.Distance(bdamaXZ, targetXZ);

                var direction = targetXZ - bdamaXZ;
                var angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
                return new { bdama, distance, angle, };
            }).OrderBy(x => x.distance).ToArray();

            var items = UIManager.Instance.rankingItems;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (orderedBdamas.Length <= i)
                {
                    if (item.gameObject.activeSelf) item.gameObject.SetActive(false);
                    continue;
                }
                if (!item.gameObject.activeSelf) item.gameObject.SetActive(true);
                var rankItem = orderedBdamas[i];
                var bdama = rankItem.bdama;
                var distance = rankItem.distance;
                var angle = rankItem.angle;
                item.UpdateUI(i, bdama, distance);
                if (bdama == PlayerBdama)
                {
                    var START = -22.5f;
                    var DIFF = 360 / 8;
                    var text = angle.ToString("N0");
                    if (angle > START + DIFF * 0 && angle < START + DIFF * 1) text = "北";
                    else if (angle > START + DIFF * 1 && angle < START + DIFF * 2) text = "北東";
                    else if (angle > START + DIFF * 2 && angle < START + DIFF * 3) text = "東";
                    else if (angle > START + DIFF * 3 && angle < START + DIFF * 4) text = "南東";
                    else if (angle > START + DIFF * 4 && angle < START + DIFF * 5) text = "南";
                    else if (angle + 360 > START + DIFF * 4 && angle + 360 < START + DIFF * 5) text = "南";
                    else if (angle + 360 > START + DIFF * 5 && angle + 360 < START + DIFF * 6) text = "南西";
                    else if (angle + 360 > START + DIFF * 6 && angle + 360 < START + DIFF * 7) text = "西";
                    else if (angle + 360 > START + DIFF * 7 && angle + 360 < START + DIFF * 8) text = "北西";

                    UIManager.Instance.UpdateDistance(distance, text, angle);
                }
            }
        }
    }

    internal void OnGoal()
    {
        // TODO
        state.OnStageStart();
    }

    internal void ResetVelocity()
    {
        Debug.Log("Reset Velocity!");
        PlayerBdama.rb.velocity = Vector3.zero;
    }
}
