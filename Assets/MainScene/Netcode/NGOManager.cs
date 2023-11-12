using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NGOManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField txtServerIp;

    private (string, ushort) GetServerAddress()
    {
        // "127.0.0.1:7777"という形式
        var serverAddress = txtServerIp.text;
        var serverAddressSplit = serverAddress.Split(':');
        var serverIp = serverAddressSplit[0];
        var serverPort = ushort.Parse(serverAddressSplit[1]);
        return (serverIp, serverPort);
    }

    private bool SetConnectionData()
    {
        try
        {
            if (NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var trans))
            {
                var (addr, port) = GetServerAddress();
                trans.SetConnectionData(addr, port);
                return true;
            }
            Debug.LogError("UnityTransport not found!");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Invalid Server Address! ({txtServerIp.text}) {ex}");
            return false;
        }
    }


    public void OnClickStartHost()
    {
        if (!SetConnectionData()) return;
        NetworkManager.Singleton.StartHost();
    }

    public void OnClickStartServer()
    {
        if (!SetConnectionData()) return;
        NetworkManager.Singleton.StartServer();
    }

    public void OnClickStartClient()
    {
        if (!SetConnectionData()) return;
        NetworkManager.Singleton.StartClient();
        //StartCoroutine(StartClientCoroutine());
    }

    //private IEnumerator StartClientCoroutine()
    //{
    //    yield return new WaitForSeconds(1f);
    //}

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnOnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnOnClientDisconnectCallback;
    }

    private void OnOnClientConnectedCallback(ulong obj)
    {
        gameObject.SetActive(false);
    }

    private void OnOnClientDisconnectCallback(ulong obj)
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnOnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnOnClientDisconnectCallback;
        NetworkManager.Singleton.Shutdown();
    }
}
