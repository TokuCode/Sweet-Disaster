using Code.Networking.Session;
using UnityEngine;
using System;
using TMPro;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyMaps : Refreshable
    {
        [SerializeField] private TextMeshProUGUI mapNameText;
        
        public override void Refresh()
        {
            string mapName = SessionManager.Instance.ActiveSession.Properties.
                TryGetValue(SessionManager.Instance.MapPropertyKey, out var mapNameProp)
                ? mapNameProp.Value : String.Empty;

            mapNameText.text = $"Mapa: {mapName}";
        }
    }
}