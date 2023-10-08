using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject panelMenu;
    [SerializeField] private TextMeshProUGUI textDistance;
    [SerializeField] private TextMeshProUGUI textDirection;
    [SerializeField] private JumpButton buttonJump;
    [SerializeField] private TextMeshProUGUI buttonTextJump;
    [SerializeField] private TextMeshProUGUI textDestination;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private TextMeshProUGUI textAngle;
    [SerializeField] private GameObject goal;

    [System.NonSerialized] public BDama bdama;

    [SerializeField] private AssetReference refMapDataScene;
    [SerializeField] private bool needLoadMapDataScene = true;

    private bool buttonJumpPressing = false;
    private float buttonJumpPressingTime = 0f;

    public void OnMenuToggleClick()
    {
        panelMenu.SetActive(!panelMenu.activeSelf);
    }

    public void OnResetBDamaClick()
    {
        bdama.transform.position = initialBDamaPosition;
        bdama.rb.velocity = Vector3.zero;
        OnGravityResetClick();
    }

    public void OnResetTargetLocationClick()
    {
        OnStageStart();
    }

    public void OnFreeCameraToggleClick()
    {

    }

    public void OnLookTargetLocationClick()
    {
    }

    public void OnGravityResetClick()
    {
        Debug.Log("Reset Gravity!");
        Physics.gravity = Vector3.down * gravityAmount;
    }

    public void OnVelocityResetClick()
    {
        Debug.Log("Reset Velocity!");
        bdama.rb.velocity = Vector3.zero;
    }

    public void OnJumpClick(float time)
    {
        Debug.Log("Jump! " + buttonJumpPressingTime.ToString());
        bdama.Jump(time);
    }


    private void Awake()
    {
        Instance = this;
        panelMenu.SetActive(false);
        // 重力の大きさを取得する。
        gravityAmount = Physics.gravity.magnitude;
        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
    }

    public void OnPlayerBdamaSpawned(BDama b)
    {
        bdama = b;
        vcam.Follow = bdama.transform;
        initialBDamaPosition = bdama.transform.position;
        OnStageStart();
        StartCoroutine(UpdateDistanceLoop());
    }

    private void Start()
    {
        // 必要ならマップデータシーンを読み込む。
        if (needLoadMapDataScene)
        {
            Addressables.LoadSceneAsync(refMapDataScene, LoadSceneMode.Additive);
        }
    }

    [SerializeField] CinemachineVirtualCamera vcam;
    private CinemachineTransposer transposer;

    private void Update()
    {
        if (bdama == null) return;

        var prevPressed = buttonJumpPressing;
        var prevTime = buttonJumpPressingTime;
        if (buttonJump.pressing)
        {
            buttonJumpPressing = true;
            if (!prevPressed)
            {
                buttonJumpPressingTime = 0;
            }
            else
            {
                buttonJumpPressingTime += Time.deltaTime;
                var power = Mathf.Min((int)(buttonJumpPressingTime * 10), bdama.jumpForceTimeMax * 10);
                if ((int)(prevTime * 10) < power)
                {
                    buttonTextJump.text = $"ジャンプ\n({power})";
                }
            }
        }
        else
        {
            if (prevPressed)
            {
                OnJumpClick(buttonJumpPressingTime);
                buttonJumpPressing = false;
                buttonTextJump.text = "ジャンプ";
            }
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnJumpClick(bdama.jumpForceTimeMax / 2);
            buttonJumpPressing = false;
            buttonTextJump.text = "ジャンプ";
        }
        else if (Keyboard.current.cKey.isPressed)
        {
            OnGravityResetClick();
        }
        else if (Keyboard.current.vKey.isPressed)
        {
            OnVelocityResetClick();
        }
        else
        {
            buttonJumpPressingTime = 0;
        }


        // 前側45度に重力の方向を変える。
        if (Keyboard.current.upArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, Vector3.right) * Vector3.down * gravityAmount;
        else if (Keyboard.current.downArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, -Vector3.right) * Vector3.down * gravityAmount;
        else if (Keyboard.current.leftArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, Vector3.forward) * Vector3.down * gravityAmount;
        else if (Keyboard.current.rightArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, -Vector3.forward) * Vector3.down * gravityAmount;
    }

    private Vector3 initialBDamaPosition = Vector3.zero;
    private float gravityAmount = 0;

    private TargetLocation targetLocation;
    private void OnStageStart()
    {
        var target = TargetLocation.Data[Random.Range(0, TargetLocation.Data.Count)];
        while (target == targetLocation)
        {
            target = TargetLocation.Data[Random.Range(0, TargetLocation.Data.Count)];
        }

        targetLocation = target;
        textDestination.text = targetLocation.name;
        textDescription.text = targetLocation.description;
        goal.transform.position = targetLocation.position;
    }

    private IEnumerator UpdateDistanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            if (targetLocation == null) continue;

            var bdamaXZ = new Vector3(bdama.transform.position.x, 0, bdama.transform.position.z);
            var targetXZ = new Vector3(targetLocation.position.x, 0, targetLocation.position.z);
            var distance = Vector3.Distance(bdamaXZ, targetXZ);
            textDistance.text = distance.ToString("N0") + "m";

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
            textDirection.text = text;
            textAngle.text = angle.ToString("N0") + "";
        }
    }

    internal void OnGoal()
    {
        OnStageStart();
    }
}
