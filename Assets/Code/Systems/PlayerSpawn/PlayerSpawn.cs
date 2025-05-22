using Code.Systems.Session;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Code.Systems.PlayerSpawn
{
    public class PlayerSpawn : NetworkBehaviour
    {
        [SerializeField] GameObject characterDefaultPrefab;

        [Header("Character Prefabs")]
        [SerializeField] private List<CharacterPrefab> characterPrefabs;

        [Header("Spawn Points")]
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private List<string> _tags;
        private int _index;

        [Serializable]
        public struct CharacterPrefab
        {
            public string characterName;
            public GameObject characterPrefab;
        }

        public override void OnNetworkSpawn()
        {
            // Only the server should spawn players
            if (IsServer)
            {
                SpawnAllPlayers();
            }
        }

        private void SpawnAllPlayers()
        {
            if (SessionManager.Instance == null || SessionManager.Instance.ActiveSession == null)
            {
                Debug.LogError("SessionManager or ActiveSession is missing!");
                return;
            }

            foreach (var sessionPlayer in SessionManager.Instance.ActiveSession.Players)
            {
                SpawnPlayer(sessionPlayer);
            }
        }

        private void SpawnPlayer(IReadOnlyPlayer sessionPlayer)
        {
            // 1. Get the player's authentication ID
            if (!sessionPlayer.Properties.TryGetValue(SessionManager.Instance.playerAuthIdPropertyKey, out var authIdProp))
            {
                Debug.LogError($"Authentication ID not found for player: {sessionPlayer.Id}");
                return;
            }

            string authId = authIdProp.Value;

            // 2. Map authentication ID to client ID
            if (!SessionManager.Instance.playerIdToClientId.TryGetValue(authId, out ulong clientId))
            {
                Debug.LogError($"Client ID not found for authentication ID: {authId}");
                return;
            }

            // 3. Get the player's selected character
            if (!sessionPlayer.Properties.TryGetValue(SessionManager.Instance.playerCharacterPropertyKey, out var characterProp))
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

            // 4. Spawn the player
            Transform spawnPoint = GetNextSpawnPoint();
            GameObject playerObj = Instantiate(character.characterPrefab, spawnPoint.position, spawnPoint.rotation);
            //GameObject playerObj = Instantiate(characterDefaultPrefab, spawnPoint.position, spawnPoint.rotation);

            NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();
            playerObject.tag = _tags[_index % _tags.Count];
            networkObject.SpawnAsPlayerObject(clientId);

            //sessionPlayer.Properties.TryGetValue(SessionManager.Instance.playerColorPropertyKey, out var colorProp);
            //networkObject.transform.GetChild(0).GetComponent<SpriteRenderer>().color = SessionManager.Instance.colors[colorProp.Value];
        }

        private Transform GetNextSpawnPoint()
        {
            var spawnPoint = _spawnPoints[_index % _spawnPoints.Count];
            _index++;
            return spawnPoint;
        }
    }
}