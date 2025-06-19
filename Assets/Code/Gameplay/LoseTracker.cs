using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Gameplay
{
    public class LoseTracker : NetworkBehaviour
    {
        public static LoseTracker Instance;

        public HashSet<ulong> LostPlayers = new();
        
        private SessionManager _sessionManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _sessionManager = SessionManager.Instance;
        }

        private async void Start()
        {
            _sessionManager.ActiveSession.CurrentPlayer.SetProperty(
                _sessionManager.PlayerReadyToRestart,
                new PlayerProperty("false", VisibilityPropertyOptions.Member));
            await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
        }

        private void OnDisable() => Destroy(gameObject);

        public async void ReportPlayerLoss(ulong clientId)
        {
            if (!IsServer) return;

            LostPlayers.Add(clientId);

            var alive = NetworkManager.Singleton.ConnectedClientsList
                .Select(c => c.ClientId)
                .Except(LostPlayers)
                .ToList();

            if (alive.Count == 1)
            {
                ulong winnerClientId = alive[0];
                Debug.Log($"[Server] Winner determined: Client {winnerClientId}");

                // Get PlayerId from mapping
                var playerIdMap = _sessionManager.PlayerIdToClientId;
                string winnerPlayerId = playerIdMap.FirstOrDefault(p => p.Value == winnerClientId).Key;

                if (!string.IsNullOrEmpty(winnerPlayerId))
                {
                    var session = _sessionManager.ActiveSession.AsHost();

                    session.SetProperty(
                        _sessionManager.WinnerPropertyKey,
                        new SessionProperty(winnerPlayerId, VisibilityPropertyOptions.Member)
                    );

                    await session.SavePropertiesAsync();
                    
                    if (_sessionManager.ActiveSession.IsHost)
                        NetworkManager.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogWarning($"[Server] Could not find PlayerId for winner ClientId {winnerClientId}");
                }
            }
        }
    }
}