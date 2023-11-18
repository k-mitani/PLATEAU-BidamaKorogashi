using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;

public class NGOManager : MonoBehaviour
{
    public static NGOManager Instance { get; private set; }

    [SerializeField] private NetworkManager net;
    private ServerSearcher udp;
    private Action<NetworkEndPoint> onServerFound;

    [Header("デバッグ表示用")]
    [SerializeField] private NetworkEndPoint currentServerEndPoint;
    [SerializeField] private NetworkEndPoint currentRemoteEndPoint;
    [SerializeField] private NGOMode currentMode;
    private bool shouldShutdown;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        try
        {
            udp = new ServerSearcher();
            udp.Receive += Udp_Receive;
        }
        catch (Exception ex)
        {
            Debug.Log("UDP初期化エラー: " + ex.ToString());
        }

        net.OnTransportFailure += Net_OnTransportFailure;
        net.OnServerStarted += Net_OnServerStarted;
        net.OnServerStopped += Net_OnServerStopped;
        net.OnClientStarted += Net_OnClientStarted;
        net.OnClientStopped += Net_OnClientStopped;
        net.OnClientConnectedCallback += Net_OnClientConnectedCallback;
        net.OnClientDisconnectCallback += Net_OnClientDisconnectCallback;
    }

    private void Udp_Receive(object sender, ServerSearcher.Data e)
    {
        if (e.type == ServerSearcher.MessageType.SearchServer)
        {
            if (net.IsHost)
            {
                Debug.Log("サーバー情報要求を受信しました。");
                var addresses = ServerSearcher.GetLocalIPAddressList();
                Debug.Log("自身のIPアドレス: " + string.Join(", ", addresses.Select(x => x.ToString())));
                var address = addresses.FirstOrDefault();
                Debug.Log("サーバー情報を送信します: " + address.ToString());
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
                udp.Send(new ServerSearcher.Data
                {
                    type = ServerSearcher.MessageType.SearchServerResponse,
                    clientId = e.clientId,
                    serverIpv4Address = address.Address,
                    serverIpv4Port = currentServerEndPoint.Port,
                });
#pragma warning restore CS0618 // 型またはメンバーが旧型式です
            }
        }
        if (e.type == ServerSearcher.MessageType.SearchServerResponse)
        {
            var addr = new IPAddress(e.serverIpv4Address);
            var endpoint = NetworkEndPoint.Parse(addr.ToString(), e.serverIpv4Port);
            Debug.Log($"サーバー情報を受信しました。{addr}:{e.serverIpv4Port}");
            onServerFound?.Invoke(endpoint);
        }
    }

    private void Net_OnTransportFailure()
    {
        Debug.Log($"NGO EVENT トランスポート失敗");
    }

    private void Net_OnServerStarted()
    {
        Debug.Log($"NGO EVENT サーバー開始({currentServerEndPoint})");
        GameManager.Instance.OnServerStarted();
    }

    private void Net_OnServerStopped(bool localIsClient)
    {
        Debug.Log($"NGO EVENT サーバー停止({currentServerEndPoint})");
    }

    private void Net_OnClientStarted()
    {
        Debug.Log($"NGO EVENT クライアント開始({currentRemoteEndPoint})");
    }

    private void Net_OnClientStopped(bool localIsServer)
    {
        Debug.Log($"NGO EVENT クライアント停止({currentRemoteEndPoint})");
        // 人為的に停止したのではなくて、接続が切れた場合は再接続を試みる。
        if (currentMode == NGOMode.Client && !shouldShutdown)
        {
            Debug.Log($"NGO 切断されたため、クライアント再接続を試行します({currentRemoteEndPoint})");
            // なぜかすぐに開始すると接続できないので、適当に1秒待ってから再接続する。
            StartCoroutine(aoiueo());
            IEnumerator aoiueo()
            {
                yield return null;
                StartClient(currentRemoteEndPoint);
            }
        }
    }

    private void Net_OnClientConnectedCallback(ulong localClientId)
    {
        Debug.Log($"NGO EVENT クライアント接続({localClientId})");
    }

    private void Net_OnClientDisconnectCallback(ulong localClientId)
    {
        Debug.Log($"NGO EVENT クライアント切断({localClientId})");
    }


    public void StartServer(NetworkEndPoint endpoint)
    {
        net.TryGetComponent<UnityTransport>(out var trans);
        if (trans == null) throw new Exception("UnityTransportが見つかりません。");
        
        currentServerEndPoint = endpoint;
        currentMode = NGOMode.Server;
        shouldShutdown = false;
        Debug.Log($"サーバーを開始します。{endpoint}");
        trans.SetConnectionData(endpoint, endpoint);
        net.StartHost();
    }

    public void SearchServer(Action<NetworkEndPoint> onServerFound)
    {
        this.onServerFound = onServerFound;
        Debug.Log($"サーバー情報要求を送信します。{udp}");
        if (udp == null)
        {
            udp.Send(new ServerSearcher.Data
            {
                type = ServerSearcher.MessageType.SearchServer,
            });
            Debug.Log("サーバー情報要求を送信しました。");
        }
    }

    public void StartClient(NetworkEndPoint endpoint)
    {
        net.TryGetComponent<UnityTransport>(out var trans);
        if (trans == null) throw new Exception("UnityTransportが見つかりません。");

        currentRemoteEndPoint = endpoint;
        currentMode = NGOMode.Client;
        shouldShutdown = false;
        Debug.Log($"接続を開始します。{endpoint}");
        trans.SetConnectionData(endpoint, endpoint);
        net.StartClient();
    }

    public void Shutdown()
    {
        shouldShutdown = true;
        net.Shutdown();
    }

    private void OnDestroy()
    {
        shouldShutdown = true;
        if (net != null)
        {
            net.Shutdown();
        }
        if (udp != null)
        {
            udp.Dispose();
        }
    }

}

public enum NGOMode : int
{
    None = 0,
    Server,
    Client,
}
