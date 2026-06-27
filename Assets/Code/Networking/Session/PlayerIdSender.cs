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

            SessionManager.Instance.SessionChanged += OnPlayerJoined;
        }

        public override void OnNetworkDespawn()
        {
            SessionManager.Instance.SessionChanged -= OnPlayerJoined;
        }

        private void RegisterPlayerId()
        {
            if (SessionManager.Instance == null)
                return;

            if (!SessionManager.Instance.TryGetCurrentPlayerId(out string playerId))
            {
                Debug.LogWarning("Could not register player ID because current player ID is missing.");
                return;
            }

            ulong clientId = NetworkManager.LocalClient.ClientId;

            if (SessionManager.Instance.TryRegisterPlayerClientId(playerId, clientId))
                Debug.Log($"[Client] Self Registered: {playerId} with clientId: {clientId}");

            SendPlayerIdRpc(playerId);
        }

        private void OnPlayerJoined()
        {
            RegisterPlayerId();
        }
        
        [Rpc(SendTo.NotMe)]
        private void SendPlayerIdRpc(string playerId, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!SessionManager.Instance.TryRegisterPlayerClientId(playerId, clientId))
                return;

            Debug.Log($"Registered PlayerId {playerId} with ClientId {clientId}");
        }
    }
}