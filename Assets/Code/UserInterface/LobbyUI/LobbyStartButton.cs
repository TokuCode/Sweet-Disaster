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

            _sessionManager.PlayerPropertiesChanged += UpdateButtonState;
        }

        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(StartGame);
            if (!_sessionManager.HasActiveSession) return;
            _sessionManager.PlayerPropertiesChanged -= UpdateButtonState;
        }
        
        private bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in _sessionManager.GetPlayers())
            {
                if (string.IsNullOrEmpty(_sessionManager.GetPlayerCharacter(player)))
                    return false;
            }
            return true;
        }

        private void UpdateButtonState()
        {
            if ((_sessionManager.IsLocalPlayerSessionHost && _sessionManager.PlayerCount > 1) ||
                (_sessionManager.IsLocalPlayerSessionHost && _sessionManager.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                startGameButton.interactable = AllPlayersHaveSelectedCharacters();
        }
        
        private void StartGame()
        {
            if (!_sessionManager.IsLocalPlayerSessionHost) return;
            NetworkManager.Singleton.SceneManager.LoadScene(_sessionManager.IsPracticeMode? "Tutorial" : "MultiplayerTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}