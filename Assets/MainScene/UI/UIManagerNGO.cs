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
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_InputField txtServerIp;
    [SerializeField] private TextMeshProUGUI labelServerInfoMessage;

    [SerializeField] private TMP_InputField txtRemoteIp;
    [SerializeField] private TextMeshProUGUI labelClientInfoMessage;

    [SerializeField] private bool autoStart;

    [SerializeField] private bool debugMainIsServerAndCloneIsClient = true;

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
        var previousMode = (NGOMode)PlayerPrefs.GetInt("NGO_mode", (int)NGOMode.None);
#if UNITY_EDITOR
        // 開発中は、ParrelSyncのクローンかどうかに応じてモードを切り替える。
        previousMode =
            (debugMainIsServerAndCloneIsClient && !ClonesManager.IsClone()) ||
            (!debugMainIsServerAndCloneIsClient && ClonesManager.IsClone()) ? NGOMode.Server : NGOMode.Client;
#endif
        if (previousMode != NGOMode.None && autoStart)
        {
            root.SetActive(false);
            Debug.Log($"自動起動有効: {previousMode}");
            StartCoroutine(AutoStart(previousMode));
            IEnumerator AutoStart(NGOMode mode)
            {
                // 1フレーム待ってから開始する。
                yield return null;
                if (mode == NGOMode.Server)
                {
                    OnClickStartServer();
                }
                else if (mode == NGOMode.Client)
                {
                    OnClickConnect();
                }
            }
        }
        else
        {
            root.SetActive(true);
        }
    }

    public void ToggleVisibility() => root.SetActive(!root.activeSelf);
    public void Show() => root.SetActive(true);
    public void Hide() => root.SetActive(false);

    public void OnClickStartServer()
    {
        var endpoint = ParseIPText(txtServerIp.text);
        PlayerPrefs.SetString("NGO_serverIp", txtServerIp.text);
        PlayerPrefs.SetInt("NGO_mode", (int)NGOMode.Server);
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
        PlayerPrefs.SetInt("NGO_mode", (int)NGOMode.Client);
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
