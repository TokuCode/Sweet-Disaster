using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace Code.Networking.ClientPrediction
{
    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public ulong networkObjectId;
        
        public Vector3 position;
        public Vector2 velocity;
        public float localYScale;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref localYScale);
        }
    }

    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public DateTime timestamp;
        public ulong networkObjectId;
        
        public float move;
        public bool jump;
        public bool crouch;
        public bool reload;
        //public bool shieldAction;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref move);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref crouch);
            //serializer.SerializeValue(ref shieldAction);
            serializer.SerializeValue(ref reload);
        }
    }

    public struct BulletStatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Vector3 direction;
        public FixedString32Bytes ownerTag;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref direction);
            serializer.SerializeValue(ref ownerTag);
        }
    }
}