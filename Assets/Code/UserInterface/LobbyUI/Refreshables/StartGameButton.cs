using System;
using UnityEngine;
using UnityEngine.UI;
using Code.Networking.Session;
using Unity.Netcode;

namespace Code.UserInterface.LobbyUI
{
    public class StartGameButton : Refreshable
    {
        [SerializeField] private Button startGameButton;
        
        private SessionManager _sessionManager;

        private void Awake()
        {
            _sessionManager = SessionManager.Instance;
            startGameButton.onClick.AddListener(StartGame);
        }

        private void OnDisable() => startGameButton.onClick.RemoveListener(StartGame);

        public override void Refresh()
        {
            if ((_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount > 1) ||
                (_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                startGameButton.interactable = LobbyUIManager.Instance.AllPlayersHaveSelectedCharacters();
        }

        private void StartGame()
        {
            if (!_sessionManager.ActiveSession.IsHost) return;
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}