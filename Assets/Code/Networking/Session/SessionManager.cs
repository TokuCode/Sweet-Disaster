using Code.Helpers.Singleton;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Code.Helpers.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

namespace Code.Networking.Session
{
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        private ISession _activeSession;
        [SerializeField] private int maxPlayers;
        private Dictionary<string, ulong> playerIdToClientId = new();
        
        public ISession ActiveSession
        {
            get => _activeSession;
            set
            {
                if (_activeSession != null)
                {
                    _activeSession.Changed -= OnSessionChanged;
                    _activeSession.PlayerHasLeft -= OnPlayerHasLeft;

                    _activeSession.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
                    _activeSession.PlayerLeaving -= OnPlayerLeaving;
                }

                _activeSession = value;

                if (_activeSession != null)
                {
                    _activeSession.Changed += OnSessionChanged;
                    _activeSession.PlayerHasLeft += OnPlayerHasLeft;

                    _activeSession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
                    _activeSession.PlayerLeaving += OnPlayerLeaving;
                }

#if UNITY_EDITOR
                Debug.Log($"Active session: {_activeSession}");
#endif
            }
        }

        [SerializeField] private PlayerInfo playerInfo;
        public bool IsPracticeMode { get; private set; }
        public bool ShouldRetrievePing { get; private set; } = true;
        
        // Session properties keys
        public readonly string MapPropertyKey = "map";
        
        // Player properties keys
        public readonly string PlayerNameKey = "playerName";
        public readonly string PlayerColorKey = "playerColor";
        public readonly string PlayerCharacterKey = "playerCharacter";
        
        //public Dictionary<string, ulong> PlayerIdToClientId => playerIdToClientId;

        private async void Start() => await InitializeServices();

        private async Task InitializeServices()
        {
            try
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously, PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo conectar a los servicios online", true);
            }
        }
        
        public async void StartSessionAsHost(bool isPracticeMode)
        {
            ConnectionMode = SessionConnectionMode.Online;
            IsPracticeMode = isPracticeMode;
            
            try
            {
                UIUtilities.Instance.MessagePopUp("Creando la sesión...", false);
                
                var options = new SessionOptions
                {
                    MaxPlayers = !isPracticeMode ? maxPlayers : 1,
                    IsPrivate = true,
                
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { MapPropertyKey, new SessionProperty("Classic", VisibilityPropertyOptions.Member) }
                    },
                    
                    PlayerProperties = SetPlayerProperties()
                    
                }.WithRelayNetwork();
                
                ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options); // Create the session as host
#if UNITY_EDITOR
                Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
