using Unity.Netcode;
using UnityEngine;

namespace Code.Networking.Session
{
    public class PlayerIdSender : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            string playerId = SessionManager.Instance.ActiveSession.CurrentPlayer.Id;
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