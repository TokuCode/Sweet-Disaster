using Code.Systems.NetworkObjectPool;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public struct BulletArgs : CustomBehaviourArgs
    {
        public string OwnerTag;
        public float LifeTime;
        public float Damage;
        public int KnockbackLevel;
        public float Speed;
        public Vector2 Direction;
    }
}