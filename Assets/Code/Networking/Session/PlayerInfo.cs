using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Services.Multiplayer;

namespace Code.Networking.Session
{
    public class PlayerInfo : MonoBehaviour
    {
        private readonly Dictionary<string, Color> _playerColors = new()
        {
            { "blue", Color.blue },
            { "red", Color.red },
            { "yellow", Color.yellow },
            { "green", Color.green }
        };
        
        private readonly string[] _randomNames =
        {
            "PanConWifi",
            "TioPapita",
            "LagMan3000",
            "CucharaNinja",
            "Tiramisu",
            "SinManaNiGloria",
            "DonCeviche",
            "PatitoDeFuego",
            "ElTamalAsesino",
            "Albondigón3000",
            "ChispaDeTuna",
            "CalabazaEspía",
            "SeñorTaco"
        };
        
        public string GetAvailableColorName()
        {
            var takenColors = new HashSet<string>();

            if (SessionManager.Instance.ActiveSession != null)
            {
                foreach (var player in SessionManager.Instance.ActiveSession.Players)
                {
                    if (player.Properties.TryGetValue(SessionManager.Instance.PlayerColorKey, out var prop))
                        takenColors.Add(prop.Value);
                }
            }
            foreach (var colorName in _playerColors.Keys)
            {
                if (!takenColors.Contains(colorName))
                    return colorName;
            }
            return String.Empty;
        }

        public Color GetColor(IReadOnlyPlayer player)
        {
            string colorName = player.Properties.TryGetValue(SessionManager.Instance.PlayerColorKey, out var colorProp)
                ? colorProp.Value : String.Empty;
            
            return _playerColors.TryGetValue(colorName, out var color) ? color : Color.gray;
        }
        
        public string GetRandomName()
        {
            var takenNames = new HashSet<string>();

            if (SessionManager.Instance.ActiveSession != null)
            {
                foreach (var player in SessionManager.Instance.ActiveSession.Players)
                {
                    if (player.Properties.TryGetValue(SessionManager.Instance.PlayerNameKey, out var prop))
                        takenNames.Add(prop.Value);
                }
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