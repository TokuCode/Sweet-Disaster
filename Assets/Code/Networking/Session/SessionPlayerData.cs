using UnityEngine;

namespace Code.Networking.Session
{
    public class SessionPlayerData
    {
        public string PlayerId;
        public ulong ClientId;
        public string PlayerName;
        public string PlayerColorName;
        public Color PlayerColor;
        public string CharacterName;
        public bool IsHost;
        public bool IsCurrentPlayer;
    }
}