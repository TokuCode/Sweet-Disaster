using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Code.Helpers.UI;

namespace Code.Gameplay
{
    public class LoseTracker : NetworkBehaviour
    {
        public static LoseTracker Instance;

        //public HashSet<ulong> LostPlayers = new();
        
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

        public void ReportPlayerLoss(ulong clientId)
        {
            if (!IsServer) return;

            if (_sessionManager.ActiveSession.IsHost && _sessionManager.IsPracticeMode)
            {
                NetworkManager.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                return;
            }
            
            SendPlayerDataToStackRpc(clientId);
            
            var excludedIds = WinnersData.playerStatusDataStack.Select(p => p.ClientId);
            var remainingIds = NetworkManager.Singleton.ConnectedClientsList
                .Select(c => c.ClientId)
                .Except(excludedIds)
                .ToList();

            if (remainingIds.Count == 1)
            {
                SendPlayerDataToStackRpc(remainingIds[0]);
                
                if (_sessionManager.ActiveSession.IsHost && !_sessionManager.IsPracticeMode)
                    NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SendPlayerDataToStackRpc(ulong clientId)
        {
            var playerStatusData = new WinnersData.PlayerStatusData
            {
                ClientId = clientId,
                Lives = 0,              // TO-DO: Modify for player's actual info
                AccumulatedDmg = 0f     // TO-DO: Modify for player's actual info
            };
            
            WinnersData.playerStatusDataStack.Push(playerStatusData);
        }
    }
}