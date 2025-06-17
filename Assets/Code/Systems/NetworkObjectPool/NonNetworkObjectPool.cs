using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Code.Systems.NetworkObjectPool
{
    public class NonNetworkObjectPool : NetworkBehaviour
    {
       public static NonNetworkObjectPool Singleton { get; private set; }
        
        [SerializeField] private List<PoolConfigObject> _poolConfigObjects;
        Dictionary<SerializableGuid, GameObject> _prefabs = new ();
        Dictionary<GameObject, ObjectPoolWithId> _pooledObjects = new ();

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

        public override void OnNetworkSpawn()
        {
            foreach (var configObject in _poolConfigObjects)
            {
                RegisterPrefabInternal(configObject.prefab, configObject.prewarmCount, configObject.prefabId);
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (var prefab in _prefabs.Values)
            {
                _pooledObjects[prefab].Clear();
            }
            _pooledObjects.Clear();
            _prefabs.Clear();
        }

        private void OnValidate()
        {
            for (int i = 0; i < _poolConfigObjects.Count; i++)
            {
                var prefab = _poolConfigObjects[i].prefab;
                if (prefab != null)
                {
                    Assert.IsNotNull(prefab.GetComponent<NetworkObject>(),
                        $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
                }
            }
        }

        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation, out int id)
        {
            var go = _pooledObjects[prefab].Get(out id);
            
            var goTransform = go.transform;
            goTransform.position = position;
            goTransform.rotation = rotation;
            
            return go;
        }

        public NetworkObject GetNetworkObjectById(GameObject prefab, Vector3 position, Quaternion rotation, int id)
        {
            var go = _pooledObjects[prefab].GetById(id);
            
            var goTransform = go.gameObject.transform;
            goTransform.position = position;
            goTransform.rotation = rotation;
            
            return go;
        }

        public GameObject GetPrefab(SerializableGuid Id)
        {
            return _prefabs.GetValueOrDefault(Id);
        }

        public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
        {
            _pooledObjects[prefab].Release(networkObject);
        }

        public void ReturnNetworkObject(NetworkObject networkObject, SerializableGuid prefabId)
        {
            var prefab = GetPrefab(prefabId);
            if (prefab != null) _pooledObjects[prefab].Release(networkObject);
        }

        public void ReturnNetworkObjectId(int noId, SerializableGuid prefabId)
        {
            var prefab = GetPrefab(prefabId);
            if(prefab != null) _pooledObjects[prefab].ReleaseById(noId);
        }

        public NetworkObject GetNetworkObjectReference(SerializableGuid prefabId, int id)
        {
            var prefab = GetPrefab(prefabId);
            return _pooledObjects[prefab].GetReferenceById(id);
        }

        private void RegisterPrefabInternal(GameObject prefab, int prewarmCount, SerializableGuid id)
        {
            NetworkObject CreateFunc()
            {
                return Instantiate(prefab).GetComponent<NetworkObject>();
            }

            void ActionOnGet(NetworkObject networkObject)
            {
                networkObject.gameObject.SetActive(true);
            }
            
            void ActionOnRelease(NetworkObject networkObject)
            {
                networkObject.gameObject.SetActive(false);
            }
            
            void ActionOnDestroy(NetworkObject networkObject)
            {
                if(IsServer) Destroy(networkObject.gameObject);
            }

            bool ActionCheck(NetworkObject networkObject)
            {
                return networkObject.gameObject.activeSelf;
            }

            _prefabs.Add(id, prefab);
            
            _pooledObjects[prefab] = new ObjectPoolWithId(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, ActionCheck, prewarmCount);
        } 
    }
}