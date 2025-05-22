using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Systems.Session
{
    public class LobbyUIManager : MonoBehaviour
    {
        [SerializeField] Transform playerListParent;
        [SerializeField] GameObject playerSlotPrefab;
        [SerializeField] GameObject colorMarkerPrefab;
        [SerializeField] List<CharacterButtonUI> characterButtons;

        private void Start() => SessionManager.Instance.SessionUpdated += RefreshLobby;
        private void OnDisable()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.SessionUpdated -= RefreshLobby;
        }

        private void RefreshLobby()
        {
            RefreshPlayerList(SessionManager.Instance.ActiveSession.Players.ToList(), SessionManager.Instance.colors);
            RefreshCharacterSelectionUI(SessionManager.Instance.ActiveSession.Players.ToList(), SessionManager.Instance.colors);
        }

        private void RefreshPlayerList(List<IReadOnlyPlayer> players, Dictionary<string, Color> namedColors)
        {
            // Clear old entries
            foreach (Transform child in playerListParent)
                Destroy(child.gameObject);

            foreach (var player in players)
            {
                //string playerName = player.Properties.TryGetValue("playerName", out var nameProp) ? nameProp.Value : "Unnamed";
                string colorName = player.Properties.TryGetValue(SessionManager.Instance.playerColorPropertyKey, out var colorProp) ? colorProp.Value : "";

                namedColors.TryGetValue(colorName, out Color playerColor);

                var slot = Instantiate(playerSlotPrefab, playerListParent);
                slot.GetComponent<PlayerSlotUI>().Setup("player", playerColor);
            }
        }
        public async void OnCharacterSelected(string characterName)
        {
            bool success = await SessionManager.Instance.TrySelectCharacter(characterName);
            if (!success)
                Debug.Log("Character already taken");
            else
                Debug.Log($"My Character: {characterName}");
        }
        private void RefreshCharacterSelectionUI(List<IReadOnlyPlayer> players, Dictionary<string, Color> colorMap)
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
                if (!player.Properties.TryGetValue(SessionManager.Instance.playerCharacterPropertyKey, out var charProp) ||
                    !player.Properties.TryGetValue(SessionManager.Instance.playerColorPropertyKey, out var colorProp))
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
    }
}