using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.PlayerSpawn
{
    public class PlayerSpawn : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private List<string> _tags;
        private int _index;

        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        public override void OnNetworkDespawn()
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            
            var spawnPoint = GetNextSpawnPoint();
            GameObject playerObject = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
            playerObject.tag = _tags[_index % _tags.Count];
            networkObject.SpawnAsPlayerObject(clientId, true);
        }
        
        private Transform GetNextSpawnPoint()
        {
            var spawnPoint = _spawnPoints[_index % _spawnPoints.Count];
            _index++;
            return spawnPoint;
        }
    }
}