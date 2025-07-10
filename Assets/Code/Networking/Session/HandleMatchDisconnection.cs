using Code.Helpers.UI;
using Unity.Services.Multiplayer;
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
            if (_sessionManager.ActiveSession == null) return;
            _sessionManager.ActiveSession.PlayerHasLeft -= OnPlayerLeft;
        }

        private void OnPlayerLeft(string playerId)
        {
            _sessionManager.LeaveSession();
            UIUtilities.Instance.MessagePopUp("Un jugador ha abandonado la partida, la partida será cancelada", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
            
            if (playerId != _sessionManager.ActiveSession.Host) return;
            
            _sessionManager.LeaveSession();
            UIUtilities.Instance.MessagePopUp("El anfitrión ha abandonado la partida, la partida será cancelada", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }
    }
}