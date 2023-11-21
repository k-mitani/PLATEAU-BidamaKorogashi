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
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static string TAG { get; } = "[GM]";
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool needLoadMapDataScene = true;
    [SerializeField] private AssetReference refMapDataScene;

    [SerializeField] public CinemachineVirtualCamera vcam;
    [SerializeField] public Camera[] subcams;
    [SerializeField] public CinemachineVirtualCamera[] subvcams;

    [SerializeField] private NetworkGameState networkGameStatePrefab;
    [NonSerialized] public NetworkGameState state;

    [SerializeField] public GameObject goal;
    [SerializeField] public GameObject initialBDamaPosition;

    [Header("デバッグ用")]
    [SerializeField] public List<NetworkPlayer> players = new();
    [field: SerializeField] public NetworkPlayer LocalPlayer { get; private set; }
    [SerializeField] public List<BDama> bdamas = new();
    [field: SerializeField] public BDama PlayerBdama { get; private set; }

    private void Awake()
    {
        Instance = this;


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
        bdama.transform.position = initialBDamaPosition.transform.position + new Vector3(
            UnityEngine.Random.value * 30,
            UnityEngine.Random.value * 100,
            UnityEngine.Random.value * 30);
        bdamas.Add(bdama);
        if (bdama.IsOwner)
        {
            PlayerBdama = bdama;
            vcam.Follow = PlayerBdama.transform;
        }
        else if (LocalPlayer != null)
        {
            if (LocalPlayer.mode.Value == NetworkPlayerMode.Watch &&
                LocalPlayer.watchMode == NetworkPlayer.WatchMode.Player)
            {
                UpdateWatchPlayer();
            }
            else if (LocalPlayer.mode.Value == NetworkPlayerMode.Watch &&
                               LocalPlayer.watchMode == NetworkPlayer.WatchMode.DividedDisplay)
            {
                UpdateDividedDisplay();
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
        if (LocalPlayer.mode.Value == NetworkPlayerMode.Watch &&
            LocalPlayer.watchMode == NetworkPlayer.WatchMode.DividedDisplay)
        {
            UpdateDividedDisplay();
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

    public void OnGoal(BDama bdama)
    {
        state.OnGoal(bdama);
    }

    internal void ResetVelocity()
    {
        Debug.Log("Reset Velocity!");
        PlayerBdama.rb.velocity = Vector3.zero;
    }

    internal void UpdateDividedDisplay()
    {
        // 一旦通常状態に戻す。
        foreach (var cam in subcams) cam.gameObject.SetActive(false);
        foreach (var vcam in subvcams) vcam.gameObject.SetActive(false);
        UIManager.Instance.DividerFor2.SetActive(false);
        UIManager.Instance.DividerFor3.SetActive(false);
        UIManager.Instance.DividerFor4.SetActive(false);
        Camera.main.rect = new Rect(0, 0, 1, 1);

        void ActivateDivider(GameObject container, string name, BDama bdama)
        {
            container.SetActive(true);
            var t = container.transform.Find(name);
            if (t == null) return;
            var image = t.GetComponent<Image>();
            if (image == null) return;
            image.color = UIManager.Instance.playerColors[bdama.player.colorIndex.Value].color;
        }

        var player = LocalPlayer;
        // 通常のカメラ表示なら
        if (player.mode.Value != NetworkPlayerMode.Watch || player.watchMode != NetworkPlayer.WatchMode.DividedDisplay)
        {
            return;
        }
        // 分割表示なら
        var playerCount = bdamas.Count;
        if (playerCount == 0) return;
        if (playerCount == 1)
        {
            vcam.Follow = bdamas[0].transform;
            return;
        }
        else if (playerCount == 2)
        {
            ActivateDivider(UIManager.Instance.DividerFor2, "P1H", bdamas[0]);
            Camera.main.rect = new Rect(0, 0.5f, 1, 0.5f);
            vcam.Follow = bdamas[0].transform;
            ActivateDivider(UIManager.Instance.DividerFor2, "P2H", bdamas[1]);
            subcams[0].gameObject.SetActive(true);
            subvcams[0].gameObject.SetActive(true);
            subcams[0].rect = new Rect(0, 0, 1, 0.5f);
            subvcams[0].Follow = bdamas[1].transform;
        }
        else if (playerCount == 3)
        {
            ActivateDivider(UIManager.Instance.DividerFor3, "P1H", bdamas[0]);
            Camera.main.rect = new Rect(0, 0.5f, 1, 0.5f);
            vcam.Follow = bdamas[0].transform;

            ActivateDivider(UIManager.Instance.DividerFor3, "P2H", bdamas[1]);
            ActivateDivider(UIManager.Instance.DividerFor3, "P2V", bdamas[1]);
            subcams[0].gameObject.SetActive(true);
            subvcams[0].gameObject.SetActive(true);
            subcams[0].rect = new Rect(0, 0, 0.5f, 0.5f);
            subvcams[0].Follow = bdamas[1].transform;

            ActivateDivider(UIManager.Instance.DividerFor3, "P3H", bdamas[2]);
            ActivateDivider(UIManager.Instance.DividerFor3, "P3V", bdamas[2]);
            subcams[1].gameObject.SetActive(true);
            subvcams[1].gameObject.SetActive(true);
            subcams[1].rect = new Rect(0.5f, 0, 0.5f, 0.5f);
            subvcams[1].Follow = bdamas[2].transform;
        }
        else
        {
            ActivateDivider(UIManager.Instance.DividerFor4, "P1H", bdamas[0]);
            ActivateDivider(UIManager.Instance.DividerFor4, "P1V", bdamas[0]);
            Camera.main.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
            vcam.Follow = bdamas[0].transform;

            ActivateDivider(UIManager.Instance.DividerFor4, "P2H", bdamas[1]);
            ActivateDivider(UIManager.Instance.DividerFor4, "P2V", bdamas[1]);
            subcams[0].gameObject.SetActive(true);
            subvcams[0].gameObject.SetActive(true);
            subcams[0].rect = new Rect(0, 0, 0.5f, 0.5f);
            subvcams[0].Follow = bdamas[1].transform;

            ActivateDivider(UIManager.Instance.DividerFor4, "P3H", bdamas[2]);
            ActivateDivider(UIManager.Instance.DividerFor4, "P3V", bdamas[2]);
            subcams[1].gameObject.SetActive(true);
            subvcams[1].gameObject.SetActive(true);
            subcams[1].rect = new Rect(0.5f, 0, 0.5f, 0.5f);
            subvcams[1].Follow = bdamas[2].transform;

            ActivateDivider(UIManager.Instance.DividerFor4, "P4H", bdamas[3]);
            ActivateDivider(UIManager.Instance.DividerFor4, "P4V", bdamas[3]);
            subcams[2].gameObject.SetActive(true);
            subvcams[2].gameObject.SetActive(true);
            subcams[2].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            subvcams[2].Follow = bdamas[3].transform;
        }
    }
}
