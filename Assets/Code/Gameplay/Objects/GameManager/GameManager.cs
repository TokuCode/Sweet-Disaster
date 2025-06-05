using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Code.Helpers.UI;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Gameplay.Objects
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;

        public HashSet<ulong> LostPlayers = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            SessionManager.Instance.ActiveSession.Changed += ChangeScene;
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
                var playerIdMap = SessionManager.Instance.PlayerIdToClientId;
                string winnerPlayerId = playerIdMap.FirstOrDefault(p => p.Value == winnerClientId).Key;

                if (!string.IsNullOrEmpty(winnerPlayerId))
                {
                    var session = SessionManager.Instance.ActiveSession.AsHost();

                    session.SetProperty(
                        SessionManager.Instance.SessionKeys[SessionPropertyKeys.Winner],
                        new SessionProperty(winnerPlayerId, VisibilityPropertyOptions.Member)
                    );

                    await session.SavePropertiesAsync();
                }
                else
                {
                    Debug.LogWarning($"[Server] Could not find PlayerId for winner ClientId {winnerClientId}");
                }
            }
        }


        private void ChangeScene()
        {
            if (SceneManager.GetActiveScene().name != "MultiplayerTest") return;
            UIUtilities.Instance.FadeIn(UIUtilities.Instance.TransitionPanel, UIUtilities.Instance.TransitionDuration);
            if (SessionManager.Instance.ActiveSession.IsHost)
                NetworkManager.Singleton.SceneManager.LoadScene("PostGame", LoadSceneMode.Single);
        }
    }
}