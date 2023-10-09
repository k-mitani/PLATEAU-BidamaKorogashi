using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BDama : NetworkBehaviour
{
    [NonSerialized] public Rigidbody rb;
    [NonSerialized] public Vector3 initialPosition = Vector3.zero;
    [SerializeField] private float jumpForceMax = 100;
    [SerializeField] public float jumpForceTimeMax = 5;


    public override void OnNetworkSpawn()
    {
        Debug.Log("Spawn!");
        if (IsLocalPlayer)
        {
            GameManager.Instance.OnPlayerBdamaSpawned(this);
        }
    }

    private void Awake()
    {
        TryGetComponent(out rb);
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
            GameManager.Instance.OnGoal();
        }
    }
}
