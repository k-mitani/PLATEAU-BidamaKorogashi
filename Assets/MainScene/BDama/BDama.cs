using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BDama : NetworkBehaviour
{
    [NonSerialized] public NetworkPlayer player;
    [NonSerialized] public Rigidbody rb;
    [NonSerialized] public MeshRenderer meshRenderer;
    [NonSerialized] public Vector3 initialPosition = Vector3.zero;
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
        initialPosition = transform.position;
    }

    public void Jump(float time)
    {
        Debug.Log("Jump! " + time.ToString());
        time = Mathf.Clamp(time, 0, jumpForceTimeMax);
        var force = Mathf.Lerp(0, jumpForceMax, time / jumpForceTimeMax);
        Debug.Log(time / jumpForceTimeMax);
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
                player.score.Value++;
                OnGoalServerRpc();
            }
        }
    }

    [ServerRpc]
    private void OnGoalServerRpc()
    {
        GameManager.Instance.OnGoal();
    }

    internal void UpdateGravityDirection(Vector3 vector3)
    {
        gravity.Value = gravityAmountAdjustment * gravityAmountNormal * vector3;
    }

    private void FixedUpdate()
    {
        rb.AddForce(gravity.Value, ForceMode.Acceleration);
    }
}
