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

    public void OnFreeCameraToggleClick()
    {

    }

    public void OnLookTargetLocationClick()
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


        // 前側45度に重力の方向を変える。
        if (Keyboard.current.upArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, Vector3.right) * Vector3.down);
        else if (Keyboard.current.downArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, -Vector3.right) * Vector3.down);
        else if (Keyboard.current.leftArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, Vector3.forward) * Vector3.down);
        else if (Keyboard.current.rightArrowKey.isPressed) bdama.UpdateGravityDirection(Quaternion.AngleAxis(-45, -Vector3.forward) * Vector3.down);
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
