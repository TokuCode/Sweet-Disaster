using System;
using System.Collections.Generic;
using Code.Helpers.Singleton;
using UnityEngine;
using UnityEngine.Pool;

namespace Code.Systems.NetworkObjectPool
{
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        [SerializeField] private List<SimplePoolConfigObject> PooledPrefabsList;

        HashSet<GameObject> m_Prefabs = new ();

        Dictionary<GameObject, ObjectPool<GameObject>> m_PooledObjects = new Dictionary<GameObject, ObjectPool<GameObject>>();

        private void Start()
        {
            // Registers all objects in PooledPrefabsList to the cache.
            foreach (var configObject in PooledPrefabsList)
            {
                RegisterPrefabInternal(configObject.prefab, configObject.prewarmCount);
            }
        }

        private void OnDestroy()
        {
            foreach (var prefab in m_Prefabs)
            {
                m_PooledObjects[prefab].Clear();
            }
            m_PooledObjects.Clear();
            m_Prefabs.Clear();
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var go = m_PooledObjects[prefab].Get();

            var goTransform = go.transform;
            goTransform.position = position;
            goTransform.rotation = rotation;

            return go;
        }

        /// <summary>
        /// Return an object to the pool (reset objects before returning).
        /// </summary>
        public void Return(GameObject go, GameObject prefab)
        {
            m_PooledObjects[prefab].Release(go);
        }

        /// <summary>
        /// Builds up the cache for a prefab.
        /// </summary>
        void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
        {
            GameObject CreateFunc()
            {
                return Instantiate(prefab);
            }

            void ActionOnGet(GameObject go)
            {
                go.SetActive(true);
            }

            void ActionOnRelease(GameObject go)
            {
                go.SetActive(false);
            }

            void ActionOnDestroy(GameObject go)
            {
                Destroy(go);
            }

            m_Prefabs.Add(prefab);

            // Create the pool
            m_PooledObjects[prefab] = new ObjectPool<GameObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);

            // Populate the pool
            var prewarmNetworkObjects = new List<GameObject>();
            for (var i = 0; i < prewarmCount; i++)
            {
                prewarmNetworkObjects.Add(m_PooledObjects[prefab].Get());
            }
            foreach (var networkObject in prewarmNetworkObjects)
            {
                m_PooledObjects[prefab].Release(networkObject);
            }
        }
    }
}