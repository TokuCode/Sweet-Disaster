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
        
        [Header("UI Elements")]
        [SerializeField] private Transform _portraitContainer;

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
                SpawnPortraits();
                _portraitsLoaded = true;
            }
        }

        private void SetPlayerCount(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            _playerCount = SessionManager.Instance.ActiveSession.Players.Count;
            _playersLoaded = true;
        }

        public void SpawnPortraits()
        {
            var players = new List<PlayerPublicInfo>(PlayerVisibility.Instance.Players);
            MergeSortUtil<PlayerPublicInfo>.MergeSort(players);
            foreach (var player in players)
            {
                SpawnPortrait(player);
            }
        }

        public void SpawnPortrait(PlayerPublicInfo playerInfo)
        {
            var spawned = Instantiate(_portraitPrefab, _portraitContainer);
            var portrait = spawned.GetComponent<PlayerPortrait>();
            portrait.CachePlayerInfo(playerInfo);
        }
    }
}