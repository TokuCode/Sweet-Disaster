using UnityEngine;
using System.Collections.Generic;
using Code.Networking.Session;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using Code.Gameplay.Character.Visuals;
using Code.Networking;

namespace Code.Gameplay
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private List<MapInfo> maps;

        [Header("Character Prefabs")]
        [SerializeField] private List<CharacterVisuals> characterVisualsList;

        [Header("Spawn Points")]
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private List<string> _tags;
        private int _index;
        
        private SessionManager _sessionManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkManager.SceneManager.OnLoadEventCompleted += SpawnAllPlayers;
            _sessionManager = SessionManager.Instance;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnAllPlayers;
        }

        private void SpawnAllPlayers(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;
            if (_sessionManager == null || _sessionManager.ActiveSession == null)
            {
                Debug.LogError("SessionManager or ActiveSession is missing!");
                return;
            }
            
            if(_sessionManager.ActiveSession.Properties.TryGetValue(_sessionManager.MapPropertyKey, out SessionProperty mapProperty))
            {
                SelectMap(mapProperty.Value);
                SetMapClientRpc(mapProperty.Value);
            }

            foreach (var sessionPlayer in _sessionManager.ActiveSession.Players)
            {
                SpawnPlayer(sessionPlayer);
            }
        }

        private void SelectMap(string mapName)
        {
            foreach (var map in maps)
            {
                if (!map.MapName.Equals(mapName))
                {
                    map.MapObject.SetActive(false);
                }
                else map.MapObject.SetActive(true);
            }
        }
		
        private void SpawnPlayer(IReadOnlyPlayer sessionPlayer)
        {
            // Map authentication ID to client ID
            if (!SessionManager.Instance.PlayerIdToClientId.TryGetValue(sessionPlayer.Id, out ulong clientId))
            {
                Debug.LogError($"Client ID not found for authentication ID: {clientId}");
                return;
            }

            // Get the player's selected character
            if (!sessionPlayer.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var characterProp))
            {
                Debug.LogError($"Character not selected for player: {sessionPlayer.Id}");
                return;
            }

            string characterName = characterProp.Value;
            CharacterVisuals character = characterVisualsList.Find(c => c.characterName == characterName);

            if (character == null)
            {
                Debug.LogError($"Character not found: {characterName}, assigning first character on list");
                character = characterVisualsList[0];
            }

            // Spawn the player
            Transform spawnPoint = _spawnPoints[_index % _spawnPoints.Count];
            GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            playerObj.tag = _tags[_index % _tags.Count];
            playerObj.GetComponent<AnimationHandler>().SetVisuals(character);
            
            NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, true);

            SetTagForClientRpc(clientId, _index);
            SetAnimatorClientRpc(clientId, characterName);
            
            _index++;
        }

        [ClientRpc]
        private void SetTagForClientRpc(ulong clientId, int _index)
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.tag = _tags[_index % _tags.Count];
        }

        [ClientRpc]
        private void SetAnimatorClientRpc(ulong clientId, string characterName)
        {
            CharacterVisuals character = characterVisualsList.Find(c => c.characterName == characterName);

            if (character == null)
            {
                Debug.LogError($"Character not found: {characterName}, assigning first character on list");
                character = characterVisualsList[0];
            }
            
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.GetComponent<AnimationHandler>().SetVisuals(character);
        }

        [ClientRpc]
        private void SetMapClientRpc(string mapName)
        {
            SelectMap(mapName);
        }
    }
}