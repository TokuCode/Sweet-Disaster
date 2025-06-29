using System.Collections.Generic;
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
        
        private const float serverTickRate = 60f;
        [SerializeField] private SerializableGuid bombPrefabId;

        [Header("Runtime")]
        [SerializeField] private List<ObjectBomb> _bombsToSync = new();
        private NetworkTimer _networkTimer;
        
        private void Awake()
        {
            SetSingleton();
            _networkTimer = new(serverTickRate);
        }

        private void SetSingleton()
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

        private void Update()
        {
            if (!IsServer) return;
            
            //_networkTimer.Update(Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            if (!IsServer) return;

            while (_networkTimer.ShouldTick())
            {
                HandleServerTick();
            }
        }

        private void HandleServerTick()
        {
            if(!IsServer) return;

            foreach (var bomb in _bombsToSync)
            {
                SyncBombById(bomb);
            }
        }

        public void AddBomb(ObjectBomb bomb)
        {
            _bombsToSync.Add(bomb);
        }

        public void RemoveBomb(ObjectBomb bomb)
        {
            _bombsToSync.Remove(bomb); 
        }

        private void SyncBombById(ObjectBomb bomb)
        {
            var bombState = bomb.GetState();
            RequestHardSyncRpc(bombState);
        }

        public void RequestHardSync(BombStatePayload bombState)
        {
            RequestHardSyncRpc(bombState);
        }

        [Rpc(SendTo.NotMe)]
        private void RequestHardSyncRpc(BombStatePayload bombState)
        {
            var go = NonNetworkObjectPool.Singleton.GetNetworkObjectReference(bombPrefabId, bombState.objectId);
            if(go == null || go.gameObject == null || !go.gameObject.activeSelf) return;
            var bomb = go.GetComponent<ObjectBomb>();
            bomb.HardSync(bombState.position, bombState.velocity, MilisecondsUtils.CalculateLatency(bombState.timestamp));
        }

        [Rpc(SendTo.NotMe)]
        private void RequestBombRemoveRpc(int bombId)
        {
            var go = NonNetworkObjectPool.Singleton.GetNetworkObjectReference(bombPrefabId, bombId);
            if(go == null || go.gameObject == null || !go.gameObject.activeSelf) return;
            var bomb = go.GetComponent<ObjectBomb>();
            RemoveBomb(bomb);
            bomb.ResetNonNotify();
        }

        public void RequestBombRemoval(int bombId)
        {
            RequestBombRemoveRpc(bombId);
        }
    }
}