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
    private string m_joinCode;

    public async Task<string> CreateRelay()
    {
        try
        {
            // Because max player is alway 2 so...
            // ...We just need 1 connection to the room
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            m_joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // we use dtls protocol 
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // After the host click Start game or after we create room...
            // ...we create allocation and start host
            NetworkManager.Singleton.StartHost();

            Debug.Log("Allocation created: " + allocation + " join Code: " + m_joinCode);
            return m_joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    /// <summary>
    /// It just simple Player Join Relay which is created by the host
    /// </summary>
    /// <returns></returns>
    public async void JoinRelay(string joinCode)
    {
        try
        {
            m_joinCode = joinCode;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.StartClient();

            Debug.Log("Join Relay: " + joinAllocation);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
