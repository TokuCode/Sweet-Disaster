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
        public List<PlayerPublicInfo> players = new();
        private int playerCount;
        [SerializeField] private GameObject _portraitPrefab;
        
        [Header("UI Elements")]
        [SerializeField] private Transform _portraitContainer;

        public override void OnNetworkSpawn()
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += SetPlayerCount;
            PlayerVisibility.Instance.PlayerAdded += OnPlayerPost;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SetPlayerCount;
        }

        private void SetPlayerCount(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            playerCount = SessionManager.Instance.ActiveSession.Players.Count;
        }

        public void OnPlayerPost(PlayerPublicInfo player)
        { 
            players.Add(player);
            if (players.Count == playerCount)
            {
                SpawnPortraits();
            }
        }

        public void SpawnPortraits()
        {
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