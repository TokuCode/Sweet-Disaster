using System;
using Code.UserInterface.LobbyUI;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Networking.Session;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Code.Helpers.UI;
using Code.Gameplay;
using Unity.Collections;

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
            for (int i = 0; i < _sessionManager.PlayerCount; i++)
            {
                if (WinnersData.playerStatusDataStack.Count == 0)
                {
                    Debug.LogWarning("[PostGameUI] No more player status data available.");
                    return;
                }

                var playerStatusData = WinnersData.playerStatusDataStack.Pop();

                if (!_sessionManager.TryGetSessionPlayerByClientId(playerStatusData.ClientId, out var sessionPlayer))
                {
                    Debug.LogWarning($"[PostGameUI] Could not find session player data for clientId: {playerStatusData.ClientId}");
                    return;
                }

                string playerName = sessionPlayer.PlayerName;
                Color playerColor = sessionPlayer.PlayerColor;
                
                playerSlots[i].SetSlot(playerName, playerColor);
                
                if (i == 0)
                    winnerTitle.text = $"{playerName}";
                
                playersPositionsText[i].text = (i + 1).ToString();
                
                if (i > 0)
                {
                    if (_cachedPlayerStatusData.Lives == playerStatusData.Lives 
                        && Mathf.Approximately(_cachedPlayerStatusData.AccumulatedDmg, playerStatusData.AccumulatedDmg))
                    {
                        if (_sessionManager.PlayerCount == 2 || i == 1)
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
            Debug.Log($"_playersReadyToRestart.Count {_playersReadyToRestart.Count} , _numberOfPlayers {_numberOfPlayers}, _sessionManager.PlayerCount {_sessionManager.PlayerCount}");
            if (_playersReadyToRestart.Count < _numberOfPlayers) return;
            if (_sessionManager.PlayerCount < 2) return;
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
        }

        private void ReturnListChanged(NetworkListEvent<ulong> listEvent)
        {
            Debug.Log($"_playersReadyToReturn.Count {_playersReadyToReturn.Count} , _numberOfPlayers {_numberOfPlayers}, _sessionManager.PlayerCount {_sessionManager.PlayerCount}");

            if (_playersReadyToReturn.Count < _numberOfPlayers) return;
            if (_sessionManager.PlayerCount < 2) return;

            if (_sessionManager.IsLanMode)
            {
                ClearLanCharactersAndReturnToLobby();
                return;
            }

            ResetCharacterProperty();
            ResetCharacterPropertyRpc();
        }
        
        private void ClearLanCharactersAndReturnToLobby()
        {
            if (!IsServer)
                return;

            _sessionManager.ClearAllLanPlayerCharacters();
            ClearAllLanCharactersRpc();

            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        
        [Rpc(SendTo.NotMe)]
        private void ClearAllLanCharactersRpc()
        {
            _sessionManager.ClearAllLanPlayerCharacters();
        }

        private void Update()
        {
            if (_sessionManager.IsLanMode) return;
            if (!_sessionManager.HasActiveSession) return;
            if (!IsServer) return;

            if (!_sessionManager.HaveAllPlayersClearedCharacterSelection())
                return;

            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
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
                if (_sessionManager.IsLanMode)
                {
                    ResetLanCharacterProperty();
                    return;
                }

                bool success = await _sessionManager.TryClearCurrentPlayerCharacterAsync();

                if (!success)
                    throw new Exception("Could not clear current player character.");
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
                returnToLobbyButton.interactable = true;
                statusText.text = string.Empty;

                if (IsServer) _playersReadyToReturn.Remove(NetworkManager.LocalClientId);
                if (IsClient && !IsHost) SendDeleteReadyToReturnRpc();
                
                UIUtilities.Instance.MessagePopUp("Hubo un error al reiniciar el personaje del jugador", true);
            }
        }
        
        private void ResetLanCharacterProperty()
        {
            if (!_sessionManager.TryGetCurrentPlayerId(out string playerId))
            {
                Debug.LogWarning("[PostGameUI] Could not clear LAN character because current player id is missing.");
                return;
            }

            if (IsServer)
            {
                if (!_sessionManager.TryClearLanPlayerCharacter(playerId))
                {
                    Debug.LogWarning($"[PostGameUI] Server could not clear LAN character for playerId: {playerId}");
                    return;
                }

                SyncClearLanCharacterRpc(playerId);
                return;
            }

            // Client clears its local copy if possible, but the server is authoritative.
            _sessionManager.TryClearLanPlayerCharacter(playerId);
            SendClearLanCharacterRpc(playerId);
        }
        
        [Rpc(SendTo.Server)]
        private void SendClearLanCharacterRpc(string playerId)
        {
            if (!_sessionManager.TryClearLanPlayerCharacter(playerId))
            {
                Debug.LogWarning($"[PostGameUI] Server could not clear LAN character for playerId: {playerId}");
                return;
            }

            SyncClearLanCharacterRpc(playerId);
        }

        [Rpc(SendTo.NotMe)]
        private void SyncClearLanCharacterRpc(string playerId)
        {
            _sessionManager.TryClearLanPlayerCharacter(playerId);
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
        private void SendPlayerLeavingRpc(string playerId, RpcParams rpcParams = default)
        {
            Debug.Log(playerId);

            var clientId = rpcParams.Receive.SenderClientId;

            _sessionManager.RemovePlayerClientId(playerId);
            _playersReadyToRestart.Remove(clientId);
            _playersReadyToReturn.Remove(clientId);
        }

        [Rpc(SendTo.Server)]
        private void SendDeleteReadyToReturnRpc(RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            _playersReadyToReturn.Remove(clientId);
        }
        
        private async void PerformReturnToMenu()
        {
            bool wasHost = _sessionManager.IsLocalPlayerSessionHost;

            if (wasHost)
            {
                ReturnToMenuRpc();

                // Small delay so the RPC has a chance to be sent before host shuts down.
                await Task.Delay(100);
            }

            await ReturnToMenuAsync();

            UIUtilities.Instance.LoadScene("MainMenu");
        }

        private async Task ReturnToMenuAsync()
        {
            if (_sessionManager.TryGetCurrentPlayerId(out string playerId))
            {
                SendPlayerLeavingRpc(playerId);
            }

            await SessionManager.Instance.LeaveSessionAsync();
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
    }
}