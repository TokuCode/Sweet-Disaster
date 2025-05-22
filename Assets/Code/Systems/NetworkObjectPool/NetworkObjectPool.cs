using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Code.Systems.NetworkObjectPool
{
    public class NetworkObjectPool : NetworkBehaviour
    {
        public static NetworkObjectPool Singleton { get; private set; }
        
        [SerializeField] private List<PoolConfigObject> _poolConfigObjects;
        HashSet<GameObject> _prefabs = new ();
        Dictionary<GameObject, ObjectPool<NetworkObject>> _pooledObjects = new ();

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
                RegisterPrefabInternal(configObject.prefab, configObject.prewarmCount);
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (var prefab in _prefabs)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
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

        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation, CustomBehaviourArgs args = null)
        {
            var networkObject = _pooledObjects[prefab].Get();
            var noTransform = networkObject.transform;
            noTransform.position = position;
            noTransform.rotation = rotation;
            
            var customBehaviour = networkObject.gameObject.GetComponent<PoolCustomBehaviour>();
            if (customBehaviour != null)
            {
                customBehaviour.SetArgs(args);
            }
            
            return networkObject;
        }

        public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
        {
            _pooledObjects[prefab].Release(networkObject);
        }

        private void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
        {
            NetworkObject CreateFunc()
            {
                var networkObject = Instantiate(prefab).GetComponent<NetworkObject>();
                
                var customBehaviour = networkObject.gameObject.GetComponent<PoolCustomBehaviour>();
                if (customBehaviour != null)
                {
                    customBehaviour.Prefab = prefab;
                }
                
                return networkObject;
            }

            void ActionOnGet(NetworkObject networkObject)
            {
                networkObject.gameObject.SetActive(true);
            }
            
            void ActionOnRelease(NetworkObject networkObject)
            {
                var customBehaviour = networkObject.gameObject.GetComponent<PoolCustomBehaviour>();
                if (customBehaviour != null)
                {
                    customBehaviour.Reset();
                }
                
                networkObject.gameObject.SetActive(false);
            }
            
            void ActionOnDestroy(NetworkObject networkObject)
            {
                Destroy(networkObject.gameObject);
            }
            
            _prefabs.Add(prefab);
            
            _pooledObjects[prefab] = new ObjectPool<NetworkObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);
            
            var prewarmNetworkObjects = new List<NetworkObject>();
            for (int i = 0; i < prewarmCount; i++)
            {
                prewarmNetworkObjects.Add(_pooledObjects[prefab].Get());
            }
            
            foreach (var networkObject in prewarmNetworkObjects)
            {
                _pooledObjects[prefab].Release(networkObject);
            }

            NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
        }
    }
}