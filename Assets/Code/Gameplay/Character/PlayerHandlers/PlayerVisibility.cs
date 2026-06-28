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
            
            if (!SessionManager.Instance.TryGetSessionPlayerByClientId(clientId, out var sessionPlayer))
            {
                Debug.LogWarning($"Could not find session player data for clientId: {clientId}");
                //return null;
            }
            
            Debug.Log(
                $"[PostPlayer] clientId: {clientId}, " +
                $"name: {sessionPlayer.PlayerName}, " +
                $"color: {sessionPlayer.PlayerColor}, " +
                $"character: '{sessionPlayer.CharacterName}'"
            );

            string playerName = sessionPlayer.PlayerName;
            Color playerColor = sessionPlayer.PlayerColor;
            string characterName = sessionPlayer.CharacterName;
            
            CharacterScriptable scriptable = _spawner.GetCharacter(sessionPlayer.CharacterName);

            Debug.Log(
                $"[PostPlayer] scriptable: {(scriptable != null ? scriptable.characterName : "NULL")}, " +
                $"icon: {(scriptable != null && scriptable.characterIcon != null ? scriptable.characterIcon.name : "NULL")}"
            );
            
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