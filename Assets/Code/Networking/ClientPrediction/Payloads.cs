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
        //public bool isCrouching;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            //serializer.SerializeValue(ref isCrouching);
        }
    }

    public enum InputActionButton
    {
        Pressed,
        Released,
        Unchanged
    }

    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public DateTime timestamp;
        public ulong networkObjectId;
        
        public float moveInput;
        //public Vector3 handlePosition;
        //public Vector3 handleDirection;
        //public InputActionButton jumpAction;
        //public InputActionButton crouchAction;
        //public InputActionButton shieldAction;
        //public bool shootRequested;
        //public bool bombRequested;
        //public bool reloadRequested;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref moveInput);
            //serializer.SerializeValue(ref handlePosition);
            //serializer.SerializeValue(ref handleDirection);
            //serializer.SerializeValue(ref jumpAction);
            //serializer.SerializeValue(ref crouchAction);
            //serializer.SerializeValue(ref shieldAction);
            //serializer.SerializeValue(ref shootRequested);
            //serializer.SerializeValue(ref bombRequested);
            //serializer.SerializeValue(ref reloadRequested);
        }
    }
}