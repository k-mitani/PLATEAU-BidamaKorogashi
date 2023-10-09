using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Cinemachine;
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

    [SerializeField] private GameObject goal;

    private TargetLocation targetLocation;
    [NonSerialized] public float gravityAmount = 0;

    public BDama PlayerBdama { get; private set; }

    private void Awake()
    {
        Instance = this;
        // �d�͂̑傫�����擾����B
        gravityAmount = Physics.gravity.magnitude;

        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
    }

    private void Start()
    {
        // �K�v�Ȃ�}�b�v�f�[�^�V�[����ǂݍ��ށB
        if (needLoadMapDataScene)
        {
            Addressables.LoadSceneAsync(refMapDataScene, LoadSceneMode.Additive);
        }
    }

    public void OnPlayerBdamaSpawned(BDama b)
    {
        PlayerBdama = b;
        vcam.Follow = PlayerBdama.transform;
        OnStageStart();
        StartCoroutine(UpdateDistanceLoop());
    }

    public void OnStageStart()
    {
        var target = TargetLocation.Data[UnityEngine.Random.Range(0, TargetLocation.Data.Count)];
        while (target == targetLocation)
        {
            target = TargetLocation.Data[UnityEngine.Random.Range(0, TargetLocation.Data.Count)];
        }

        targetLocation = target;
        UIManager.Instance.UpdateTargetLocation(targetLocation);
        goal.transform.position = targetLocation.position;
    }

    private IEnumerator UpdateDistanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            if (targetLocation == null) continue;

            var bdamaXZ = new Vector3(PlayerBdama.transform.position.x, 0, PlayerBdama.transform.position.z);
            var targetXZ = new Vector3(targetLocation.position.x, 0, targetLocation.position.z);
            var distance = Vector3.Distance(bdamaXZ, targetXZ);

            var direction = targetXZ - bdamaXZ;
            var angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);

            var START = -22.5f;
            var DIFF = 360 / 8;
            var text = angle.ToString("N0");
            if (angle > START + DIFF * 0 && angle < START + DIFF * 1) text = "�k";
            else if (angle > START + DIFF * 1 && angle < START + DIFF * 2) text = "�k��";
            else if (angle > START + DIFF * 2 && angle < START + DIFF * 3) text = "��";
            else if (angle > START + DIFF * 3 && angle < START + DIFF * 4) text = "�쓌";
            else if (angle > START + DIFF * 4 && angle < START + DIFF * 5) text = "��";
            else if (angle + 360 > START + DIFF * 4 && angle + 360 < START + DIFF * 5) text = "��";
            else if (angle + 360 > START + DIFF * 5 && angle + 360 < START + DIFF * 6) text = "�쐼";
            else if (angle + 360 > START + DIFF * 6 && angle + 360 < START + DIFF * 7) text = "��";
            else if (angle + 360 > START + DIFF * 7 && angle + 360 < START + DIFF * 8) text = "�k��";

            UIManager.Instance.UpdateDistance(distance, text, angle);
        }
    }

    internal void OnGoal()
    {
        OnStageStart();
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
