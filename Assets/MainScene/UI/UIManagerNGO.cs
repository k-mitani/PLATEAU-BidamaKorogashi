using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class UIManagerNGO : MonoBehaviour
{
    [SerializeField] private TMP_InputField txtServerIp;
    [SerializeField] private TextMeshProUGUI labelServerInfoMessage;

    [SerializeField] private TMP_InputField txtRemoteIp;
    [SerializeField] private TextMeshProUGUI labelClientInfoMessage;

    [SerializeField] private bool autoStart;

    [SerializeField] private bool debugMainIsServerAndCloneIsClient = true;

    private enum Mode : int
    {
        None = 0,
        Server,
        Client,
    }

    private void Start()
    {
        var defaultServerIp = PlayerPrefs.GetString("NGO_serverIp", null);
        if (!string.IsNullOrEmpty(defaultServerIp))
        {
            txtServerIp.text = defaultServerIp;
        }
        var defaultRemoteIp = PlayerPrefs.GetString("NGO_remoteIp", null);
        if (!string.IsNullOrEmpty(defaultRemoteIp))
        {
            txtRemoteIp.text = defaultRemoteIp;
        }
        var previousMode = (Mode)PlayerPrefs.GetInt("NGO_mode", (int)Mode.None);
#if UNITY_EDITOR
        // 開発中は、ParrelSyncのクローンかどうかに応じてモードを切り替える。
        previousMode =
            (debugMainIsServerAndCloneIsClient && !ClonesManager.IsClone()) ||
            (!debugMainIsServerAndCloneIsClient && ClonesManager.IsClone()) ? Mode.Server : Mode.Client;
#endif
        if (previousMode != Mode.None && autoStart)
        {
            Debug.Log($"自動起動有効: {previousMode}");
            StartCoroutine(AutoStart(previousMode));
            IEnumerator AutoStart(Mode mode)
            {
                // 1フレーム待ってから開始する。
                yield return null;
                if (mode == Mode.Server)
                {
                    OnClickStartServer();
                }
                else if (mode == Mode.Client)
                {
                    OnClickConnect();
                }
                gameObject.SetActive(false);
            }
        }
    }

    public void OnClickStartServer()
    {
        var endpoint = ParseIPText(txtServerIp.text);
        PlayerPrefs.SetString("NGO_serverIp", txtServerIp.text);
        PlayerPrefs.SetInt("NGO_mode", (int)Mode.Server);
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
        PlayerPrefs.SetString("NGO_remoteIp", txtRemoteIp.text);
        PlayerPrefs.SetInt("NGO_mode", (int)Mode.Client);
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
