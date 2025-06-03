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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkManager.SceneManager.OnLoadEventCompleted += SpawnAllPlayers;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnAllPlayers;
        }

        private void SpawnAllPlayers(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;
            if (SessionManager.Instance == null || SessionManager.Instance.ActiveSession == null)
            {
                Debug.LogError("SessionManager or ActiveSession is missing!");
                return;
            }

            foreach (var sessionPlayer in SessionManager.Instance.ActiveSession.Players) 
                SpawnPlayer(sessionPlayer);
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
            if (!sessionPlayer.Properties.TryGetValue(SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerCharacter], out var characterProp))
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
            Transform spawnPoint = GetNextSpawnPoint();
            GameObject playerObj = Instantiate(character.characterPrefab, spawnPoint.position, spawnPoint.rotation);

            NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();
            playerObj.gameObject.tag = _tags[_index % _tags.Count];
            
            networkObject.SpawnAsPlayerObject(clientId);
        }

        private Transform GetNextSpawnPoint()
        {
            var spawnPoint = _spawnPoints[_index % _spawnPoints.Count];
            _index++;
            return spawnPoint;
        }
    }   
}