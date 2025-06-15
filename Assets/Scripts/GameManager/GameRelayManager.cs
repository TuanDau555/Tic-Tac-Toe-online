using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// We need to use Persistent cause Data need to pass through scene
/// </remark> 
public class GameRelayManager : SingletonPersistent<GameRelayManager>
{
    private string joinCode;
    private string ip;
    private int port;
    private byte[] connectionData;
    private System.Guid allocationId;
    public async Task<string> CreateRelay()
    {
        try
        {
            // Because max player is alway 2 so...
            // ...We just need 1 connection to the room
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // we use dtls protocol 
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // After the host click Start game
            // We create allocation and start host
            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinRelay()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
