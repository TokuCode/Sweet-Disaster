using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Code.Helpers.Singleton;
using Code.Networking.Session;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyPlayerList : Singleton<LobbyPlayerList>
    {
        private SessionManager _sessionManager;
        [SerializeField] private List<LobbyPlayerListItem> lobbyPlayerListItems;

        protected override void Awake()
        {
            base.Awake();
            
            if (SessionManager.Instance == null) return;
            _sessionManager = SessionManager.Instance;
        }

        private void Start()
        {
            UpdatePlayerList();
            if (_sessionManager != null)
                _sessionManager.SessionChanged += UpdatePlayerList;
        }

        private void OnDisable()
        {
            if (_sessionManager != null)
                _sessionManager.SessionChanged -= UpdatePlayerList;
        }
        
        private void OnDestroy()
        {
            if (_sessionManager != null)
                _sessionManager.SessionChanged -= UpdatePlayerList;
        }

        private void UpdatePlayerList()
        {
            if (this == null || !gameObject.scene.isLoaded)
                return;

            if (_sessionManager == null)
                return;

            var players = _sessionManager.GetLobbyPlayers();

            foreach (var item in lobbyPlayerListItems)
            {
                if (item == null) continue;
                item.ResetItem();
            }
            
            Debug.Log($"[LobbyPlayerList] LocalClientId: {Unity.Netcode.NetworkManager.Singleton.LocalClientId}, PlayersCount: {players.Count}");

            for (int i = 0; i < players.Count; i++)
            {
                Debug.Log($"[LobbyPlayerList] Slot {i}: {players[i].PlayerName}, ClientId: {players[i].ClientId}, IsCurrent: {players[i].IsCurrentPlayer}");
            }

            for (int i = 0; i < players.Count && i < lobbyPlayerListItems.Count; i++)
            {
                if (lobbyPlayerListItems[i] == null) continue;

                var player = players[i];

                lobbyPlayerListItems[i].Set(
                    player.PlayerId,
                    player.PlayerName,
                    player.PlayerColor
                );
            }
        }
    }   
}