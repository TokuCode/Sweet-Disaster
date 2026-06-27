using System;
using System.Collections.Generic;
using System.Linq;
using Code.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class PlayerVisibility : NetworkBehaviour
    {
        public static PlayerVisibility Instance;
        public List<PlayerPublicInfo> Players = new();
        [SerializeField] private PlayerSpawner _spawner;
        
        public event Action<PlayerPublicInfo> PlayerAdded;
        
        private readonly Dictionary<string, Color> _playerColors = new()
        {
            { "blue", Color.blue },
            { "red", Color.red },
            { "yellow", Color.yellow },
            { "green", Color.green }
        }; 
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"There is more than one instance of {GetType().Name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public PlayerPublicInfo PostPlayer(PlayerController player, bool isPlayer)
        {
            ulong clientId = (ulong)player.clientId;
            
            if (!SessionManager.Instance.TryGetPlayerByClientId(clientId, out var playerProps))
            {
                Debug.LogWarning($"Could not find session player for clientId: {clientId}");
            }

            string playerName = SessionManager.Instance.GetPlayerName(playerProps);
            Color playerColor = SessionManager.Instance.GetPlayerColor(playerProps);
            string characterName = SessionManager.Instance.GetPlayerCharacter(playerProps);
            
            CharacterScriptable scriptable = _spawner.GetCharacter(characterName);
            Sprite characterIcon = scriptable.characterIcon;
            
            var newPlayer = new PlayerPublicInfo
            {
                player = player,
                isPlayer = isPlayer,
                playerName = playerName,
                playerColor = playerColor,
                playerIcon = characterIcon,
                scriptable = scriptable
            };
            Players.Add(newPlayer);
            PlayerAdded?.Invoke(newPlayer);
            
            return newPlayer;
        }
        
        public Color GetPlayerColor(PlayerController player)
        {
            var playerPublicInfo = Players.FirstOrDefault(playerInfo => playerInfo.player == player);
            return playerPublicInfo.playerColor;
        }

        public Sprite GetPlayerSprite(PlayerController player)
        {
            var playerPublicInfo = Players.FirstOrDefault(playerInfo => playerInfo.player == player);
            return playerPublicInfo.playerIcon;
        }

        public string GetPlayerName(PlayerController player)
        {
            var playerPublicInfo = Players.FirstOrDefault(playerInfo => playerInfo.player == player);
            return playerPublicInfo.playerName;
        }
    }

    [Serializable]
    public struct PlayerPublicInfo : IComparable<PlayerPublicInfo>
    {
        public PlayerController player;
        public bool isPlayer;
        
        public string playerName;
        public Color playerColor;
        public Sprite playerIcon;
        
        public CharacterScriptable scriptable;

        public int CompareTo(PlayerPublicInfo other)
        {
            return player.clientId - other.player.clientId;
        }
    }
}