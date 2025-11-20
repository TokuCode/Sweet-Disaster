using UnityEngine;
using System.Collections.Generic;
using Code.Gameplay.Character;
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

        [Header("Character's info")]
        [SerializeField] private List<CharacterScriptable> charactersInfo;

        [Header("Spawn Points")]
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private Transform tutorialSpawnPoint;

        private List<int> _spawnPointIndexes = new();
        [SerializeField] private List<string> _tags;
        private int _index;
        
        [Header("Respawn Logic")]
        
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
            
            for (int i = 0; i < _sessionManager.ActiveSession.Players.Count; i++)
            {
                var  sessionPlayer = _sessionManager.ActiveSession.Players[i];
                SpawnPlayer(sessionPlayer, i);
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
		
        private void SpawnPlayer(IReadOnlyPlayer sessionPlayer, int playerNumber)
        {
            // Map authentication ID to client ID
            if (!SessionManager.Instance.PlayerIdToClientId.TryGetValue(sessionPlayer.Id, out ulong clientId))
            {
                Debug.LogError($"Client ID not found for authentication ID: {clientId}");
                return;
            }

            string characterName;

            // Get the player's selected character
            if (!sessionPlayer.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var characterProp))
            {
                characterName = "Ceci";
                Debug.LogError($"Character not selected for player: {sessionPlayer.Id}");
                //return;
            }

            characterName = characterProp?.Value;
            CharacterScriptable character = GetCharacter(characterName);

            // Spawn the player
            
            Transform spawnPoint = _spawnPoints[GetRandomIndexNotFromPrevious()];
            if (_sessionManager.IsPracticeMode) spawnPoint = tutorialSpawnPoint;
            
            GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            playerObj.tag = _tags[_index % _tags.Count];
            playerObj.GetComponent<AnimationHandler>().SetAnimator(character);
            playerObj.GetComponent<ArmSpriteChanger>().SetSprites(character);
            
            playerObj.GetComponent<PlayerController>().SetSpawnPosition(spawnPoint);
            playerObj.GetComponent<PlayerController>().SetNumber(playerNumber);
            
            NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, true);

            SetTagForClientRpc(clientId, _index);
            SetAnimatorClientRpc(clientId, characterName);
            
            _index++;
        }
        
        public CharacterScriptable GetCharacter(string characterName)
        {
            CharacterScriptable character = charactersInfo.Find(c => c.characterName == characterName);

            if (character == null)
            {
                Debug.LogError($"Character not found: {characterName}, assigning first character on list");
                character = charactersInfo[0];
            }
            
            return character;
        }

        private int GetRandomIndexNotFromPrevious()
        {
            int options = _spawnPoints.Count - _spawnPointIndexes.Count;

            if (options <= 0) return -1;
            
            int option = -1;
            int offset = Random.Range(0, _spawnPoints.Count);
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                int value = (i + offset) % _spawnPoints.Count;
                if (!_spawnPointIndexes.Contains(value))
                {
                    option = value;
                    break;
                }
            }
            if(option >= 0) _spawnPointIndexes.Add(option);
            return option;
        }

        [ClientRpc]
        private void SetTagForClientRpc(ulong clientId, int _index)
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.tag = _tags[_index % _tags.Count];
        }

        [ClientRpc]
        private void SetAnimatorClientRpc(ulong clientId, string characterName)
        {
            CharacterScriptable character = GetCharacter(characterName);
            
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.
                gameObject.GetComponent<AnimationHandler>().SetAnimator(character);
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.
                gameObject.GetComponent<ArmSpriteChanger>().SetSprites(character);
        }

        [ClientRpc]
        private void SetMapClientRpc(string mapName)
        {
            SelectMap(mapName);
        }
    }
}