using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BDama : MonoBehaviour
{
    [NonSerialized] public Rigidbody rb;
    [SerializeField] private float jumpForceMax = 100;
    [SerializeField] public float jumpForceTimeMax = 5;


    private void Awake()
    {
        TryGetComponent(out rb);
    }

    public void Jump(float time)
    {
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
            UIManager.Instance.OnGoal();
        }
    }
}
