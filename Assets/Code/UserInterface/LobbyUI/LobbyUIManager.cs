using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;
using SessionManager = Code.Networking.Session.SessionManager;
using System.Threading.Tasks;
using Code.Helpers.UI;
using Unity.Netcode;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("Player list visuals")]
		[SerializeField] private List<PlayerSlotUI> playerSlots;
        
        [Header("Character selection visuals")]
        [SerializeField] private List<CharacterButtonUI> characterButtons;

        [Header("Lobby general")] 
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI codeText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backButton;

        [Header("Maps")] 
        [SerializeField] private List<string> mapNames;
        [SerializeField] private Button nextMapButton;
        [SerializeField] private Button prevMapButton;
        [SerializeField] private TextMeshProUGUI mapNameText;
        [SerializeField] private Image mapImage;
        private int _currentMapIndex;

        private SessionManager _sessionManager;

        private async void Awake()
        {
            if (SessionManager.Instance == null) return;
            _sessionManager = SessionManager.Instance;
            
            // Buttons
            startGameButton.onClick.AddListener(StartGame);
            backButton.onClick.AddListener(_sessionManager.LeaveSession);
            backButton.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
            nextMapButton.onClick.AddListener(() => ChangeMap(1));
            prevMapButton.onClick.AddListener(() => ChangeMap(-1));

            nextMapButton.interactable = _sessionManager.ActiveSession.IsHost;
            prevMapButton.interactable = _sessionManager.ActiveSession.IsHost;
        }

        private void Start()
        {
            // Set code
            codeText.text = _sessionManager.ActiveSession.Code;
            
            // Build the lobby
            RefreshLobby();
            
            // Listen to changes to refresh the player list, character selection UI, and start game button
            _sessionManager.ActiveSession.Changed += RefreshLobby;
        }

        private void OnDisable()
        {
            _sessionManager.ActiveSession.Changed -= RefreshLobby;
            startGameButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }

        public void StartGame()
        {
            if (!_sessionManager.ActiveSession.IsHost) return;
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        
        private void RefreshLobby()
        {
            RefreshPlayerList(_sessionManager.ActiveSession.Players.ToList());
            RefreshCharacterSelectionUI(_sessionManager.ActiveSession.Players.ToList());
            RefreshStartGameButton();
            RefreshStatusText();
            RefreshMap();
        }

        private void RefreshMap()
        {
            string mapName = _sessionManager.ActiveSession.Properties.TryGetValue(_sessionManager.MapPropertyKey, out var mapNameProp)
                ? mapNameProp.Value : String.Empty;

            mapNameText.text = $"Mapa: {mapName}";
        }

        private void ChangeMap(float buttonDir)
        {
            if (!_sessionManager.ActiveSession.IsHost) return;
            
            if (buttonDir == 0) return;

            buttonDir = Mathf.Sign(buttonDir);

            _currentMapIndex = (_currentMapIndex + (int)buttonDir + mapNames.Count) % mapNames.Count;
            
            _sessionManager.ActiveSession.AsHost().SetProperty(_sessionManager.MapPropertyKey, 
                new SessionProperty(mapNames[_currentMapIndex], VisibilityPropertyOptions.Member));

            _sessionManager.ActiveSession.AsHost().SavePropertiesAsync();
        }

        private void RefreshPlayerList(List<IReadOnlyPlayer> players)
        {
            // Clear old entries
            foreach (var slot in playerSlots)
                slot.SetDefault();

            for (int i = 0; i < players.Count; i++)
            {
                string playerName = players[i].Properties.TryGetValue(_sessionManager.PlayerNameKey, out var nameProp)
                    ? nameProp.Value : String.Empty;
                
                Color playerColor = _sessionManager.playerInfo.GetColor(players[i]);
                
                playerSlots[i].Setup(playerName, playerColor);
            }
        }
        
        private void RefreshCharacterSelectionUI(List<IReadOnlyPlayer> players)
        {
            // Clear all markers
            foreach (var button in characterButtons)
            {
                button.outlineColorImage.color = button.DefaultColor;
                button.SelectButton.interactable = true;
            }

            foreach (var player in players)
            {
                if (!player.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var charProp))
                    continue;
                
                var characterName = charProp.Value;
                
                var btn = characterButtons.FirstOrDefault(b => b.characterName == characterName);
                if (btn == null) continue;
                
                btn.SelectButton.interactable = false;
                btn.outlineColorImage.color = _sessionManager.playerInfo.GetColor(player);
            }
        }
        
        private void RefreshStartGameButton()
        {
            if ((_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount > 1) ||
                (_sessionManager.ActiveSession.IsHost && _sessionManager.ActiveSession.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                startGameButton.interactable = AllPlayersHaveSelectedCharacters();
        }
        
        private void RefreshStatusText()
        {
            string charName = _sessionManager.ActiveSession.CurrentPlayer.Properties.
                TryGetValue(_sessionManager.PlayerCharacterKey, out var charProp)
                ? charProp.Value : String.Empty;
            
            if (charName != String.Empty && charName != "None")
            {
                if (_sessionManager.ActiveSession.PlayerCount > 1 || (_sessionManager.ActiveSession.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                {
                    if (AllPlayersHaveSelectedCharacters())
                        statusText.text = _sessionManager.ActiveSession.IsHost ? 
                            "La partida esta lista para ser iniciada" : "Esperando al anfitrión";
                    else statusText.text = "Esperando a los jugadores";
                }
                else statusText.text = "Esperando a los jugadores";
            }
            else statusText.text = "Elige tu personaje";
        }
            
        private bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in _sessionManager.ActiveSession.Players)
            {
                if (!player.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var charProp) || 
                    string.IsNullOrEmpty(charProp.Value) || charProp.Value == "None")
                    return false;
            }
            return true;
        }
        
        private async Task<bool> TrySelectCharacter(string characterName)
        {
            bool isTaken = _sessionManager.ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var prop) &&
                prop.Value == characterName);

            if (isTaken) return false;

            _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerCharacterKey,
                new PlayerProperty(characterName, VisibilityPropertyOptions.Member));
            await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
            return true;
        }
        
        public async void OnCharacterSelected(CharacterButtonUI character)
        {
            bool success = await TrySelectCharacter(character.characterName);
            if (!success) Debug.Log("Character already taken");
            else Debug.Log($"My Character: {character.characterName}");
        }
        
        public void CopyToClipboard() => GUIUtility.systemCopyBuffer = codeText.text;
    }
}