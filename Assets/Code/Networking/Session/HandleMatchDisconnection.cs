using Code.Helpers.UI;
using UnityEngine;

namespace Code.Networking.Session
{
    public class HandleMatchDisconnection : MonoBehaviour
    {
        private SessionManager _sessionManager;

        private void Awake()
        {
            _sessionManager = SessionManager.Instance;
            _sessionManager.ActiveSession.PlayerHasLeft += OnPlayerLeft;
        }

        private void OnDisable()
        {
            _sessionManager.ActiveSession.PlayerHasLeft -= OnPlayerLeft;
            UIUtilities.Instance.MessageOkBtn.onClick.RemoveAllListeners();
        }

        private void OnPlayerLeft(string playerId)
        {
            if (playerId != _sessionManager.ActiveSession.Host) return;
            
            _sessionManager.LeaveSession();
            
            UIUtilities.Instance.MessagePopUp("El anfitrión ha abandonado la partida, la partida será cancelada", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }
    }
}