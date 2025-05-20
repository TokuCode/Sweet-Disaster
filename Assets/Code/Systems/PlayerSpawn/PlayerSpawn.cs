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
        private int _spawnPointIndex;

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
            NetworkObject playerObject = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation).GetComponent<NetworkObject>();
            playerObject.SpawnAsPlayerObject(clientId, true);
        }
        
        private Transform GetNextSpawnPoint()
        {
            var spawnPoint = _spawnPoints[_spawnPointIndex % _spawnPoints.Count];
            _spawnPointIndex++;
            return spawnPoint;
        }
    }
}