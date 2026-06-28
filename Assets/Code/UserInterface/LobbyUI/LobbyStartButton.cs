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

            if (_sessionManager != null)
                _sessionManager.PlayerPropertiesChanged += UpdateButtonState;
        }

        private void Start()
        {
            UpdateButtonState();
        }

        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(StartGame);

            if (_sessionManager != null)
                _sessionManager.PlayerPropertiesChanged -= UpdateButtonState;
        }
        
        private void UpdateButtonState()
        {
            if (_sessionManager == null)
                return;

            bool canStart =
                _sessionManager.IsLocalPlayerSessionHost &&
                (
                    _sessionManager.PlayerCount > 1 ||
                    (_sessionManager.PlayerCount == 1 && _sessionManager.IsPracticeMode)
                ) &&
                _sessionManager.HaveAllPlayersSelectedCharacters();

            startGameButton.interactable = canStart;
        }
        
        private void StartGame()
        {
            if (!_sessionManager.IsLocalPlayerSessionHost)
                return;

            NetworkManager.Singleton.SceneManager.LoadScene(
                _sessionManager.IsPracticeMode ? "Tutorial" : "MultiplayerTest",
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}