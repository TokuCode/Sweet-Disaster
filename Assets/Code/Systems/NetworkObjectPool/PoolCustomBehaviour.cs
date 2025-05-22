using System;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    public abstract class PoolCustomBehaviour : NetworkBehaviour
    {
        [HideInInspector] public GameObject Prefab;
        [HideInInspector] public NetworkObject networkObject;

        protected virtual void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
        }

        public abstract void Reset();
        public abstract void SetArgs(CustomBehaviourArgs args);
    }
}