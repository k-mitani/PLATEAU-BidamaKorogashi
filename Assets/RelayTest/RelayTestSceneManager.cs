using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using System.Linq;
using Unity.Services.Core;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;

public class RelayTestSceneManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textJoinCodeForHost;
    [SerializeField] private TMP_InputField inputJoinCodeForClient;
    [SerializeField] private Allocation allocation;
    [SerializeField] private JoinAllocation joinAllocation;
    [SerializeField] private string joinCode;


    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async void StartGameAsHost()
    {
        //// 匿名ユーザーとしてログインする。
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
        //var playerId = AuthenticationService.Instance.PlayerId;
        //Debug.Log($"Signed in. Player ID: {playerId}");

        //// サーバーの地域一覧を取得する。
        //Debug.Log("Host - Getting regions.");
        //var allRegions = await RelayService.Instance.ListRegionsAsync();
        //var regions = new List<Region>();
        //var regionOptions = new List<string>();
        //foreach (var r in allRegions)
        //{
        //    Debug.Log(r.Id + ": " + r.Description);
        //    regionOptions.Add(r.Id);
        //    regions.Add(r);
        //}

        //// 最適なサーバーの地域を自動選択する。
        //Debug.Log("Host - Creating an allocation.");
        ////string region = GetRegionOrQosDefault();
        //string region = "asia-northeast1";
        //Debug.Log($"The chosen region is: {region}");

        //// サーバーの割当を取得する。（ホスト用）
        //// Important: Once the allocation is created, you have ten seconds to BIND
        //allocation = await RelayService.Instance.CreateAllocationAsync(4, region);
        //var hostAllocationId = allocation.AllocationId;
        //var allocationRegion = allocation.Region;
        //Debug.Log($"Host Allocation ID: {hostAllocationId}, region: {allocationRegion}");

        //// ルーム参加用のコードを取得する。
        //joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocationId);
        //Debug.Log("Host - Got join code: " + joinCode);

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        allocation = await RelayService.Instance.CreateAllocationAsync(10);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        textJoinCodeForHost.text = joinCode;
        var ok = NetworkManager.Singleton.StartHost();
        Debug.Log($"StartHost: ok:{ok}");
    }



    public async void JoinGameAsClient()
    {
        var joinCode = inputJoinCodeForClient.text;
        //Debug.Log($"Joining game with join code {joinCode}");
        //// （クライアント用）ルーム参加用のコードを入力して参加する。
        //joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        //var playerAllocationId = joinAllocation.AllocationId;
        //Debug.Log("Player Allocation ID: " + playerAllocationId);

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        var ok =  NetworkManager.Singleton.StartClient();
        Debug.Log($"StartClient: ok:{ok}");
    }
}
