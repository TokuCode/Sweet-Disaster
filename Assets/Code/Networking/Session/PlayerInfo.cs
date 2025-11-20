using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Services.Multiplayer;

namespace Code.Networking.Session
{
    public class PlayerInfo : MonoBehaviour
    {
        public string playerDisplayName;
        
        private readonly Dictionary<string, Color> _playerColors = new()
        {
            { "blue", Color.orange },
            { "red", Color.red },
            { "yellow", Color.yellow },
            { "green", Color.green }
        };
        
        private readonly string[] _randomNames =
        {
            "ArrozConLeche",
            "Mazamorra",
            "Suspiro",
            "TortaHelada"
        };
        
        public Color GetColor(IReadOnlyPlayer player)
        {
            string colorName = GetPropertyValue(player, SessionManager.Instance.PlayerColorKey);

            Color color = _playerColors.TryGetValue(colorName, out var colorProp) ? colorProp : Color.gray;
            
            return color;
        }

        public string GetPropertyValue(IReadOnlyPlayer player, string propertyKey)
        {
            string propertyValue = player.Properties.TryGetValue(propertyKey, out var prop) ?
                prop.Value : String.Empty;
            
            return propertyValue;
        }
        
        public string GetAvailableColorName()
        {
            var takenColors = new HashSet<string>();

            if (SessionManager.Instance.ActiveSession != null)
            {
                foreach (var player in SessionManager.Instance.ActiveSession.Players)
                    takenColors.Add(GetPropertyValue(player, SessionManager.Instance.PlayerColorKey));
            }
            foreach (var colorName in _playerColors.Keys)
            {
                if (!takenColors.Contains(colorName))
                    return colorName;
            }
            return String.Empty;
        }
        
        public string GetRandomName()
        {
            var takenNames = new HashSet<string>();

            if (SessionManager.Instance.ActiveSession != null)
            {
                foreach (var player in SessionManager.Instance.ActiveSession.Players)
                    takenNames.Add(GetPropertyValue(player, SessionManager.Instance.PlayerNameKey));
            }
            
            if (takenNames.Count >= _randomNames.Length)
                return "Unnamed";

            string candidate;
            do
            {
                candidate = _randomNames[UnityEngine.Random.Range(0, _randomNames.Length)];
            } 
            while (takenNames.Contains(candidate));

            return candidate;
        }
    }
}