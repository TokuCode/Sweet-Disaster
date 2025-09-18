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
            _sessionManager.ActiveSession.Changed += UpdatePlayerList;
        }

        private void OnDisable()
        {
            if (_sessionManager.ActiveSession == null) return;
            _sessionManager.ActiveSession.Changed -= UpdatePlayerList;
        }

        private void UpdatePlayerList()
        {
            var players = _sessionManager.ActiveSession.Players.ToList();

            foreach (var item in lobbyPlayerListItems)
                item.ResetItem();
            
            for (int i = 0; i < players.Count; i++)
            {
                string playerName = _sessionManager.playerInfo.GetPropertyValue(players[i], _sessionManager.PlayerNameKey);
                Color playerColor = _sessionManager.playerInfo.GetColor(players[i]);
                
                lobbyPlayerListItems[i].Set(players[i].Id, playerName, playerColor);
            }
        }
    }   
}