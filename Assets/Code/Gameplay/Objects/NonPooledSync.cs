using System;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class NonPooledSync : NetworkBehaviour
    {
        public static NonPooledSync Singleton { get; private set; }
        [SerializeField] private SerializableGuid bombPrefabId;
        
        private void Awake()
        {
            if(Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Singleton = this;
            }
        }
       
        [ClientRpc]
        private void RequestHardSynchronizationClientRpc(BombStatePayload bombState)
        {
            var go = NonNetworkObjectPool.Singleton.GetNetworkObjectReference(bombPrefabId, bombState.objectId);
            var bomb = go.GetComponent<ObjectBomb>();
            bomb.HardSync(bombState.position, bombState.velocity, MilisecondsUtils.CalculateLatency(bombState.timestamp));
        }

        public void RequestHardSync(BombStatePayload bombState)
        {
            RequestHardSynchronizationClientRpc(bombState);
        }
    }
}