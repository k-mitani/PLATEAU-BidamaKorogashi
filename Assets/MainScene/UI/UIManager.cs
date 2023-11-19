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
    [SerializeField] private JumpButton buttonJump;
    [SerializeField] private TextMeshProUGUI buttonTextJump;
    [SerializeField] private TextMeshProUGUI textDestination;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private TextMeshProUGUI textAngle;

    [SerializeField] private UIManagerNGO ngo;
    [SerializeField] private GameObject panelPlayerSetting;
    [SerializeField] private GameObject panelDebug;
    [SerializeField] private TextMeshProUGUI textDebug;
    [SerializeField] public bool debugMode = false;
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
    [Header("スマホ用")]
    [SerializeField] private float mobileJumpAccelerationMagnitudeDiffrenceThreshold = 3;
    [SerializeField] private float mobileJumpForceAdjustment = 0.15f;
    [SerializeField] private float mobileJumpCoolTimeMax = 0.5f;
    [SerializeField] private float mobileJumpCoolTime = 0f;

    private bool collectData = false;


    private bool buttonJumpPressing = false;
    private float buttonJumpPressingTime = 0f;

    public void OnMenuToggleClick()
    {
        panelMenu.SetActive(!panelMenu.activeSelf);
    }

    public void OnResetBDamaClick()
    {
        var bdama = GameManager.Instance.PlayerBdama;
        bdama.transform.position = bdama.initialPosition;
        bdama.rb.velocity = Vector3.zero;
        OnGravityResetClick();
    }

    public void OnResetTargetLocationClick()
    {
        // TODO
        //GameManager.Instance.OnStageStart();
    }

    public void OnDebugToggleClick()
    {
        debugMode = !debugMode;
        panelDebug.SetActive(debugMode);
        textDebug.text = "";
    }

    public void OnNetworkToggleClick()
    {
        ngo.ToggleVisibility();
    }

    public void OnPlayerSettingToggleClick()
    {
        panelPlayerSetting.SetActive(!panelPlayerSetting.activeSelf);
    }

    public void OnFreeCameraToggleClick()
    {

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

    }

    public void PlayerSettingOnClickEnd()
    {

    }

    public void WatchSettingOnClickStart()
    {

    }

    public void WatchSettingOnClickEnd()
    {

    }

    public void OnGravityResetClick()
    {
        Debug.Log("Reset Gravity!");
        var bdama = GameManager.Instance.PlayerBdama;
        if (bdama == null) return;
        bdama.UpdateGravityDirection(Vector3.down);
    }

    public void OnVelocityResetClick()
    {
        GameManager.Instance.ResetVelocity();
    }

    public void OnJumpClick(float time)
    {
        var bdama = GameManager.Instance.PlayerBdama;
        bdama.Jump(time);
    }


    public void OnJumpClick()
    {
        // Android版でなければ何もしない。
        if (Application.platform != RuntimePlatform.Android) return;

        var bdama = GameManager.Instance.PlayerBdama;
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
    }

    private void Update()
    {
        var bdama = GameManager.Instance.PlayerBdama;
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
                bdama.Jump(accelerationDiff * bdama.jumpForceTimeMax / mobileJumpForceAdjustment);
                mobileJumpCoolTime = mobileJumpCoolTimeMax;
            }
        }
        if (mobileJumpCoolTime > 0)
        {
            mobileJumpCoolTime -= Time.fixedDeltaTime;
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
