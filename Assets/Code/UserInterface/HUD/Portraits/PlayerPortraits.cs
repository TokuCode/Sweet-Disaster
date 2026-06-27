using System.Collections.Generic;
using Code.Gameplay.Character;
using Code.Helpers.MergeSort;
using Code.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class PlayerPortraits : NetworkBehaviour
    {
        private int _playerCount;
        private bool _playersLoaded;
        private bool _portraitsLoaded;
        [SerializeField] private GameObject _portraitPrefab;
        [SerializeField] private GameObject _playerPositionPrefab;
        
        [Header("UI Elements")]
        [SerializeField] private Transform _portraitContainer;
        [SerializeField] private Transform _playerPositionContainer;

        public override void OnNetworkSpawn()
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += SetPlayerCount;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SetPlayerCount;
        }

        private void Update()
        {
            if (PlayerVisibility.Instance.Players.Count == _playerCount && _playersLoaded && !_portraitsLoaded)
            {
                SpawnPlayerElements();
                _portraitsLoaded = true;
            }
        }

        private void SetPlayerCount(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            _playerCount = SessionManager.Instance.GetPlayers().Count;
            _playersLoaded = true;
        }

        private void SpawnPlayerElements()
        {
            var players = new List<PlayerPublicInfo>(PlayerVisibility.Instance.Players);
            MergeSortUtil<PlayerPublicInfo>.MergeSort(players);
            foreach (var player in players)
            {
                SpawnPortrait(player);
                SpawnPositionIndicator(player);
            }
        }

        private void SpawnPortrait(PlayerPublicInfo playerInfo)
        {
            var spawned = Instantiate(_portraitPrefab, _portraitContainer);
            var portrait = spawned.GetComponent<PlayerPortrait>();
            portrait.CachePlayerInfo(playerInfo);
        }

        private void SpawnPositionIndicator(PlayerPublicInfo playerInfo)
        {
            var spawned = Instantiate(_playerPositionPrefab, _playerPositionContainer);
            var indicator = spawned.GetComponent<PlayerPositionIndicator>();
            indicator.CachePlayerInfo(playerInfo);
        }
    }
}