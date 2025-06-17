using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    public class ObjectPoolWithId
    {
        private readonly int _prewarm;
        private List<NetworkObject> _pooledObjects = new ();
        public int Count => _pooledObjects.Count;
        public int ActiveCount => _pooledObjects.Count(no => _checkActive(no));
        private int _currentId;
        
        private Func<NetworkObject> _createFunc;
        private Action<NetworkObject> _onGet;
        private Action<NetworkObject> _onRelease;
        private Action<NetworkObject> _onDestroy;
        private Func<NetworkObject, bool> _checkActive;

        public ObjectPoolWithId(Func<NetworkObject> CreateFunc, Action<NetworkObject> ActionOnGet, Action<NetworkObject> ActionOnRelease, Action<NetworkObject> ActionOnDestroy, Func<NetworkObject, bool> checkActive, int prewarm)
        {
            _prewarm = prewarm;
            _createFunc = CreateFunc;
            _onGet = ActionOnGet;
            _onRelease = ActionOnRelease;
            _onDestroy = ActionOnDestroy;
            _checkActive = checkActive;
            _currentId = 0;

            for (int i = 0; i < _prewarm; i++)
                Release(Create(out int id));
        }

        private NetworkObject Create(out int id)
        {
            NetworkObject no = _createFunc();
            id = _currentId;
            _currentId++;
            
            _pooledObjects.Add(no);

            return no;
        }

        private void Destroy(NetworkObject networkObject)
        {
            _pooledObjects.Remove(networkObject);
            _onDestroy?.Invoke(networkObject);
        }

        public NetworkObject Get(out int id)
        {
            NetworkObject no = null;
            id = -1;
            
            if (ActiveCount >= Count) no = Create(out id);
            else
            {
                for (var i = 0; i < _pooledObjects.Count; i++)
                {
                    no = _pooledObjects[i];
                    if (!_checkActive(no))
                    {
                        id = i;
                        break;
                    }
                }
            }
            
            _onGet?.Invoke(no);
            return no;
        }

        public NetworkObject GetById(int id = -1)
        {
            if (id == -1) return null;

            NetworkObject no = _pooledObjects[id];
            _onGet?.Invoke(no);
            
            return no;
        }

        public NetworkObject GetReferenceById(int id = -1)
        {
            if (id == -1) return null;
            
            NetworkObject no = _pooledObjects[id];
            
            if (!_checkActive(no)) return null;

            return no;
        }

        public void ReleaseById(int id = -1)
        {
            if (id == -1) return;
            
            NetworkObject no = _pooledObjects[id];
            _onRelease?.Invoke(no);
        }

        public void Release(NetworkObject networkObject)
        {
            _onRelease?.Invoke(networkObject);
        }

        public void Clear()
        {
            for (var i = 0; i < _pooledObjects.Count; i++)
                Destroy(_pooledObjects[i]);
            
            _pooledObjects.Clear();
        }
    }
}