#endif
                if (!ActiveSession.IsHost) return;

                if (IsPracticeMode)
                {
                    try
                    {
                        ActiveSession.CurrentPlayer.SetProperty(PlayerCharacterKey,
                            new PlayerProperty("Gladys", VisibilityPropertyOptions.Member));
                        await ActiveSession.SaveCurrentPlayerDataAsync();
                        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        UIUtilities.Instance.MessagePopUp("No se pudo crear la sesión", true);
                    }
                }
                else
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo crear la sesión", true);
            }
        }
        
        public async void JoinSessionByCode(string code)
        {
            ConnectionMode = SessionConnectionMode.Online;
            IsPracticeMode = false;
            
            try
            {
                UIUtilities.Instance.MessagePopUp("Uniéndose a la sesión...", false); ;

                ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code); // Join the player to the session by code
                
                ActiveSession.CurrentPlayer.SetProperties(SetPlayerProperties());
                await ActiveSession.SaveCurrentPlayerDataAsync();
#if UNITY_EDITOR
                Debug.Log($"Session {ActiveSession.Id} joined");
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo unir a la sesión", true);
            }
        }
        
        public async void LeaveSession()
        {
            if (ActiveSession == null) return;
            try
            {
                ShouldRetrievePing = false;
                playerIdToClientId.Clear();
                await ActiveSession.LeaveAsync();
            }
            catch
            {
                // Do nothing as we are leaving the session
            }
            finally
            {
                ActiveSession = null;
            }
        }

        private Dictionary<string, PlayerProperty> SetPlayerProperties()
        {
            return new Dictionary<string, PlayerProperty>
            {
                { PlayerNameKey, new PlayerProperty(IsPracticeMode ? "Gladys" : playerInfo.playerDisplayName, VisibilityPropertyOptions.Member) },
                { PlayerColorKey, new PlayerProperty(playerInfo.GetAvailableColorName(), VisibilityPropertyOptions.Member) },
                { PlayerCharacterKey, new PlayerProperty(String.Empty, VisibilityPropertyOptions.Member) },
            };
        }
        
        // Wrapper methods to hide online logic from other scripts
        
        public IReadOnlyList<IReadOnlyPlayer> GetPlayers()
        {
            if (ActiveSession == null)
                return Array.Empty<IReadOnlyPlayer>();
            return ActiveSession.Players;
        }

        public int PlayerCount => ActiveSession?.PlayerCount ?? 0;

        public bool IsLocalPlayerSessionHost => ActiveSession != null && ActiveSession.IsHost;

        public IReadOnlyPlayer GetPlayer(string playerId)
        {
            return ActiveSession.Players.FirstOrDefault(p => p.Id == playerId);
        }
        
        // Player info helper methods wrapped here?
        
        public string GetPlayerName(IReadOnlyPlayer player)
        {
            return playerInfo.GetPropertyValue(player, PlayerNameKey);
        }

        public Color GetPlayerColor(IReadOnlyPlayer player)
        {
            return playerInfo.GetColor(player);
        }

        public string GetPlayerCharacter(IReadOnlyPlayer player)
        {
            return playerInfo.GetPropertyValue(player, PlayerCharacterKey);
        }
        
        // Events
        
        public event Action SessionChanged;

        private void OnSessionChanged()
        {
            SessionChanged?.Invoke();
        }
        
        public event Action<string> PlayerLeft;
        
        private void OnPlayerHasLeft(string playerId)
        {
            PlayerLeft?.Invoke(playerId);
        }

        private void OnDisable()
        {
            if (ActiveSession != null)
            {
                ActiveSession.Changed -= OnSessionChanged;
                ActiveSession.PlayerHasLeft -= OnPlayerHasLeft;
            }
        }
        
        public string HostPlayerId
        {
            get
            {
                if (ActiveSession == null)
                    return string.Empty;

                return ActiveSession.Host;
            }
        }

        public bool IsHostPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            return playerId == HostPlayerId;
        }
        
        public bool TryGetPlayerByClientId(ulong clientId, out IReadOnlyPlayer player)
        {
            player = null;

            if (ActiveSession == null)
                return false;

            foreach (var sessionPlayer in ActiveSession.Players)
            {
                if (!playerIdToClientId.TryGetValue(sessionPlayer.Id, out ulong mappedClientId))
                    continue;

                if (mappedClientId != clientId)
                    continue;

                player = sessionPlayer;
                return true;
            }

            return false;
        }
        
        public bool HasActiveSession => ActiveSession != null;

        public bool TryGetSessionProperty(string key, out string value)
        {
            value = string.Empty;

            if (ActiveSession == null)
                return false;

            if (!ActiveSession.Properties.TryGetValue(key, out SessionProperty property))
                return false;

            value = property.Value;
            return true;
        }
        
        public bool TryGetCurrentPlayerId(out string playerId)
        {
            playerId = string.Empty;

            if (ActiveSession?.CurrentPlayer == null)
                return false;

            playerId = ActiveSession.CurrentPlayer.Id;
            return !string.IsNullOrEmpty(playerId);
        }

        public bool TryRegisterPlayerClientId(string playerId, ulong clientId)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            if (playerIdToClientId.TryGetValue(playerId, out ulong existingClientId))
            {
                if (existingClientId == clientId)
                    return false;

                playerIdToClientId[playerId] = clientId;
                return true;
            }

            playerIdToClientId.Add(playerId, clientId);
            return true;
        }

        public bool TryGetClientIdFromPlayerId(string playerId, out ulong clientId)
        {
            return playerIdToClientId.TryGetValue(playerId, out clientId);
        }

        public void RemovePlayerClientId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            playerIdToClientId.Remove(playerId);
        }
        
        public string JoinDisplayCode
        {
            get
            {
                if (ActiveSession == null)
                    return string.Empty;

                return ActiveSession.Code;
            }
        }
        
        public event Action PlayerPropertiesChanged;
        public event Action<string> PlayerLeaving;
        
        private void OnPlayerPropertiesChanged()
        {
            this.PlayerPropertiesChanged?.Invoke();
        }

        private void OnPlayerLeaving(string playerId)
        {
            this.PlayerLeaving?.Invoke(playerId);
        }
        
        public bool IsCurrentPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            if (!TryGetCurrentPlayerId(out string currentPlayerId))
                return false;

            return playerId == currentPlayerId;
        }
        
        public bool IsCharacterTaken(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return false;

            if (ActiveSession == null)
                return false;

            return ActiveSession.Players.Any(player =>
                player.Properties.TryGetValue(PlayerCharacterKey, out var prop) &&
                prop.Value == characterName);
        }
        
        public async Task<bool> TrySelectCurrentPlayerCharacterAsync(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return false;

            if (ActiveSession?.CurrentPlayer == null)
                return false;

            if (IsCharacterTaken(characterName))
                return false;

            ActiveSession.CurrentPlayer.SetProperty(
                PlayerCharacterKey,
                new PlayerProperty(characterName, VisibilityPropertyOptions.Member));

            await ActiveSession.SaveCurrentPlayerDataAsync();

            return true;
        }
        
        public bool HaveAllPlayersClearedCharacterSelection()
        {
            if (ActiveSession == null)
                return false;

            foreach (var player in ActiveSession.Players)
            {
                if (!player.Properties.TryGetValue(PlayerCharacterKey, out var charProp))
                    return false;

                if (!string.IsNullOrEmpty(charProp.Value))
                    return false;
            }

            return true;
        }

        public async Task<bool> TryClearCurrentPlayerCharacterAsync()
        {
            if (ActiveSession?.CurrentPlayer == null)
                return false;

            ActiveSession.CurrentPlayer.SetProperty(
                PlayerCharacterKey,
                new PlayerProperty(string.Empty, VisibilityPropertyOptions.Member));

            await ActiveSession.SaveCurrentPlayerDataAsync();
            return true;
        }
        
        public List<SessionPlayerData> GetSessionPlayers()
        {
            var result = new List<SessionPlayerData>();

            if (ActiveSession == null)
                return result;

            foreach (var player in ActiveSession.Players)
            {
                if (!TryGetClientIdFromPlayerId(player.Id, out ulong clientId))
                {
                    Debug.LogWarning($"Client ID not found for player ID: {player.Id}");
                    continue;
                }

                result.Add(new SessionPlayerData
                {
                    PlayerId = player.Id,
                    ClientId = clientId,
                    PlayerName = GetPlayerName(player),
                    PlayerColor = GetPlayerColor(player),
                    CharacterName = GetPlayerCharacter(player),
                    IsHost = IsHostPlayer(player.Id),
                    IsCurrentPlayer = IsCurrentPlayer(player.Id)
                });
            }

            return result;
        }
        
        public string LocalPlayerDisplayName
        {
            get
            {
                if (playerInfo == null)
                    return string.Empty;

                return playerInfo.playerDisplayName;
            }
            set
            {
                if (playerInfo == null)
                    return;

                playerInfo.playerDisplayName = value;
            }
        }

        public bool HasLocalPlayerDisplayName => !string.IsNullOrEmpty(LocalPlayerDisplayName);
        
        public SessionConnectionMode ConnectionMode { get; private set; } = SessionConnectionMode.Online;

        public bool IsOnlineMode => ConnectionMode == SessionConnectionMode.Online;
        public bool IsLanMode => ConnectionMode == SessionConnectionMode.Lan;
    }
    
    public enum SessionConnectionMode
    {
        Online,
        Lan
    }
}