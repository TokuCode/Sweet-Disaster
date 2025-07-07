using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;

namespace Code.UserInterface.LobbyUI
{
    public class SelectedCharacters : Refreshable
    {
        [SerializeField] private List<CharacterButtonUI> characterButtons;
        
        public override void Refresh()
        {
            var players = SessionManager.Instance.ActiveSession.Players.ToList();
            
            // Clear all markers
            foreach (var button in characterButtons)
            {
                button.outlineColorImage.color = button.DefaultColor;
                button.SelectButton.interactable = true;
            }

            foreach (var player in players)
            {
                var characterName = SessionManager.Instance.playerInfo.GetPropertyValue(player, SessionManager.Instance.PlayerCharacterKey);
                
                var btn = characterButtons.FirstOrDefault(b => b.characterName == characterName);
                if (btn == null) continue;
                
                btn.SelectButton.interactable = false;
                btn.outlineColorImage.color = SessionManager.Instance.playerInfo.GetColor(player);
            }
        }
    }
}