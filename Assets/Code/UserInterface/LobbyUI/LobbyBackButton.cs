using System.Threading.Tasks;
using Code.Networking.Session;
using UnityEngine;
using UnityEngine.UI;
using Code.Helpers.UI;
using Unity.Netcode;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyBackButton : NetworkBehaviour
    {
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (SessionManager.Instance == null || UIUtilities.Instance == null) return;
            
            backButton.onClick.AddListener(PerformReturnToMenu);
        }
        
        private async void PerformReturnToMenu()
        {
            bool wasHost = SessionManager.Instance.IsLocalPlayerSessionHost;

            if (wasHost)
            {
                ReturnToMenuRpc();
            }

            await ReturnToMenuAsync();

            UIUtilities.Instance.LoadScene("MainMenu");
        }
        
        [Rpc(SendTo.NotMe)]
        private void ReturnToMenuRpc()
        {
            _ = HandleHostReturnToMenuAsync();
        }
        
        private async Task HandleHostReturnToMenuAsync()
        {
            await SessionManager.Instance.LeaveSessionAsync();
            
            UIUtilities.Instance.MessagePopUp("El anfitrión abandonó la partida", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() =>
            {
                UIUtilities.Instance.LoadScene("MainMenu");
            });
        }

        private async Task ReturnToMenuAsync()
        {
            if (SessionManager.Instance.TryGetCurrentPlayerId(out string playerId))
            {
                SendPlayerLeavingRpc(playerId);
            }

            await SessionManager.Instance.LeaveSessionAsync();
        }

        [Rpc(SendTo.Server)]
        private void SendPlayerLeavingRpc(string playerId, RpcParams rpcParams = default)
        {
            SessionManager.Instance.RemovePlayerClientId(playerId);
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(PerformReturnToMenu);
        }
    }
}