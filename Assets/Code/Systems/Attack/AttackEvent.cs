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
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SourcePosition);
            serializer.SerializeValue(ref DamagePercentage);
            serializer.SerializeValue(ref KnockbackForce);
            serializer.SerializeValue(ref KnockbackUpForce);
            serializer.SerializeValue(ref Success);
        }
    }
}
