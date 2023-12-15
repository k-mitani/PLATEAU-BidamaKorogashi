using System;
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
    [SerializeField] public JumpButton buttonJump;
    [SerializeField] private TextMeshProUGUI buttonTextJump;
    [SerializeField] private TextMeshProUGUI textDestination;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private TextMeshProUGUI textAngle;

    [SerializeField] private UIManagerNGO ngo;
    [SerializeField] private GameObject panelPlayerSetting;
    [SerializeField] private GameObject panelDebug;
    [SerializeField] private TextMeshProUGUI textDebug;
    [SerializeField] public bool debugMode = false;
    [Header("ゴールUI")]
    [SerializeField] public GameObject panelGoal;
    [SerializeField] public TextMeshProUGUI textGoalPlayer;
    [SerializeField] public TextMeshProUGUI textGoalPlayerCongraturation;
    [SerializeField] public GameObject panelNext;
    [SerializeField] public TextMeshProUGUI textNextDestination;
    [SerializeField] public TextMeshProUGUI textNextDestinationType;
    [Header("カメラ設定UI")]
    [SerializeField] private Slider sliderCameraTilt;
    [SerializeField] private Slider sliderCameraZoom;
    [Header("ランキングUI")]
    [SerializeField] private GameObject rankingParent;
    [NonSerialized] public RankingItem[] rankingItems;
    [Header("プレーヤー設定UI")]
    [SerializeField] public Material[] playerColors;
    [SerializeField] private TMP_Dropdown inputPlayerColor;
    [SerializeField] private TextMeshProUGUI playerSettingLog;
    [SerializeField] private int selectedColorIndex;
    [Header("観戦設定UI")]
    [SerializeField] private Toggle radioWatchModeDivide;
    [SerializeField] private Toggle radioWatchModeFree;
    [SerializeField] private Toggle radioWatchModePlayer;
    [SerializeField] private TMP_Dropdown dropdownWatchPlayer;
    [SerializeField] private TextMeshProUGUI watchSettingLog;
    [SerializeField] public GameObject DividerFor4;
    [SerializeField] public GameObject DividerFor3;
    [SerializeField] public GameObject DividerFor2;
    [Header("スマホ用")]
    [SerializeField] private float mobileJumpAccelerationMagnitudeDiffrenceThreshold = 3;
    [SerializeField] private float mobileJumpForceAdjustment = 0.15f;
    [SerializeField] public float mobileJumpCoolTimeMax = 0.5f;
    [SerializeField] public float mobileJumpCoolTime = 0f;

    private bool collectData = false;


    private bool buttonJumpPressing = false;
    private float buttonJumpPressingTime = 0f;

    private float? defaultDistance = null;
    public void OnCameraTiltChange(float value)
    {
        UpdateCameraSetting();
    }

    public void OnCameraZoomChange(float value)
    {
        UpdateCameraSetting();
    }

    private void UpdateCameraSetting()
    {
        var gm = GameManager.Instance;
        var transposer = gm.vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (defaultDistance == null)
        {
            defaultDistance = transposer.m_FollowOffset.magnitude;
        }
        var zoom = sliderCameraZoom.value;
        var tilt = (sliderCameraTilt.value - 0.5f) * Mathf.PI;
        var x = Mathf.Sin(tilt);
        var y = Mathf.Cos(tilt);
        transposer.m_FollowOffset = defaultDistance.Value * zoom * new Vector3(0, y, x);
        //gm.vcam.transform.LookAt(gm.vcam.Follow);
        gm.vcam.transform.rotation = Quaternion.Euler(180 * sliderCameraTilt.value, 0, 0);
    }

    public void OnMenuToggleClick()
    {
        panelMenu.SetActive(!panelMenu.activeSelf);
    }

    public void OnResetBDamaClick()
    {
        var bdama = GameManager.Instance.PlayerBdama;
        bdama.ResetStateServerRpc(GameManager.Instance.initialBDamaPosition.transform.position);
    }

    public void OnResetBDamaAllClick()
    {
        GameManager.Instance.LocalPlayer.RequestResetBDamaAllServerRpc();
    }

    public void OnResetScoreAllClick()
    {
        GameManager.Instance.LocalPlayer.RequestResetScoreAllServerRpc();
    }


    public void OnResetTargetLocationClick()
    {
        GameManager.Instance.LocalPlayer.RequestStageStartServerRpc();
    }

    public void OnDebugToggleClick()
    {
        GameManager.Instance.PlayerBdama.OnGoalServerRpc();
        //debugMode = !debugMode;
        //panelDebug.SetActive(debugMode);
        //textDebug.text = "";
    }

    public void OnNetworkToggleClick()
    {
        ngo.ToggleVisibility();
    }

    public void OnPlayerSettingToggleClick()
    {
        panelPlayerSetting.SetActive(!panelPlayerSetting.activeSelf);
    }

    public void OnLookTargetLocationClick()
    {
    }


    public void PlayerSettingOnChangeColor(int selectedIndex)
    {
        UpdateLocalPlayerColor();
    }

    public void UpdateLocalPlayerColor()
    {
        var index = inputPlayerColor.value - 1;
        // -1ならランダムに割り振る。
        if (index == -1)
        {
            index = UnityEngine.Random.Range(0, playerColors.Length);
        }
        selectedColorIndex = index;
        var player = GameManager.Instance.LocalPlayer;
        if (player != null)
        {
            player.colorIndex.Value = selectedColorIndex;
            Debug.Log("プレーヤーカラーセット！ " + selectedColorIndex);
        }
    }

    public void PlayerSettingOnClickStart()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player == null)
        {
            playerSettingLog.text = $"{DateTime.Now:HH:mm:ss} サーバーと接続されていません。";
            return;
        }
        player.StartBDamaGameServerRpc();
        panelPlayerSetting.SetActive(false);
    }

    public void PlayerSettingOnClickEnd()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player == null)
        {
            playerSettingLog.text = $"{DateTime.Now:HH:mm:ss} サーバーと接続されていません。";
            return;
        }
        if (player.mode.Value != NetworkPlayerMode.BDama)
        {
            playerSettingLog.text = $"{DateTime.Now:HH:mm:ss} ビー玉モード中ではありません。";
            return;
        }
        player.EndBDamaGameServerRpc();
    }

    public void WatchSettingOnClickStart()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player == null)
        {
            watchSettingLog.text = $"{DateTime.Now:HH:mm:ss} サーバーと接続されていません。";
            return;
        }
        if (player.mode.Value == NetworkPlayerMode.None)
        {
            player.StartWatchServerRpc();
            //panelPlayerSetting.SetActive(false);
        }

        if (player.mode.Value == NetworkPlayerMode.Watch)
        {
            if (radioWatchModeDivide.isOn) WatchByDividedDisplay();
            else if (radioWatchModeFree.isOn) WatchByFreeCamera();
            else if (radioWatchModePlayer.isOn) WatchPlayer(dropdownWatchPlayer.value);
        }
    }

    public void WatchSettingOnClickEnd()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player == null)
        {
            watchSettingLog.text = $"{DateTime.Now:HH:mm:ss} サーバーと接続されていません。";
            return;
        }
        if (player.mode.Value != NetworkPlayerMode.Watch)
        {
            watchSettingLog.text = $"{DateTime.Now:HH:mm:ss} 観戦モード中ではありません。";
            return;
        }
        player.EndWatch();
    }

    public void OnJumpClick(float time)
    {
        if (mobileJumpCoolTime > 0) return;
        var bdama = GameManager.Instance.PlayerBdama;
        if (bdama == null) return;
        bdama.Jump(time);
    }


    public void OnJumpClick()
    {
        //// Android版でなければ何もしない。
        //if (Application.platform != RuntimePlatform.Android) return;
        if (mobileJumpCoolTime > 0) return;
        var bdama = GameManager.Instance.PlayerBdama;
        if (bdama == null) return;
        bdama.Jump(bdama.jumpForceTimeMax / mobileJumpForceAdjustment);
    }

    public void OnCollectDataToggleClick()
    {
        collectData = !collectData;
    }


    private void Awake()
    {
        Instance = this;
        panelMenu.SetActive(false);
        rankingItems = rankingParent.transform.GetComponentsInChildren<RankingItem>(true);
    }

    private void WatchPlayer(int index)
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player != null && player.mode.Value == NetworkPlayerMode.Watch)
        {
            player.WatchPlayer(index);
            radioWatchModePlayer.isOn = true;
            dropdownWatchPlayer.value = index;
        }
    }

    private void WatchByDividedDisplay()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player != null && player.mode.Value == NetworkPlayerMode.Watch)
        {
            player.WatchByDividedDisplay();
            radioWatchModeDivide.isOn = true;
        }
    }

    private void WatchByFreeCamera()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player != null && player.mode.Value == NetworkPlayerMode.Watch)
        {
            player.WatchByFreeCamera();
            radioWatchModeFree.isOn = true;
        }
    }

    private void Update()
    {
        var player = GameManager.Instance.LocalPlayer;
        if (player != null && player.mode.Value == NetworkPlayerMode.Watch)
        {
            // 1-9が押されたら、そのプレーヤーを観戦する。
            if (Keyboard.current.digit1Key.wasPressedThisFrame) WatchPlayer(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) WatchPlayer(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) WatchPlayer(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) WatchPlayer(3);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame) WatchPlayer(4);
            else if (Keyboard.current.digit6Key.wasPressedThisFrame) WatchPlayer(5);
            else if (Keyboard.current.digit7Key.wasPressedThisFrame) WatchPlayer(6);
            else if (Keyboard.current.digit8Key.wasPressedThisFrame) WatchPlayer(7);
            else if (Keyboard.current.digit9Key.wasPressedThisFrame) WatchPlayer(8);
            // 0が押されたら、画面分割にする。
            else if (Keyboard.current.digit0Key.wasPressedThisFrame) WatchByDividedDisplay();
            // -が押されたら、自由視点にする。
            else if (Keyboard.current.minusKey.wasPressedThisFrame) WatchByFreeCamera();
        }

        var bdama = GameManager.Instance.PlayerBdama;
        if (bdama == null) return;

        //var prevPressed = buttonJumpPressing;
        //var prevTime = buttonJumpPressingTime;
        //if (buttonJump.pressing)
        //{
        //    buttonJumpPressing = true;
        //    if (!prevPressed)
        //    {
        //        buttonJumpPressingTime = 0;
        //    }
        //    else
        //    {
        //        buttonJumpPressingTime += Time.deltaTime;
        //        var power = Mathf.Min((int)(buttonJumpPressingTime * 10), bdama.jumpForceTimeMax * 10);
        //        if ((int)(prevTime * 10) < power)
        //        {
        //            buttonTextJump.text = $"ジャンプ\n({power})";
        //        }
        //    }
        //}
        //else
        //{
        //    if (prevPressed)
        //    {
        //        OnJumpClick(buttonJumpPressingTime);
        //        buttonJumpPressing = false;
        //        buttonTextJump.text = "ジャンプ";
        //    }
        //}

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnJumpClick();
            buttonJumpPressing = false;
            buttonTextJump.text = "ジャンプ";
        }
        else
        {
            buttonJumpPressingTime = 0;
        }

        // スマホの場合
        if (Application.platform == RuntimePlatform.Android)
        {
            // スマホ版の操作
            // 端末の傾きで、重力の向きを変える。
            // Physics.gravity = Input.acceleration.normalized * gravityAmount;
            var gr = Quaternion.AngleAxis(-90, Vector3.right) * Input.acceleration.normalized;
            gr.z = -gr.z;
            bdama.UpdateGravityDirection(gr);
        }

        // 前側45度に重力の方向を変える。
        if (Keyboard.current.upArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, Vector3.right) * Vector3.down);
        else if (Keyboard.current.downArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, -Vector3.right) * Vector3.down);
        else if (Keyboard.current.leftArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, Vector3.forward) * Vector3.down);
        else if (Keyboard.current.rightArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, -Vector3.forward) * Vector3.down);
    }

    private Vector3 prevAcceleration = Vector3.zero;
    private void FixedUpdate()
    {
        var prev = prevAcceleration;
        var current = Input.acceleration;
        prevAcceleration = current;
        if (debugMode)
        {
            if (collectData)
            {
                textDebug.text = $"raw:{current}\t{current - prev}\t{current.magnitude:0.00}\t{(current - prev).magnitude:0.00}{Environment.NewLine}{textDebug.text}";
            }
            if (Application.platform == RuntimePlatform.Android)
            {
            }
        }
        

        var accelerationDiff = (current - prev).magnitude;
        if (accelerationDiff > mobileJumpAccelerationMagnitudeDiffrenceThreshold)
        {
            if (mobileJumpCoolTime <= 0)
            {
                var bdama = GameManager.Instance.PlayerBdama;
                if (bdama != null)
                {
                    bdama.Jump(accelerationDiff * bdama.jumpForceTimeMax / mobileJumpForceAdjustment);
                }
            }
        }
        if (mobileJumpCoolTime > 0)
        {
            mobileJumpCoolTime -= Time.fixedDeltaTime;
            if (mobileJumpCoolTime <= 0)
            {
                buttonJump.GetComponent<Button>().interactable = true;
            }
        }
    }

    internal void UpdateDistance(float distance, string text, float angle)
    {
        textDistance.text = distance.ToString("N0") + "m";
        textDirection.text = text;
        textAngle.text = angle.ToString("N0") + "";
    }

    internal void UpdateTargetLocation(TargetLocation targetLocation)
    {
        textDestination.text = targetLocation.name;
        textDescription.text = targetLocation.description;
    }

}
