using System;
using System.Linq;
using Code.UserInterface.LobbyUI;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Multiplayer;
using Code.Networking.Session;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Code.Helpers.UI;
using Code.Gameplay;

namespace Code.UserInterface.PostGameUI
{
    public class PostGameUIManager : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI winnerTitle;
        
        [SerializeField] private List<PlayerSlotUI> playerSlots;
        [SerializeField] private List<TextMeshProUGUI> playersPositionsText;

        private WinnersData.PlayerStatusData _cachedPlayerStatusData;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button playAgainButton;
        [SerializeField] private UnityEngine.UI.Button returnToLobbyButton;
        [SerializeField] private UnityEngine.UI.Button exitButton;
        
        [SerializeField] private TextMeshProUGUI statusText;

        private NetworkList<ulong> _playersReadyToRestart = new(new List<ulong>());
        
        private SessionManager _sessionManager;
        private CancellationTokenSource  _cancellationTokenSource;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
            
            exitButton.onClick.AddListener(PerformReturnToMenu);
            
            _sessionManager = SessionManager.Instance;
            _cancellationTokenSource = new CancellationTokenSource();

            if (!IsServer) return;
            
            _playersReadyToRestart.OnListChanged += ListChanged;
            PopulatePlayers();
            PopulatePlayersRpc();
        }

        private void OnDisable()
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
            exitButton.onClick.RemoveListener(PerformReturnToMenu);
            UIUtilities.Instance.MessageOkBtn.onClick.RemoveAllListeners();
            
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            
            if (!IsServer) return;
            _playersReadyToRestart.OnListChanged -= ListChanged;
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
            _playersReadyToRestart.Add(NetworkManager.LocalClientId);
            if (IsClient && !IsHost) SendReadyStatusRpc();
            
            playAgainButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
        }

        private void ListChanged(NetworkListEvent<ulong> listEvent)
        {
            if (_playersReadyToRestart.Count < _sessionManager.ActiveSession.PlayerCount) return;
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
        }

        [Rpc(SendTo.Server)]
        private void SendReadyStatusRpc()
        {
            _playersReadyToRestart.Add(NetworkManager.LocalClientId);
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
            UIUtilities.Instance.LoadScene("MainMenu");
        }

        [Rpc(SendTo.NotMe)]
        private void ReturnToMenuRpc()
        {
            SessionManager.Instance.LeaveSession();
            
            UIUtilities.Instance.MessagePopUp("El anfitrión abandonó la partida", true);
            UIUtilities.Instance.MessageOkBtn.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }

        private void PerformReturnToLobby()
        {
            
        }

        private void ReturnToLobby()
        {
            
        }
    }
}