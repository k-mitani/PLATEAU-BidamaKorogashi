using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool needLoadMapDataScene = true;
    [SerializeField] private AssetReference refMapDataScene;

    [SerializeField] CinemachineVirtualCamera vcam;
    private CinemachineTransposer transposer;

    [SerializeField] private NetworkGameState networkGameStatePrefab;
    private NetworkGameState networkGameState;

    [SerializeField] public GameObject goal;

    [NonSerialized] public float gravityAmount = 0;

    public BDama PlayerBdama { get; private set; }

    private void Awake()
    {
        Instance = this;

        // 重力の大きさを取得する。
        gravityAmount = Physics.gravity.magnitude;

        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();

        // 必要ならマップデータシーンを読み込む。
        if (needLoadMapDataScene)
        {
            Addressables.LoadSceneAsync(refMapDataScene, LoadSceneMode.Additive);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;
    }

    private void Singleton_OnServerStarted()
    {
        StartCoroutine(Singleton_OnServerStarted2());
    }
    private IEnumerator Singleton_OnServerStarted2()
    {
        yield return new WaitForSeconds(3);
        Debug.Log("Server Started!");
        networkGameState = Instantiate(networkGameStatePrefab);
        networkGameState.GetComponent<NetworkObject>().Spawn();
        networkGameState.targetLocationIndex.Value = UnityEngine.Random.Range(0, TargetLocation.Data.Count);
        networkGameState.OnStageStart();

        while (true)
        {
            yield return new WaitForSeconds(1);
            var targetLocation = networkGameState.targetLocation;
            if (targetLocation == null) continue;
            Debug.Log("Spawn no!");
            var x = Instantiate(networkGameStatePrefab, targetLocation.position, Quaternion.identity);
            x.GetComponent<NetworkObject>().Spawn();
            // random position
            x.transform.position = new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
        }
    }

    public void OnPlayerBdamaSpawned(BDama b)
    {
        PlayerBdama = b;
        vcam.Follow = PlayerBdama.transform;
        StartCoroutine(UpdateDistanceLoop());
    }

    private IEnumerator UpdateDistanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            if (networkGameState == null)
            {
                Debug.Log("notfound");
                networkGameState = GameObject.FindObjectOfType<NetworkGameState>();
                continue;
            }
            var targetLocation = networkGameState.targetLocation;
            if (targetLocation == null) continue;


            var bdamaXZ = new Vector3(PlayerBdama.transform.position.x, 0, PlayerBdama.transform.position.z);
            var targetXZ = new Vector3(targetLocation.position.x, 0, targetLocation.position.z);
            var distance = Vector3.Distance(bdamaXZ, targetXZ);

            var direction = targetXZ - bdamaXZ;
            var angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);

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

    internal void OnGoal()
    {
        // TODO
        networkGameState.OnStageStart();
    }

    internal void ResetGravity()
    {
        Debug.Log("Reset Gravity!");
        Physics.gravity = Vector3.down * gravityAmount;
    }

    internal void ResetVelocity()
    {
        Debug.Log("Reset Velocity!");
        PlayerBdama.rb.velocity = Vector3.zero;
    }
}
