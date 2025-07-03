using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Code.Gameplay.Character;
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

        public void ReportPlayerLoss(ulong clientId, int lives, float damage)
        {
            if (!IsServer) return;

            if (_sessionManager.ActiveSession.IsHost && _sessionManager.IsPracticeMode)
            {
                NetworkManager.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                return;
            }
            
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
                if (_sessionManager.ActiveSession.IsHost && !_sessionManager.IsPracticeMode)
                    NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SendPlayerDataToStackRpc(ulong clientId, int lives, float damage, bool isWinner = false)
        {
            var playerStatusData = new WinnersData.PlayerStatusData
            {
                ClientId = clientId,
                Lives = lives,         
                AccumulatedDmg = damage
            };
            
            WinnersData.playerStatusDataStack.Push(playerStatusData);
            
            /*
            Debug.Log($"[Sent] {playerStatusData.Lives}");
            Debug.Log($"[Sent] {playerStatusData.Lives}");
            Debug.Log($"[Received] {WinnersData.playerStatusDataStack.Peek().Lives}");
            Debug.Log($"[Received] {WinnersData.playerStatusDataStack.Peek().AccumulatedDmg}");*/
        }
    }
}