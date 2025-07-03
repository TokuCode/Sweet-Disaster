using Unity.Netcode;
using UnityEngine;

namespace Code.Networking.Session
{
    public class PlayerIdSender : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            RegisterPlayerId();

            SessionManager.Instance.ActiveSession.Changed += OnPlayerJoined;
        }

        public override void OnNetworkDespawn()
        {
            SessionManager.Instance.ActiveSession.Changed -= OnPlayerJoined;
        }

        private void RegisterPlayerId()
        {
            string playerId = SessionManager.Instance.ActiveSession.CurrentPlayer.Id;
            Debug.Log($"[Client] Sending PlayerId: {playerId} to server...");
            SendPlayerIdRpc(playerId);
        }

        private void OnPlayerJoined()
        {
            RegisterPlayerId();
        }

        [Rpc(SendTo.Everyone)]
        private void SendPlayerIdRpc(string playerId, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if(!SessionManager.Instance.PlayerIdToClientId.TryAdd(playerId, clientId)) return;
            Debug.Log($"Registered PlayerId {playerId} with ClientId {clientId}");
        }
    }
}