using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(
                conn => conn.ConnectionType == "dtls"
            );

            ip = dtlsEndpoint.Host;
            port = dtlsEndpoint.Port;
            connectionData = allocation.ConnectionData;
            allocationId = allocation.AllocationId;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

        return joinCode;
    }
}
