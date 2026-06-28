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

            var players = _sessionManager.GetSessionPlayers();

            foreach (var item in lobbyPlayerListItems)
            {
                if (item == null) continue;
                item.ResetItem();
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