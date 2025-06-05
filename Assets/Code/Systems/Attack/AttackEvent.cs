using UnityEngine;

namespace Code.Systems.Attack
{
    public class AttackEvent
    {
        public Vector3 SourcePosition;
        public float DamagePercentage;
        public int KnockbackLevel;
        public int KnockbackUpLevel;
        public bool Success;
    }
}
