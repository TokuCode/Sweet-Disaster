using System;
using UnityEngine;
using Unity.Netcode;

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
        //public Vector3 handlePosition;
        //public Vector3 handleDirection;
        public bool jump;
        public bool crouch;
        //public InputActionButton shieldAction;
        //public bool shootRequested;
        //public InputActionButton bombRequested;
        //public bool reloadRequested;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref move);
            //serializer.SerializeValue(ref handlePosition);
            //serializer.SerializeValue(ref handleDirection);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref crouch);
            //serializer.SerializeValue(ref shieldAction);
            //serializer.SerializeValue(ref shootRequested);
            //serializer.SerializeValue(ref bombRequested);
            //serializer.SerializeValue(ref reloadRequested);
        }
    }
}