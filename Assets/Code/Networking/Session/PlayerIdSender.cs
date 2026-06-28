using Unity.Netcode;
using UnityEngine;

namespace Code.Networking.Session
{
    public class PlayerIdSender : NetworkBehaviour
    {
        private SessionManager _sessionManager;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _sessionManager = SessionManager.Instance;

            if (_sessionManager.IsLanMode)
            {
                RegisterLanPlayer();
            }
            else
            {
                RegisterPlayerId();
                _sessionManager.SessionChanged += OnPlayerJoined;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_sessionManager != null && !_sessionManager.IsLanMode)
                _sessionManager.SessionChanged -= OnPlayerJoined;

            base.OnNetworkDespawn();
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
        
        private void RegisterLanPlayer()
        {
            if (!_sessionManager.TryGetCurrentPlayerId(out string playerId))
            {
                Debug.LogWarning("Could not register LAN player because current player ID is missing.");
                return;
            }

            string playerName = _sessionManager.GetOrCreateLocalLanPlayerName();

            SendLanPlayerInfoRpc(playerId, playerName);
        }
        
        [Rpc(SendTo.Server)]
        private void SendLanPlayerInfoRpc(string playerId, string playerName, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            string colorName = _sessionManager.GetExistingOrAvailableLanColorName(playerId);

            bool isHostPlayer = NetworkManager.Singleton.LocalClientId == clientId;

            _sessionManager.RegisterOrUpdateLanPlayer(
                playerId,
                clientId,
                playerName,
                colorName,
                string.Empty,
                isHostPlayer
            );

            BroadcastAllLanPlayers();
        }

        private void BroadcastAllLanPlayers()
        {
            foreach (var player in _sessionManager.GetSessionPlayers())
            {
                ReceiveLanPlayerInfoRpc(
                    player.PlayerId,
                    player.ClientId,
                    player.PlayerName,
                    player.PlayerColorName,
                    player.CharacterName,
                    player.IsHost
                );
            }
        }

        [Rpc(SendTo.NotMe)]
        private void ReceiveLanPlayerInfoRpc(
            string playerId,
            ulong clientId,
            string playerName,
            string colorName,
            string characterName,
            bool isHost)
        {
            _sessionManager.RegisterOrUpdateLanPlayer(
                playerId,
                clientId,
                playerName,
                colorName,
                characterName,
                isHost
            );
        }
    }
}