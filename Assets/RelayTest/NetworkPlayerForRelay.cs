using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerForRelay : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float speed = 10;


    private void Awake()
    {
        TryGetComponent(out rb);
    }

    override public void OnNetworkSpawn()
    {
        // ランダムな初期位置をセットする。
        if (IsOwner)
        {
            transform.position = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            // 上下左右キーで、そちらの方向に力を加える。
            if (Input.GetKey(KeyCode.UpArrow)) rb.AddForce(Vector3.forward * speed);
            if (Input.GetKey(KeyCode.DownArrow)) rb.AddForce(Vector3.back * speed);
            if (Input.GetKey(KeyCode.LeftArrow)) rb.AddForce(Vector3.left * speed);
            if (Input.GetKey(KeyCode.RightArrow)) rb.AddForce(Vector3.right * speed);
        }
    }
}
