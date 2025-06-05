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
        
        [Serializable]
        public struct Characters
        {
            public string name;
            public Sprite image;
        }
        [SerializeField] private List<Characters> characters;

        private async void Awake()
        {
            PopulatePlayers();
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
            exitButton.onClick.AddListener(ReturnToLobby);
            
            var session = SessionManager.Instance.ActiveSession;
            session.CurrentPlayer.SetProperty(SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerReadyToRestart],
                new PlayerProperty("false", VisibilityPropertyOptions.Member));
            await session.SaveCurrentPlayerDataAsync();
        }
        
        private void OnDisable()
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
            exitButton.onClick.RemoveListener(ReturnToLobby);
        }

        private void PopulatePlayers()
        {
            if (SessionManager.Instance == null || SessionManager.Instance.ActiveSession == null)
                return;

            var session = SessionManager.Instance.ActiveSession;
            var sessionKeys = SessionManager.Instance.SessionKeys;
            var playerKeys = SessionManager.Instance.PlayerKeys;

            // Prepare winner UI references
            var winnerImg = winner.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
            var winnerSlot = winner.transform.GetChild(1).GetComponent<PlayerSlotUI>();

            if (session.Properties.TryGetValue(sessionKeys[SessionPropertyKeys.Winner], out var winnerProp))
            {
                string winnerPlayerId = winnerProp.Value;
                var winnerMember = session.Players.FirstOrDefault(p => p.Id == winnerPlayerId);

                if (winnerMember != null)
                {
                    string characterName =
                        winnerMember.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerCharacter], out var charProp)
                            ? charProp.Value
                            : null;

                    if (!string.IsNullOrEmpty(characterName))
                    {
                        var character = characters.FirstOrDefault(c => c.name == characterName);
                        if (character.image != null)
                            winnerImg.sprite = character.image;
                    }
                    string name =
                        winnerMember.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerName], out var nameProp)
                            ? nameProp.Value
                            : $"Jugador {winnerPlayerId}";

                    string colorStr =
                        winnerMember.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerColor],
                            out var colorProp)
                            ? colorProp.Value
                            : "#FFFFFF";

                    Color color = Color.white;
                    ColorUtility.TryParseHtmlString(colorStr, out color);

                    winnerSlot.nameText.text = name;
                    winnerSlot.outlineColor.color = color;
                    winnerTitle.text = $"Ganador: {name}";

                    // Show losers
                    var losers = session.Players.Where(p => p.Id != winnerPlayerId).ToList();
                    for (int i = 0; i < loserList.Count; i++)
                    {
                        if (i < losers.Count)
                        {
                            var slot = loserList[i].transform.GetChild(1).GetComponent<PlayerSlotUI>();
                            var image = loserList[i].transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();

                            var loser = losers[i];
                            
                            string loserCharacter =
                                loser.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerCharacter], out var lcharProp)
                                    ? lcharProp.Value
                                    : null;

                            if (!string.IsNullOrEmpty(loserCharacter))
                            {
                                var character = characters.FirstOrDefault(c => c.name == loserCharacter);
                                if (character.image != null)
                                    image.sprite = character.image;
                            }
                            
                            string loserName = loser.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerName],
                                out var lnameProp)
                                ? lnameProp.Value
                                : $"Jugador {i + 1}";

                            string loserColorStr =
                                loser.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerColor],
                                    out var lcolorProp)
                                    ? lcolorProp.Value
                                    : "#FFFFFF";

                            Color loserColor = Color.white;
                            ColorUtility.TryParseHtmlString(loserColorStr, out loserColor);

                            slot.nameText.text = loserName;
                            slot.outlineColor.color = loserColor;
                        }
                    }
                }
                else
                {
                    winnerTitle.text = "Ganador desconocido";
                    winnerSlot.nameText.text = "???";
                    winnerSlot.outlineColor.color = Color.gray;
                }
            }
        }
        
        private async void OnPlayAgainPressed()
        {
            var session = SessionManager.Instance.ActiveSession;

            session.CurrentPlayer.SetProperties(new Dictionary<string, PlayerProperty>
            {
                {
                    SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerReadyToRestart],
                    new PlayerProperty("true", VisibilityPropertyOptions.Member)
                }/*,
                {
                    SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerCharacter],
                    new PlayerProperty("None", VisibilityPropertyOptions.Member)
                }*/
            });

            await session.SaveCurrentPlayerDataAsync();

            playAgainButton.interactable = false;
            statusText.text = "Esperando a los jugadores...";
            
            CheckAllReadyToRestart();
        }

        
        private async void CheckAllReadyToRestart()
        {
            var session = SessionManager.Instance.ActiveSession;
            var playerKeys = SessionManager.Instance.PlayerKeys;
            var readyKey = playerKeys[PlayerPropertyKeys.PlayerReadyToRestart];

            while (true)
            {
                bool allReady = session.Players.All(player =>
                    player.Properties.TryGetValue(readyKey, out var readyProp) &&
                    readyProp.Value == "true"
                );

                if (allReady && session.PlayerCount > 1)
                {
                    Debug.Log("All players are ready. Restarting game...");
                    UIUtilities.Instance.FadeIn(UIUtilities.Instance.TransitionPanel, UIUtilities.Instance.TransitionDuration);
                    
                    if (SessionManager.Instance.ActiveSession.IsHost)
                    {
                        /*SessionManager.Instance.ActiveSession.AsHost().SetProperty(
                            SessionManager.Instance.SessionKeys[SessionPropertyKeys.PlayersReady], 
                            new SessionProperty("false", VisibilityPropertyOptions.Member));
                        
                        await SessionManager.Instance.ActiveSession.AsHost().SavePropertiesAsync();*/
                        
                        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                    }
                    break;
                }

                await Task.Delay(1000); // Check every second
            }
        }

        private void ReturnToLobby()
        {
            SessionManager.Instance.LeaveSession();
            NetworkManager.Singleton.Shutdown();
            UIUtilities.Instance.LoadScene("LobbyTest");
        }
    }
}