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
            _sessionManager.PlayerLeft += OnPlayerLeft;
        }

        private void OnDisable()
        {
            if (!_sessionManager.HasActiveSession) return;
            _sessionManager.PlayerLeft -= OnPlayerLeft;
        }

        private void OnPlayerLeft(string playerId)
        {
            _sessionManager.LeaveSession();
            UIUtilities.Instance.MessagePopUp("Un jugador ha abandonado la partida, la partida será cancelada", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
            
            if (!_sessionManager.IsHostPlayer(playerId)) return;
            
            _sessionManager.LeaveSession();
            UIUtilities.Instance.MessagePopUp("El anfitrión ha abandonado la partida, la partida será cancelada", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }
    }
}