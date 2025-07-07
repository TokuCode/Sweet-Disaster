using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;

namespace Code.UserInterface.LobbyUI
{
    public class PlayerList : Refreshable
    {
        [SerializeField] private List<PlayerSlotUI> playerSlots;
        
        public override void Refresh()
        {
            var players = SessionManager.Instance.ActiveSession.Players.ToList();
            
            foreach (var slot in playerSlots)
                slot.SetDefault();

            for (int i = 0; i < players.Count; i++)
            {
                string playerName = SessionManager.Instance.playerInfo.GetPropertyValue(players[i], SessionManager.Instance.PlayerNameKey);
                Color playerColor = SessionManager.Instance.playerInfo.GetColor(players[i]);
                
                playerSlots[i].SetSlot(playerName, playerColor);
            }
        }
    }
}