using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BDama : NetworkBehaviour
{
    [NonSerialized] public NetworkPlayer player;
    [NonSerialized] public Rigidbody rb;
    [NonSerialized] public MeshRenderer meshRenderer;
    [SerializeField] private float jumpForceMax = 100;
    [SerializeField] public float jumpForceTimeMax = 5;
    [SerializeField] private float gravityAmountAdjustment = 30;
    [SerializeField] private float gravityAmountNormal = 9.81f;
    public NetworkVariable<Vector3> gravity = new(Vector3.zero, writePerm: NetworkVariableWritePermission.Owner);


    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnBDamaSpawned(this);
        player.OnBDamaSpawned(this);
        if (IsOwner)
        {
            gravity.Value = gravityAmountAdjustment * gravityAmountNormal * Vector3.down;
        }
    }

    public override void OnNetworkDespawn()
    {
        GameManager.Instance.OnBDamaDespawned(this);
    }

    public void SetMaterial(Material mat)
    {
        meshRenderer.material = mat;
    }

    private void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out meshRenderer);
    }

    public void Jump(float time)
    {
        Debug.Log("Jump! " + time.ToString());
        time = Mathf.Clamp(time, 0, jumpForceTimeMax);
        var force = Mathf.Lerp(0, jumpForceMax, time / jumpForceTimeMax);
        Debug.Log(time / jumpForceTimeMax);

        JumpServerRpc(force);
        UIManager.Instance.buttonJump.GetComponent<Button>().interactable = false;
        UIManager.Instance.mobileJumpCoolTime = UIManager.Instance.mobileJumpCoolTimeMax;
        StartCoroutine(UpdateButtonText());
        static IEnumerator UpdateButtonText()
        {
            var text = UIManager.Instance.buttonJump.GetComponentInChildren<TextMeshProUGUI>();
            while (UIManager.Instance.mobileJumpCoolTime > 0)
            {
                text.text = $"ジャンプ\n({UIManager.Instance.mobileJumpCoolTime.ToString("0.0")})";
                yield return new WaitForSeconds(0.1f);
            }
            text.text = $"ジャンプ";
        }
    }

    [ServerRpc]
    private void JumpServerRpc(float force)
    {
        Debug.Log("JumpServerRpc! " + force);
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger!");
        if (other.CompareTag("Goal"))
        {
            Debug.Log("Goal!");
            if (IsOwner)
            {
                OnGoalServerRpc();
            }
        }
    }

    [ServerRpc]
    public void OnGoalServerRpc()
    {
        Debug.Log("server 誰かゴールしたらしい" + OwnerClientId.ToString());
        GameManager.Instance.OnGoal(this);
    }

    internal void UpdateGravityDirection(Vector3 vector3)
    {
        gravity.Value = gravityAmountAdjustment * gravityAmountNormal * vector3;
    }

    private void FixedUpdate()
    {
        rb.AddForce(gravity.Value, ForceMode.Acceleration);
    }

    [ServerRpc]
    public void ResetServerRpc()
    {
        Reset();
    }

    public void Reset()
    {
        rb.velocity = Vector3.zero;
        transform.position = GameManager.Instance.initialBDamaPosition.transform.position + new Vector3(
            UnityEngine.Random.value * 30,
            UnityEngine.Random.value * 100,
            UnityEngine.Random.value * 30);
        
        UpdateGravityDirectionClientRpc(Vector3.down, new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    public void UpdateGravityDirectionClientRpc(Vector3 dir, ClientRpcParams clientRpcParams = default)
    {
        UpdateGravityDirection(dir);
    }
}
