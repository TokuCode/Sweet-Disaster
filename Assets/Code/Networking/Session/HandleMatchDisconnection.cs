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
            if (_sessionManager == null) return;
            _sessionManager.PlayerLeft -= OnPlayerLeft;
        }

        private bool _isLeaving;

        private async void OnPlayerLeft(string playerId)
        {
            if (_isLeaving) return;
            _isLeaving = true;

            bool hostLeft = _sessionManager.IsHostPlayer(playerId);

            string message = hostLeft
                ? "El anfitrión ha abandonado la partida, la partida será cancelada"
                : "Un jugador ha abandonado la partida, la partida será cancelada";

            await _sessionManager.LeaveSessionAsync();

            UIUtilities.Instance.MessagePopUp(message, true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() =>
            {
                UIUtilities.Instance.LoadScene("MainMenu");
            });
        }
    }
}