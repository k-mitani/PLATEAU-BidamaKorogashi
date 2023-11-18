using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;

public class UIManagerNGO : MonoBehaviour
{
    [SerializeField] private TMP_InputField txtServerIp;
    [SerializeField] private TextMeshProUGUI labelServerInfoMessage;

    [SerializeField] private TMP_InputField txtRemoteIp;
    [SerializeField] private TextMeshProUGUI labelClientInfoMessage;

    public void OnClickStartServer()
    {
        var endpoint = ParseIPText(txtServerIp.text);
        NGOManager.Instance.StartServer(endpoint);
    }

    public void OnClickSearchServer()
    {
        NGOManager.Instance.SearchServer(OnServerFound);
    }

    public void OnServerFound(NetworkEndPoint serverEndPoint)
    {
        txtRemoteIp.text = serverEndPoint.Address + ":" + serverEndPoint.Port;
        labelClientInfoMessage.text = $"[{DateTime.Now:HH:mm:ss}] サーバーが見つかりました。";
    }

    public void OnClickConnect()
    {
        var endpoint = ParseIPText(txtRemoteIp.text);
        NGOManager.Instance.StartClient(endpoint);
    }

    public void OnClickShutdown()
    {
        NGOManager.Instance.Shutdown();
    }

    private NetworkEndPoint ParseIPText(string text)
    {
        // "127.0.0.1:7777"という形式
        var serverAddressSplit = text.Split(':');
        var serverIp = serverAddressSplit[0];
        var serverPort = ushort.Parse(serverAddressSplit[1]);
        return NetworkEndPoint.Parse(serverIp, serverPort);
    }
}
