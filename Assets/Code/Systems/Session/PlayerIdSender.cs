using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

namespace Code.Systems.Session
{
    public class PlayerIdSender : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[Client] Sending PlayerId: {playerId} to server...");
            SendPlayerIdServerRpc(playerId);
        }

        [Rpc(SendTo.Server)]
        private void SendPlayerIdServerRpc(string playerId, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            SessionManager.Instance.PlayerIdToClientId[playerId] = clientId;
            Debug.Log($"Registered PlayerId {playerId} with ClientId {clientId}");
        }
    }
}