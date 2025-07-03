using System.Collections.Generic;
using Unity.Netcode;

namespace Code.Gameplay
{
    public static class WinnersData
    {
        public struct PlayerStatusData
        {
            public ulong ClientId;
            public int Lives;
            public float AccumulatedDmg;
        }
        
        public static Stack<PlayerStatusData> playerStatusDataStack = new();
    }
}