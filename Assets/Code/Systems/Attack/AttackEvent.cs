using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.Attack
{
    public class AttackEvent : INetworkSerializable
    {
        public Vector3 SourcePosition;
        public float DamagePercentage;
        public float KnockbackForce;
        public float KnockbackUpForce;
        public bool Success;
        public int SenderId;
        public int ReceiverId;
        public int Weapon;
        public bool Unblockeable;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SourcePosition);
            serializer.SerializeValue(ref DamagePercentage);
            serializer.SerializeValue(ref KnockbackForce);
            serializer.SerializeValue(ref KnockbackUpForce);
            serializer.SerializeValue(ref Success);
            serializer.SerializeValue(ref SenderId);
            serializer.SerializeValue(ref ReceiverId);
            serializer.SerializeValue(ref Weapon);
            serializer.SerializeValue(ref Unblockeable);
        }
    }
}
