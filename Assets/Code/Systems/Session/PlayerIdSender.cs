using System.Collections;
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
            Debug.Log(playerId);
            Debug.Log($"[Client] Sending PlayerId: {playerId} to server...");
            SendPlayerIdServerRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendPlayerIdServerRpc(string playerId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            SessionManager.Instance.playerIdToClientId[playerId] = clientId;
            Debug.Log($"Registered PlayerId {playerId} with ClientId {clientId}");
        }
    }
}