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
        [SerializeField] private UnityEngine.UI.Button exitButton;
        
        [SerializeField] private TextMeshProUGUI statusText;

        /*private NetworkList<bool> playersReadyToRestart = new(new[] { false, false, false, false }, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);*/

        //private List<string> playersReady = new();
        
        private SessionManager _sessionManager;
        private CancellationTokenSource  _cancellationTokenSource;

        private void Awake()
        {
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
            exitButton.onClick.AddListener(ReturnToLobby);
            
            _sessionManager = SessionManager.Instance;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override async void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                PopulatePlayers();
                PopulatePlayersRpc();
            }
            
            try
            {
                _sessionManager.ActiveSession.CurrentPlayer.SetProperty(
                    _sessionManager.PlayerReadyToRestart,
                    new PlayerProperty("false", VisibilityPropertyOptions.Member));
                await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo actualizar las propiedades del jugador", true);
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
            exitButton.onClick.RemoveListener(ReturnToLobby);
            
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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
                
                if (_cachedPlayerStatusData.Lives == playerStatusData.Lives 
                    && Mathf.Approximately(_cachedPlayerStatusData.AccumulatedDmg, playerStatusData.AccumulatedDmg)
                    && i > 0)
                {
                    if (_sessionManager.ActiveSession.PlayerCount == 2 || i == 1)
                        winnerTitle.text = "Empate";

                    playersPositionsText[i].text = i.ToString();
                }
                else
                {
                    winnerTitle.text = $"Ganador: {playerName}";
                    playersPositionsText[i].text = (i + 1).ToString();
                }
                
                _cachedPlayerStatusData = playerStatusData;
            }
        }

        [Rpc(SendTo.NotMe)]
        private void PopulatePlayersRpc()
        {
            PopulatePlayers();
        }
        
        private async void OnPlayAgainPressed()
        {
            try
            {
                _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerReadyToRestart,
                    new PlayerProperty("true", VisibilityPropertyOptions.Member));
                await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("Ha ocurrido un error al actualizar las propiedades del jugador.", false);
            }

            /*if (IsOwner && IsClient)
            {
                SendReadyStatusRpc();
                Debug.Log(playersReady);
            }*/
            
            playAgainButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
            CheckAllReadyToRestart();
        }

        [Rpc(SendTo.Server)]
        private void SendReadyStatusRpc()
        {
            //playersReady.Add(_sessionManager.ActiveSession.CurrentPlayer.Id);
        }
        
        private async void CheckAllReadyToRestart()
        {
            Debug.Log("Checking if it should restart...");
            var session = _sessionManager.ActiveSession;
            var readyKey = _sessionManager.PlayerReadyToRestart;
            var token = _cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool allReady = session.Players.All(player =>
                        player.Properties.TryGetValue(readyKey, out var readyProp) &&
                        readyProp.Value == "true"
                    );

                    if (allReady && session.PlayerCount > 1)
                    {
                        Debug.Log("All players are ready. Restarting game...");
                    
                        if (_sessionManager.ActiveSession.IsHost)
                            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                        
                        break;
                    }

                    Debug.Log("One of the players is not ready to restart.");
                    await Task.Delay(1000); // check every second
                }
            }
            catch (TaskCanceledException)
            {
                Debug.Log("Restart check cancelled.");
            }
        }
        
        private void ReturnToLobby()
        {
            SessionManager.Instance.LeaveSession();
            NetworkManager.Singleton.Shutdown();
            UIUtilities.Instance.LoadScene("MainMenu");
        }
    }
}