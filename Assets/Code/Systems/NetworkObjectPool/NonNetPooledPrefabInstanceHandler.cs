using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    class NonNetPooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        GameObject prefab;
        NonNetworkObjectPool pool;

        public NonNetPooledPrefabInstanceHandler(GameObject prefab, NonNetworkObjectPool pool)
        {
            this.prefab = prefab;
            this.pool = pool;
        }

        NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            return pool.GetNetworkObject(prefab, position, rotation, out var id);
        }

        void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
        {
            pool.ReturnNetworkObject(networkObject, prefab);
        }
    }
}