using UnityEngine;

namespace Code.Systems.Attack
{
    public class AttackEvent
    {
        public Vector3 SourcePosition;
        public float DamagePercentage;
        public float KnockbackForce;
        public float KnockbackUpForce;
        public bool Success;
    }
}
