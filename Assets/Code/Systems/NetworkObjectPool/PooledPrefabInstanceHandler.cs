using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        GameObject prefab;
        NetworkObjectPool pool;

        public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
        {
            this.prefab = prefab;
            this.pool = pool;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            return pool.GetNetworkObject(prefab, position, rotation);
        }

        void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
        {
            pool.ReturnNetworkObject(networkObject, prefab);
        }
    }
}