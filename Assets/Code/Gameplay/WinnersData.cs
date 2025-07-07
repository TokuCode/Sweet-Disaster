using System.Collections.Generic;

namespace Code.Gameplay
{
    public static class WinnersData
    {
        public struct PlayerStatusData
        {
            public ulong ClientId;
            public bool IsWinner;
            public int Lives;
            public float AccumulatedDmg;
        }
        
        public static Stack<PlayerStatusData> playerStatusDataStack = new();
    }
}