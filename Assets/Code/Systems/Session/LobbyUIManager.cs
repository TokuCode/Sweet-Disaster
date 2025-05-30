using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Systems.Session
{
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button createSessionButton;
        [SerializeField] private Button joinSessionButton;
        
        [Header("Player list visuals")]
        [SerializeField] private Transform playerListParent;
        [SerializeField] private GameObject playerSlotPrefab;
        
        [Header("Character selection visuals")]
        [SerializeField] private GameObject colorMarkerPrefab;
        [SerializeField] private List<CharacterButtonUI> characterButtons;
        
        [Header("Lobby general")]
        [SerializeField] private TextMeshProUGUI codeText;
        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private Button startGameButton;

        private void Awake()
        {
            if (SessionManager.Instance == null) return;
            
            // Buttons
            createSessionButton.onClick.AddListener(SessionManager.Instance.StartSessionAsHost);
            joinSessionButton.onClick.AddListener(() => SessionManager.Instance.JoinSessionByCode(codeInputField.text));
            startGameButton.onClick.AddListener(SessionManager.Instance.OnStartGamePressed);
            
            // Session events
            SessionManager.Instance.ActiveSessionAvailable += OnActiveSessionAvailable;
        }
        
        private void OnDisable()
        {
            if (SessionManager.Instance == null) return;
            SessionManager.Instance.ActiveSession.Changed -= RefreshLobby;
            SessionManager.Instance.ActiveSessionAvailable -= OnActiveSessionAvailable;
        }

        private void OnActiveSessionAvailable()
        {
            // Hide panel and pop
            UITransitionManager.Instance.PopDown(UITransitionManager.Instance.MessageObject);
            UITransitionManager.Instance.PopDown(GameObject.Find("PreLobby"));
            
            // Set code
            codeText.text = SessionManager.Instance.ActiveSession.Code;
            
            // Build the lobby
            RefreshLobby();
            
            // Listen to changes to refresh the player list, character selection UI, and start game button
            SessionManager.Instance.ActiveSession.Changed += RefreshLobby;
        }
        
        private void RefreshLobby()
        {
            RefreshPlayerList(SessionManager.Instance.ActiveSession.Players.ToList(), SessionManager.Instance.PlayerColors);
            RefreshCharacterSelectionUI(SessionManager.Instance.ActiveSession.Players.ToList(), SessionManager.Instance.PlayerColors);
            RefreshStartGameButton();
        }

        private void RefreshPlayerList(List<IReadOnlyPlayer> players, IReadOnlyDictionary<string, Color> colorMap)
        {
            // Clear old entries
            foreach (Transform child in playerListParent)
                Destroy(child.gameObject);

            foreach (var player in players)
            {
                string playerName = player.Properties.TryGetValue(
                    SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerName], 
                    out var nameProp) 
                    ? nameProp.Value : String.Empty;
                
                string colorName = player.Properties.TryGetValue(
                    SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerColor],
                    out var colorProp)
                    ? colorProp.Value : String.Empty;

                colorMap.TryGetValue(colorName, out Color playerColor);

                var slot = Instantiate(playerSlotPrefab, playerListParent);
                slot.GetComponent<PlayerSlotUI>().Setup(playerName, playerColor);
            }
        }
        
        private void RefreshCharacterSelectionUI(List<IReadOnlyPlayer> players, IReadOnlyDictionary<string, Color> colorMap)
        {
            // Clear all markers
            foreach (var button in characterButtons)
            {
                foreach (Transform child in button.markerContainer)
                    Destroy(child.gameObject);

                button.selectButton.interactable = true; // re-enable first
            }

            foreach (var player in players)
            {
                if (!player.Properties.TryGetValue(SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerCharacter], out var charProp) ||
                    !player.Properties.TryGetValue(SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerColor], out var colorProp))
                    continue;

                var characterName = charProp.Value;
                var colorName = colorProp.Value;

                var btn = characterButtons.FirstOrDefault(b => b.characterName == characterName);
                if (btn == null) continue;

                btn.selectButton.interactable = false;

                // Spawn marker
                var marker = Instantiate(colorMarkerPrefab, btn.markerContainer);
                marker.GetComponent<Image>().color = colorMap[colorName];
            }
        }

        private void RefreshStartGameButton()
        {
            if (SessionManager.Instance.ActiveSession.IsHost && SessionManager.Instance.ActiveSession.PlayerCount > 1)
                startGameButton.interactable = SessionManager.Instance.AllPlayersHaveSelectedCharacters();
        }
        
        public async void OnCharacterSelected(string characterName)
        {
            bool success = await SessionManager.Instance.TrySelectCharacter(characterName);
            if (!success)
                Debug.Log("Character already taken");
            else
                Debug.Log($"My Character: {characterName}");
        }
        
        public void CopyToClipboard() => GUIUtility.systemCopyBuffer = codeText.text;
    }
}