using System;

namespace Code.Helpers.Utils
{
    public static class MilisecondsUtils
    {
        public static float CalculateLatency(DateTime timestamp) => (DateTime.Now - timestamp).Milliseconds / 1000f; 
    }
}