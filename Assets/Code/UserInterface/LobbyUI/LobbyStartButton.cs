using UnityEngine;
using UnityEngine.UI;
using Code.Networking.Session;
using Unity.Netcode;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyStartButton : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        
        private SessionManager _sessionManager;

        private void Awake()
        {
            _sessionManager = SessionManager.Instance;
            startGameButton.onClick.AddListener(StartGame);

            _sessionManager.ActiveSession.PlayerPropertiesChanged += UpdateButtonState;
        }

        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(StartGame);
            if (_sessionManager.ActiveSession == null) return;
            _sessionManager.ActiveSession.PlayerPropertiesChanged -= UpdateButtonState;
        }
        
        private bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in _sessionManager.ActiveSession.Players)
            {
                if (string.IsNullOrEmpty(_sessionManager.playerInfo.GetPropertyValue(player, _sessionManager.PlayerCharacterKey)))
                    return false;
            }
            return true;
        }

        private void UpdateButtonState()
        {
            if ((_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount > 1) ||
                (_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                startGameButton.interactable = AllPlayersHaveSelectedCharacters();
        }
        
        private void StartGame()
        {
            if (!_sessionManager.ActiveSession.IsHost) return;
            NetworkManager.Singleton.SceneManager.LoadScene(_sessionManager.IsPracticeMode? "Tutorial" : "MultiplayerTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}