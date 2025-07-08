using System;
using System.Linq;
using Code.UserInterface.LobbyUI;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using Code.Networking.Session;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Code.Helpers.UI;
using Code.Gameplay;

namespace Code.UserInterface.PostGameUI
{
    public class PostGameUIManager : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI winnerTitle;
        
        [SerializeField] private List<PlayerSlotUI> playerSlots;
        [SerializeField] private List<TextMeshProUGUI> playersPositionsText;

        private int _numberOfPlayers;
        private WinnersData.PlayerStatusData _cachedPlayerStatusData;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button playAgainButton;
        [SerializeField] private UnityEngine.UI.Button returnToLobbyButton;
        [SerializeField] private UnityEngine.UI.Button exitButton;
        
        [SerializeField] private TextMeshProUGUI statusText;

        private NetworkList<ulong> _playersReadyToRestart = new(new List<ulong>());
        private NetworkList<ulong> _playersReadyToReturn = new(new List<ulong>());
        
        private SessionManager _sessionManager;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyPressed);
            exitButton.onClick.AddListener(PerformReturnToMenu);
            
            _sessionManager = SessionManager.Instance;

            if (!IsServer) return;

            _numberOfPlayers = WinnersData.playerStatusDataStack.Count;
            _playersReadyToRestart.OnListChanged += RestartListChanged;
            _playersReadyToReturn.OnListChanged += ReturnListChanged;
            PopulatePlayers();
            PopulatePlayersRpc();
        }

        private void OnDisable()
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
            returnToLobbyButton.onClick.RemoveListener(OnReturnToLobbyPressed);
            exitButton.onClick.RemoveListener(PerformReturnToMenu);
            
            if (!IsServer) return;
            _playersReadyToRestart.OnListChanged -= RestartListChanged;
            _playersReadyToReturn.OnListChanged -= ReturnListChanged;
        }

        private void PopulatePlayers()
        {
            for (int i = 0; i < _sessionManager.ActiveSession.PlayerCount; i++)
            {
                var playerStatusData = WinnersData.playerStatusDataStack.Pop();
                
                var playerId = _sessionManager.PlayerIdToClientId.FirstOrDefault(p => p.Value == playerStatusData.ClientId).Key;
                var player = _sessionManager.ActiveSession.Players.FirstOrDefault(p => p.Id == playerId);
                
                if (player == null) return;

                var playerName = _sessionManager.playerInfo.GetPropertyValue(player, _sessionManager.PlayerNameKey);
                var playerColor = _sessionManager.playerInfo.GetColor(player);
                
                playerSlots[i].SetSlot(playerName, playerColor);
                
                if (i == 0)
                    winnerTitle.text = $"Ganador: {playerName}";
                
                playersPositionsText[i].text = (i + 1).ToString();
                
                if (i > 0)
                {
                    if (_cachedPlayerStatusData.Lives == playerStatusData.Lives 
                        && Mathf.Approximately(_cachedPlayerStatusData.AccumulatedDmg, playerStatusData.AccumulatedDmg))
                    {
                        if (_sessionManager.ActiveSession.PlayerCount == 2 || i == 1)
                            winnerTitle.text = "Empate";

                        playersPositionsText[i].text = i.ToString();
                    }
                }
                
                _cachedPlayerStatusData.Lives = playerStatusData.Lives;
                _cachedPlayerStatusData.AccumulatedDmg = playerStatusData.AccumulatedDmg;
            }
        }

        [Rpc(SendTo.NotMe)]
        private void PopulatePlayersRpc()
        {
            PopulatePlayers();
        }
        
        private void OnPlayAgainPressed()
        {
            playAgainButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
            
            if (IsServer) _playersReadyToRestart.Add(NetworkManager.LocalClientId);
            if (IsClient && !IsHost) SendReadyToQuitStatusRpc();
        }
        
        private void OnReturnToLobbyPressed()
        {
            returnToLobbyButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
            
            if (IsServer) _playersReadyToReturn.Add(NetworkManager.LocalClientId);
            if (IsClient && !IsHost) SendReadyToReturnStatusRpc();
        }

        private void RestartListChanged(NetworkListEvent<ulong> listEvent)
        {
            if (_playersReadyToRestart.Count < _numberOfPlayers
                && _sessionManager.ActiveSession.PlayerCount > 1) return;
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
        }

        private void ReturnListChanged(NetworkListEvent<ulong> listEvent)
        {
            if (_playersReadyToReturn.Count < _numberOfPlayers
                && _sessionManager.ActiveSession.PlayerCount > 1) return;
            ResetCharacterProperty();
            ResetCharacterPropertyRpc();
        }

        private void Update()
        {
            if (_sessionManager.ActiveSession == null) return;
            if (!IsServer) return;

            foreach (var player in _sessionManager.ActiveSession.Players)
            {
                if (!player.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var charProp)) return;
                if (charProp.Value != String.Empty) return;
            }
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyTest", LoadSceneMode.Single);
        }

        [Rpc(SendTo.NotMe)]
        private void ResetCharacterPropertyRpc()
        {
            ResetCharacterProperty();
        }

        private async void ResetCharacterProperty()
        {
            try
            {
                _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerCharacterKey,
                    new PlayerProperty(String.Empty, VisibilityPropertyOptions.Member));

                await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
                returnToLobbyButton.interactable = true;
                statusText.text = String.Empty;
                if (IsServer) _playersReadyToReturn.Remove(NetworkManager.LocalClientId);
                if (IsClient && !IsHost) SendDeleteReadyToReturnRpc();
                
                UIUtilities.Instance.MessagePopUp("Hubo un error al reiniciar el personaje del jugador", true);
            }
        }

        [Rpc(SendTo.Server)]
        private void SendReadyToQuitStatusRpc()
        {
            _playersReadyToRestart.Add(NetworkManager.LocalClientId);
        }

        [Rpc(SendTo.Server)]
        private void SendReadyToReturnStatusRpc()
        {
            _playersReadyToReturn.Add(NetworkManager.LocalClientId);
        }

        [Rpc(SendTo.Server)]
        private void SendPlayerLeavingRpc(RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            _playersReadyToRestart.Remove(clientId);
            _playersReadyToReturn.Remove(clientId);
        }

        [Rpc(SendTo.Server)]
        private void SendDeleteReadyToReturnRpc(RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            _playersReadyToReturn.Remove(clientId);
        }
        
        private void PerformReturnToMenu()
        {
            ReturnToMenu();
            if (!_sessionManager.ActiveSession.IsHost) return;
            ReturnToMenuRpc();
        }

        private void ReturnToMenu()
        {
            SessionManager.Instance.LeaveSession();
            SendPlayerLeavingRpc();
            UIUtilities.Instance.LoadScene("MainMenu");
        }

        [Rpc(SendTo.NotMe)]
        private void ReturnToMenuRpc()
        {
            SessionManager.Instance.LeaveSession();
            
            UIUtilities.Instance.MessagePopUp("El anfitrión abandonó la partida", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }
    }
}