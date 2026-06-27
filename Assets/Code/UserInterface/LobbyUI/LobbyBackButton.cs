using System;
using Code.Networking.Session;
using UnityEngine;
using UnityEngine.UI;
using Code.Helpers.UI;
using Unity.Netcode;
using Unity.Collections;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyBackButton : NetworkBehaviour
    {
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (SessionManager.Instance == null || UIUtilities.Instance == null) return;
            
            backButton.onClick.AddListener(PerformReturnToMenu);
            backButton.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }
        
        private void PerformReturnToMenu()
        {
            ReturnToMenu();
            if (!SessionManager.Instance.IsLocalPlayerSessionHost) return;
            ReturnToMenuRpc();
        }
        
        [Rpc(SendTo.NotMe)]
        private void ReturnToMenuRpc()
        {
            SessionManager.Instance.LeaveSession();
            
            UIUtilities.Instance.MessagePopUp("El anfitrión abandonó la partida", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }

        private void ReturnToMenu()
        {
            if (SessionManager.Instance.TryGetCurrentPlayerId(out string playerId))
                SendPlayerLeavingRpc(playerId);
            SessionManager.Instance.LeaveSession();
        }

        [Rpc(SendTo.Server)]
        private void SendPlayerLeavingRpc(FixedString32Bytes playerId, RpcParams rpcParams = default)
        {
            string playerIdString = playerId.ToString();
            SessionManager.Instance.RemovePlayerClientId(playerIdString);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }
    }
}