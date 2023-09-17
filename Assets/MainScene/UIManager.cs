using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [SerializeField] private float arrowKeyForce = 100f;

    [SerializeField] private BDama bdama;

    private bool buttonJumpPressing = false;
    private float buttonJumpPressingTime = 0f;

    public void OnMenuToggleClick()
    {
        panelMenu.SetActive(!panelMenu.activeSelf);
    }

    public void OnResetBDamaClick()
    {

    }

    public void OnResetTargetLocationClick()
    {

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
        gravityDirection = Vector3.down;
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
    }

    private void Start()
    {
        OnStageStart();
        StartCoroutine(UpdateDistanceLoop());
    }

    private void Update()
    {
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


        // 力を加える。
        if (controlModeForce)
        {
            if (Keyboard.current.upArrowKey.isPressed) bdama.rb.AddForce(Vector3.forward * arrowKeyForce, ForceMode.Impulse);
            else if (Keyboard.current.downArrowKey.isPressed) bdama.rb.AddForce(Vector3.back * arrowKeyForce, ForceMode.Impulse);
            else if (Keyboard.current.leftArrowKey.isPressed) bdama.rb.AddForce(Vector3.left * arrowKeyForce, ForceMode.Impulse);
            else if (Keyboard.current.rightArrowKey.isPressed) bdama.rb.AddForce(Vector3.right * arrowKeyForce, ForceMode.Impulse);
        }
        else if (controlMode45)
        {
            // 前側45度に重力の方向を変える。
            if (Keyboard.current.upArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, Vector3.right) * Vector3.down * gravityAmount;
            else if (Keyboard.current.downArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, -Vector3.right) * Vector3.down * gravityAmount;
            else if (Keyboard.current.leftArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, Vector3.forward) * Vector3.down * gravityAmount;
            else if (Keyboard.current.rightArrowKey.isPressed) Physics.gravity = Quaternion.AngleAxis(-45, -Vector3.forward) * Vector3.down * gravityAmount;
        }
        // 重力の向きを変える。
        else
        {
            var prevGravity = Physics.gravity;
            if (Keyboard.current.upArrowKey.isPressed)
            {
                var nextGravityDirection = Quaternion.AngleAxis(-gravityAngleChangeSpeed * Time.deltaTime, Vector3.right) * gravityDirection;
                var nextGravity = gravityDirection * gravityAmount;
                var angle = Vector3.Angle(nextGravity, Vector3.down);
                Debug.Log($"{angle} {prevGravityAngle}");
                if (prevGravityAngle > angle || angle < 80)
                {
                    gravityDirection = nextGravityDirection;
                    Physics.gravity = nextGravity;
                    prevGravityAngle = angle;
                }
            }
            else if (Keyboard.current.downArrowKey.isPressed)
            {
                var nextGravityDirection = Quaternion.AngleAxis(-gravityAngleChangeSpeed * Time.deltaTime, -Vector3.right) * gravityDirection;
                var nextGravity = gravityDirection * gravityAmount;
                var angle = Vector3.Angle(nextGravity, Vector3.down);
                Debug.Log($"{angle} {prevGravityAngle}");
                if (prevGravityAngle > angle || angle < 80)
                {
                    gravityDirection = nextGravityDirection;
                    Physics.gravity = nextGravity;
                    prevGravityAngle = angle;
                }
            }
            else if (Keyboard.current.leftArrowKey.isPressed)
            {
                var nextGravityDirection = Quaternion.AngleAxis(-gravityAngleChangeSpeed * Time.deltaTime, Vector3.forward) * gravityDirection;
                var nextGravity = gravityDirection * gravityAmount;
                var angle = Vector3.Angle(nextGravity, Vector3.down);
                Debug.Log($"{angle} {prevGravityAngle}");
                if (prevGravityAngle > angle || angle < 80)
                {
                    gravityDirection = nextGravityDirection;
                    Physics.gravity = nextGravity;
                    prevGravityAngle = angle;
                }
            }
            else if (Keyboard.current.rightArrowKey.isPressed)
            {
                var nextGravityDirection = Quaternion.AngleAxis(-gravityAngleChangeSpeed * Time.deltaTime, -Vector3.forward) * gravityDirection;
                var nextGravity = gravityDirection * gravityAmount;
                var angle = Vector3.Angle(nextGravity, Vector3.down);
                Debug.Log($"{angle} {prevGravityAngle}");
                if (prevGravityAngle > angle || angle < 80)
                {
                    gravityDirection = nextGravityDirection;
                    Physics.gravity = nextGravity;
                    prevGravityAngle = angle;
                }
            }

        }
    }
    [SerializeField] private bool controlModeForce;
    [SerializeField] private bool controlMode45;
    private float gravityAmount = 0;
    private float prevGravityAngle = -1000;
    [SerializeField] private Vector3 gravityDirection = Vector3.down;
    [SerializeField] private float gravityAngleChangeSpeed = 60;


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
