using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.PlayerSpawn
{
    public class PlayerSpawn : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<PlayerConfigurationData> _configuration;
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

            if (TryGetNextPlayerData(out var playerConfig))
            {
                var player = Instantiate(playerConfig.prefab, playerConfig.spawn.position, playerConfig.spawn.rotation);
                player.transform.localScale = playerConfig.spawn.localScale;
                player.tag = playerConfig.tag;
                
                NetworkObject playerNetwork = player.GetComponent<NetworkObject>();
                playerNetwork.SpawnAsPlayerObject(clientId, true);
            }
        }
        
        private bool TryGetNextPlayerData(out PlayerConfigurationData playerConfig)
        {
            if (_index >= _configuration.Count)
            {
                playerConfig = default;
                return false;
            }
            
            playerConfig = _configuration[_index];
            _index++;
            return true;
        }
    }
}