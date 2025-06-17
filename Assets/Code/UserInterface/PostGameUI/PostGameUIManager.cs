using System;
using System.Linq;
using Code.UserInterface.LobbyUI;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using Code.Networking.Session;
using TMPro;
using Color = UnityEngine.Color;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Code.Helpers.UI;

namespace Code.UserInterface.PostGameUI
{
    public class PostGameUIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI winnerTitle;
        [SerializeField] private List<GameObject> loserList;
        [SerializeField] private GameObject winner;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button playAgainButton;
        [SerializeField] private UnityEngine.UI.Button exitButton;
        
        [SerializeField] private TextMeshProUGUI statusText;
        
        private SessionManager _sessionManager;
        
        [Serializable]
        public struct Characters
        {
            public string name;
            public Sprite image;
        }
        [SerializeField] private List<Characters> characters;

        private void Awake()
        {
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
            exitButton.onClick.AddListener(ReturnToLobby);
            
            _sessionManager = SessionManager.Instance;
        }

        private void Start()
        {
            PopulatePlayers();
        }
        
        private void OnDisable()
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
            exitButton.onClick.RemoveListener(ReturnToLobby);
        }

        private void PopulatePlayers()
        {
            var session = _sessionManager.ActiveSession;
            var winnerPlayerId = session.Properties.TryGetValue(_sessionManager.WinnerPropertyKey, out var winnerPlayer)
                ? winnerPlayer.Value : String.Empty;
        
            if (string.IsNullOrEmpty(winnerPlayerId) || session == null)
            {
                Debug.LogWarning("Winner ID not set or session is null.");
                return;
            }
        
            var winnerMember = session.Players.FirstOrDefault(p => p.Id == winnerPlayerId);
            if (winnerMember == null)
            {
                Debug.LogWarning("Winner player not found in session.");
                winnerTitle.text = "Ganador desconocido";
                return;
            }
        
            // Setup winner UI
            var winnerImg = winner.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
            var winnerSlot = winner.transform.GetChild(1).GetComponent<PlayerSlotUI>();
        
            string winnerCharacter = winnerMember.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var charProp)
                ? charProp.Value : null;
        
            if (!string.IsNullOrEmpty(winnerCharacter))
            {
                var character = characters.FirstOrDefault(c => c.name == winnerCharacter);
                if (character.image != null)
                    winnerImg.sprite = character.image;
            }
        
            string winnerName = winnerMember.Properties.TryGetValue(_sessionManager.PlayerNameKey, out var nameProp)
                ? nameProp.Value : $"Jugador {winnerPlayerId}";
        
            winnerSlot.nameText.text = winnerName;
            winnerSlot.outlineColor.color = _sessionManager.playerInfo.GetColor(winnerMember);
            winnerTitle.text = $"Ganador: {winnerName}";
        
            // Setup losers
            var losers = session.Players.Where(p => p.Id != winnerPlayerId).ToList();
            for (int i = 0; i < loserList.Count; i++)
            {
                if (i < losers.Count)
                {
                    var slot = loserList[i].transform.GetChild(1).GetComponent<PlayerSlotUI>();
                    var image = loserList[i].transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
        
                    var loser = losers[i];
        
                    string loserCharacter = loser.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var lcharProp)
                        ? lcharProp.Value : null;
        
                    if (!string.IsNullOrEmpty(loserCharacter))
                    {
                        var character = characters.FirstOrDefault(c => c.name == loserCharacter);
                        if (character.image != null)
                            image.sprite = character.image;
                    }
        
                    string loserName = loser.Properties.TryGetValue(_sessionManager.PlayerNameKey, out var lnameProp)
                        ? lnameProp.Value : $"Jugador {i + 1}";
        
                    slot.nameText.text = loserName;
                    slot.outlineColor.color = _sessionManager.playerInfo.GetColor(loser);
                }
            }
        }
        
        private async void OnPlayAgainPressed()
        {
            _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerReadyToRestart,
                new PlayerProperty("true", VisibilityPropertyOptions.Member));
            await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
            
            playAgainButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
            CheckAllReadyToRestart();
        }
        
        private async void CheckAllReadyToRestart()
        {
            Debug.Log("Checking if it should restart...");
            var session = _sessionManager.ActiveSession;
            var readyKey = _sessionManager.PlayerReadyToRestart;

            while (true)
            {
                bool allReady = session.Players.All(player =>
                    player.Properties.TryGetValue(readyKey, out var readyProp) &&
                    readyProp.Value == "true"
                );

                if (allReady && session.PlayerCount > 1)
                {
                    Debug.Log("All players are ready. Restarting game...");
                    
                    if (_sessionManager.ActiveSession.IsHost)
                    {
                        session.AsHost().SetProperty(
                            _sessionManager.WinnerPropertyKey,
                            new SessionProperty("None", VisibilityPropertyOptions.Member)
                        );
                        await session.AsHost().SavePropertiesAsync();
                        
                        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                    }
                    break;
                }

                Debug.Log("One of the players is not ready to restart.");
                await Task.Delay(1000); // check every second
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