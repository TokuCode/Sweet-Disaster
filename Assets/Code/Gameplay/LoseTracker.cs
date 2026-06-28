using Unity.Netcode;
using System.Linq;
using Code.Networking.Session;
using UnityEngine.SceneManagement;
using Code.Gameplay.Character;
using UnityEngine;

namespace Code.Gameplay
{
    public class LoseTracker : NetworkBehaviour
    {
        public static LoseTracker Instance;
        
        private SessionManager _sessionManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _sessionManager = SessionManager.Instance;
        }

        private void Start()
        {
            WinnersData.playerStatusDataStack.Clear();
        }

        private void OnDisable() => Destroy(gameObject);

        public void ReportPlayerLoss(ulong clientId, int lives, float damage)
        {
            if (!IsServer) return;
            
            SendPlayerDataToStackRpc(clientId, lives, damage);
            
            var excludedIds = WinnersData.playerStatusDataStack.Select(p => p.ClientId);
            var remainingIds = NetworkManager.Singleton.ConnectedClientsList
                .Select(c => c.ClientId)
                .Except(excludedIds)
                .ToList();
            
            if (remainingIds.Count == 1)
            {
                var remainingPlayerInfo = PlayerVisibility.Instance.Players.FirstOrDefault(playerInfo => playerInfo.player.clientId == (int)remainingIds[0]);
                PlayerController remainingPlayer = remainingPlayerInfo.player;
                if (remainingPlayer != null)
                {
                    remainingPlayer.Dependencies.TryGetFeature(out LoseReporterPadded reporter);
                    reporter.ReportDefeat();
                }
            }
            else if (remainingIds.Count == 0)
            {
                if (_sessionManager.IsLocalPlayerSessionHost && !_sessionManager.IsPracticeMode)
                    NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SendPlayerDataToStackRpc(ulong clientId, int lives, float damage)
        {
            var playerStatusData = new WinnersData.PlayerStatusData
            {
                ClientId = clientId,
                Lives = lives,         
                AccumulatedDmg = damage
            };
            
            WinnersData.playerStatusDataStack.Push(playerStatusData);
        }
    }
}