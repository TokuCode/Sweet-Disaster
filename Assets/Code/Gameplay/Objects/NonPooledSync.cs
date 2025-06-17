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

        private void HardSyncBomb(Vector3 position, Vector2 velocity, int id)
        {
            var analogousGO = NonNetworkObjectPool.Singleton.GetNetworkObjectReference(bombPrefabId, id);
            var analogous = analogousGO.GetComponent<ObjectBomb>();
            analogous.HardSync(position, velocity);
        }
       
        [ClientRpc]
        private void RequestHardSynchronizationClientRpc(Vector3 position, Vector2 velocity, int id)
        {
            HardSyncBomb(position, velocity, id);
        }

        public void RequestHardSync(Vector3 position, Vector2 velocity, int id)
        {
            RequestHardSynchronizationClientRpc(position, velocity, id);
        }
        
        private void BounceSyncBomb(Vector3 position, Vector2 velocity, int id, int bounceCount)
        {
            var analogousGO = NonNetworkObjectPool.Singleton.GetNetworkObjectReference(bombPrefabId, id);
            var analogous = analogousGO.GetComponent<ObjectBomb>();
            analogous.SynchronizeBounce(position, velocity, bounceCount);
        } 
        
        [ClientRpc]
        private void RequestBounceSynchronizationClientRpc(Vector3 position, Vector2 velocity, int id, int bounceCount)
        {
            BounceSyncBomb(position, velocity, id, bounceCount);
        }  
        
        public void RequestBounceSync(Vector3 position, Vector2 velocity, int id, int bounceCount)
        {
            RequestBounceSynchronizationClientRpc(position, velocity, id, bounceCount);
        } 
    }
}