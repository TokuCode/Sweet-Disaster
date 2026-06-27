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
            _sessionManager.SessionChanged += UpdatePlayerList;
        }

        private void OnDisable()
        {
            if (!_sessionManager.HasActiveSession) return;
            _sessionManager.SessionChanged -= UpdatePlayerList;
        }

        private void UpdatePlayerList()
        {
            var players = _sessionManager.GetPlayers().ToList();

            foreach (var item in lobbyPlayerListItems)
                item.ResetItem();
            
            for (int i = 0; i < players.Count; i++)
            {
                string playerName = _sessionManager.GetPlayerName(players[i]);
                Color playerColor = _sessionManager.GetPlayerColor(players[i]);
                
                lobbyPlayerListItems[i].Set(players[i].Id, playerName, playerColor);
            }
        }
    }   
}