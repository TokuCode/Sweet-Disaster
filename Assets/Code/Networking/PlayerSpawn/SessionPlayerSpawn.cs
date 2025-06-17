using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
using System;
using UnityEngine.SceneManagement;

namespace Code.Networking.PlayerSpawn
{
    [Serializable]
    public struct CharacterPrefab
    {
        public string characterName;
        public GameObject characterPrefab;
    }
    public class SessionPlayerSpawn : NetworkBehaviour
    {
        [SerializeField] private GameObject characterDefaultPrefab;

        [Header("Character Prefabs")]
        [SerializeField] private List<CharacterPrefab> characterPrefabs;

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

        private void SpawnAllPlayers(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;
            if (_sessionManager == null || _sessionManager.ActiveSession == null)
            {
                Debug.LogError("SessionManager or ActiveSession is missing!");
                return;
            }

            foreach (var sessionPlayer in _sessionManager.ActiveSession.Players)
            {
                SpawnPlayer(sessionPlayer);
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
            CharacterPrefab character = characterPrefabs.Find(c => c.characterName == characterName);

            if (character.characterPrefab == null)
            {
                Debug.LogError($"Character prefab not found: {characterName}");
                return;
            }

            // Spawn the player
            Transform spawnPoint = _spawnPoints[_index % _spawnPoints.Count];
            GameObject playerObj = Instantiate(character.characterPrefab, spawnPoint.position, spawnPoint.rotation);
            
            playerObj.tag = _tags[_index % _tags.Count];
            
            _index++;

            NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, true);
        }
    }   
}