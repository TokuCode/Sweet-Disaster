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
using System.Linq;
using Unity.Netcode.Transports.UTP;

namespace Code.Networking.Session
{
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        private ISession _activeSession;
        [SerializeField] private int maxPlayers;
        private Dictionary<string, ulong> playerIdToClientId = new();
        [SerializeField] private ushort lanPort = 7777;
        
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

        //private async void Start() => await InitializeServices();

        private bool _servicesInitialized;

        private async Task<bool> InitializeServices()
        {
            if (_servicesInitialized)
                return true;

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                _servicesInitialized = true;

                Debug.Log($"Signed in anonymously, PlayerID: {AuthenticationService.Instance.PlayerId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo conectar a los servicios online", true);
                return false;
            }
        }
        
        public async void StartSessionAsHost(bool isPracticeMode)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UIUtilities.Instance.MessagePopUp("Ya hay una sesión activa", true);
                return;
            }

            
            ConnectionMode = SessionConnectionMode.Online;
            IsPracticeMode = isPracticeMode;
            
            if (!await InitializeServices())
                return;
            
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
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UIUtilities.Instance.MessagePopUp("Ya hay una sesión activa", true);
                return;
            }

            
            ConnectionMode = SessionConnectionMode.Online;
            IsPracticeMode = false;
            
            if (!await InitializeServices())
                return;
            
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
            await LeaveSessionAsync();
        }
        
        public async Task LeaveSessionAsync()
        {
            ShouldRetrievePing = false;

            try
            {
                if (IsOnlineMode && ActiveSession != null)
                {
                    try
                    {
                        await ActiveSession.LeaveAsync();
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        Debug.LogException(e);
#endif
                    }

                    // Fallback: if Unity Session did not stop NGO, stop it manually.
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    {
                        await ShutdownNetworkAsync();
                    }
                }
                else
                {
                    // LAN has no ActiveSession, so we stop NGO manually.
                    await ShutdownNetworkAsync();
                }

                playerIdToClientId.Clear();
            }
            finally
            {
                ActiveSession = null;
                ClearLanSessionData();
                ConnectionMode = SessionConnectionMode.Online;
                ShouldRetrievePing = true;
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

        public int PlayerCount
        {
            get
            {
                if (IsLanMode)
                    return lanPlayers.Count;

                return ActiveSession?.PlayerCount ?? 0;
            }
        }

        public bool IsLocalPlayerSessionHost
        {
            get
            {
                if (IsLanMode)
                    return localLanPlayerId == lanHostPlayerId;

                return ActiveSession != null && ActiveSession.IsHost;
            }
        }

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

            if (IsLanMode)
                return playerId == lanHostPlayerId;

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
        
        public bool HasActiveSession
        {
            get
            {
                if (IsLanMode)
                    return lanSessionActive;

                return ActiveSession != null;
            }
        }

        public bool TryGetSessionProperty(string key, out string value)
        {
            value = string.Empty;

            if (IsLanMode)
            {
                if (key == MapPropertyKey)
                {
                    value = "Classic";
                    return true;
                }

                return false;
            }

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

            if (IsLanMode)
            {
                playerId = localLanPlayerId;
                return !string.IsNullOrEmpty(playerId);
            }

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

            if (IsLanMode)
            {
                return lanPlayers.Any(player =>
                    player.CharacterName == characterName);
            }

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
            if (IsLanMode)
                return new List<SessionPlayerData>(lanPlayers);

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
        
        private readonly List<SessionPlayerData> lanPlayers = new();

        private bool lanSessionActive;
        private string lanHostPlayerId;
        private string localLanPlayerId;
        
        private void ClearLanSessionData()
        {
            lanPlayers.Clear();
            lanSessionActive = false;
            lanHostPlayerId = string.Empty;
            localLanPlayerId = string.Empty;
        }
        
        private UnityTransport GetUnityTransport()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is missing.");
                return null;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
                Debug.LogError("UnityTransport component not found on NetworkManager.");

            return transport;
        }

        private bool ConfigureLanHostTransport()
        {
            var transport = GetUnityTransport();

            if (transport == null)
                return false;

            transport.SetConnectionData(
                "0.0.0.0",
                lanPort,
                "0.0.0.0"
            );

            return true;
        }

        private bool ConfigureLanClientTransport(string hostIp)
        {
            if (string.IsNullOrWhiteSpace(hostIp))
            {
                Debug.LogError("LAN host IP is empty.");
                return false;
            }

            var transport = GetUnityTransport();

            if (transport == null)
                return false;

            transport.SetConnectionData(
                hostIp,
                lanPort
            );

            return true;
        }
        
        private SessionPlayerData CreateLocalLanPlayer(ulong clientId, bool isHost)
        {
            if (!HasLocalPlayerDisplayName)
                LocalPlayerDisplayName = playerInfo.GetRandomName();

            string colorName = playerInfo.GetAvailableColorName();

            return new SessionPlayerData
            {
                PlayerId = localLanPlayerId,
                ClientId = clientId,
                PlayerName = LocalPlayerDisplayName,
                PlayerColorName = colorName,
                PlayerColor = playerInfo.GetColorFromName(colorName),
                CharacterName = string.Empty,
                IsHost = isHost,
                IsCurrentPlayer = true
            };
        }
        
        public async void StartLanSessionAsHost(bool isPracticeMode = false)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UIUtilities.Instance.MessagePopUp("Ya hay una sesión activa", true);
                return;
            }

            
            IsPracticeMode = isPracticeMode;
            ConnectionMode = SessionConnectionMode.Lan;

            try
            {
                UIUtilities.Instance.MessagePopUp("Creando sesión LAN...", false);

                ClearLanSessionData();

                if (!ConfigureLanHostTransport())
                {
                    ConnectionMode = SessionConnectionMode.Online;
                    UIUtilities.Instance.MessagePopUp("No se pudo configurar la conexión LAN", true);
                    return;
                }

                bool started = NetworkManager.Singleton.StartHost();

                if (!started)
                {
                    ClearLanSessionData();
                    ConnectionMode = SessionConnectionMode.Online;
                    UIUtilities.Instance.MessagePopUp("No se pudo iniciar el host LAN", true);
                    return;
                }

                lanSessionActive = true;

                localLanPlayerId = Guid.NewGuid().ToString("N");
                lanHostPlayerId = localLanPlayerId;

                ulong localClientId = NetworkManager.Singleton.LocalClientId;

                var localPlayer = CreateLocalLanPlayer(localClientId, true);
                lanPlayers.Add(localPlayer);

                SessionChanged?.Invoke();

#if UNITY_EDITOR
                Debug.Log($"LAN host started. Local LAN Player ID: {localLanPlayerId}, ClientId: {localClientId}");
#endif

                if (IsPracticeMode)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                }
                else
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                ClearLanSessionData();
                ConnectionMode = SessionConnectionMode.Online;

                UIUtilities.Instance.MessagePopUp("No se pudo crear la sesión LAN", true);
            }
        }
        
        public async void JoinLanSessionByIp(string hostIp)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UIUtilities.Instance.MessagePopUp("Ya hay una sesión activa", true);
                return;
            }
            
            IsPracticeMode = false;
            ConnectionMode = SessionConnectionMode.Lan;

            try
            {
                UIUtilities.Instance.MessagePopUp("Uniéndose a sesión LAN...", false);

                ClearLanSessionData();

                if (!ConfigureLanClientTransport(hostIp))
                {
                    ConnectionMode = SessionConnectionMode.Online;
                    UIUtilities.Instance.MessagePopUp("No se pudo configurar la conexión LAN", true);
                    return;
                }

                localLanPlayerId = Guid.NewGuid().ToString("N");

                bool started = NetworkManager.Singleton.StartClient();

                if (!started)
                {
                    ClearLanSessionData();
                    ConnectionMode = SessionConnectionMode.Online;
                    UIUtilities.Instance.MessagePopUp("No se pudo iniciar el cliente LAN", true);
                    return;
                }

#if UNITY_EDITOR
                Debug.Log($"LAN client started. Local LAN Player ID: {localLanPlayerId}. Connecting to {hostIp}:{lanPort}");
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                ClearLanSessionData();
                ConnectionMode = SessionConnectionMode.Online;

                UIUtilities.Instance.MessagePopUp("No se pudo unir a la sesión LAN", true);
            }
        }
        
        public string GetOrCreateLocalLanPlayerName()
            {
                if (!HasLocalPlayerDisplayName)
                    LocalPlayerDisplayName = playerInfo.GetRandomName();

                return LocalPlayerDisplayName;
            }

            public string GetAvailableLanColorName()
            {
                var takenColors = new HashSet<string>();

                foreach (var player in lanPlayers)
                {
                    if (!string.IsNullOrEmpty(player.PlayerColorName))
                        takenColors.Add(player.PlayerColorName);
                }

                foreach (var colorName in playerInfo.GetColorNames())
                {
                    if (!takenColors.Contains(colorName))
                        return colorName;
                }

                return string.Empty;
            }

            public string GetExistingOrAvailableLanColorName(string playerId)
            {
                var existingPlayer = lanPlayers.FirstOrDefault(p => p.PlayerId == playerId);

                if (existingPlayer != null && !string.IsNullOrEmpty(existingPlayer.PlayerColorName))
                    return existingPlayer.PlayerColorName;

                return GetAvailableLanColorName();
            }

            public void RegisterOrUpdateLanPlayer(
                string playerId,
                ulong clientId,
                string playerName,
                string colorName,
                string characterName,
                bool isHost)
            {
                if (!IsLanMode)
                    return;

                if (string.IsNullOrEmpty(playerId))
                    return;

                var player = lanPlayers.FirstOrDefault(p => p.PlayerId == playerId);

                if (player == null)
                {
                    player = new SessionPlayerData();
                    lanPlayers.Add(player);
                }

                player.PlayerId = playerId;
                player.ClientId = clientId;
                player.PlayerName = string.IsNullOrEmpty(playerName) ? "Unnamed" : playerName;
                player.PlayerColorName = colorName;
                player.PlayerColor = playerInfo.GetColorFromName(colorName);
                
                if (!string.IsNullOrEmpty(characterName))
                {
                    player.CharacterName = characterName;
                }
                else if (player.CharacterName == null)
                {
                    player.CharacterName = string.Empty;
                }
                
                player.IsHost = isHost;
                player.IsCurrentPlayer = playerId == localLanPlayerId;

                TryRegisterPlayerClientId(playerId, clientId);

                SessionChanged?.Invoke();
            }
            
            public bool TrySetLanPlayerCharacter(string playerId, string characterName)
            {
                if (!IsLanMode)
                    return false;

                if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(characterName))
                    return false;

                var player = lanPlayers.FirstOrDefault(p => p.PlayerId == playerId);

                if (player == null)
                    return false;

                bool takenByOtherPlayer = lanPlayers.Any(p =>
                    p.PlayerId != playerId &&
                    p.CharacterName == characterName);

                if (takenByOtherPlayer)
                    return false;

                player.CharacterName = characterName;

                PlayerPropertiesChanged?.Invoke();
                SessionChanged?.Invoke();

                return true;
            }
            public bool HaveAllPlayersSelectedCharacters()
            {
                var players = GetSessionPlayers();

                if (players.Count == 0)
                    return false;

                foreach (var player in players)
                {
                    if (string.IsNullOrEmpty(player.CharacterName))
                        return false;
                }

                return true;
            }
            public bool TryGetSessionPlayerByClientId(ulong clientId, out SessionPlayerData sessionPlayer)
            {
                sessionPlayer = null;

                var players = GetSessionPlayers();

                foreach (var player in players)
                {
                    if (player.ClientId != clientId)
                        continue;

                    sessionPlayer = player;
                    return true;
                }

                return false;
            }
            public bool TryClearLanPlayerCharacter(string playerId)
            {
                if (!IsLanMode)
                    return false;

                if (string.IsNullOrEmpty(playerId))
                    return false;

                var player = lanPlayers.FirstOrDefault(p => p.PlayerId == playerId);

                if (player == null)
                    return false;

                player.CharacterName = string.Empty;

                PlayerPropertiesChanged?.Invoke();
                SessionChanged?.Invoke();

                return true;
            }
            
            public void ClearAllLanPlayerCharacters()
            {
                if (!IsLanMode)
                    return;

                foreach (var player in lanPlayers)
                {
                    player.CharacterName = string.Empty;
                }

                PlayerPropertiesChanged?.Invoke();
                SessionChanged?.Invoke();
            }
            private async Task ShutdownNetworkAsync()
            {
                if (NetworkManager.Singleton == null)
                    return;

                if (!NetworkManager.Singleton.IsListening)
                    return;

                NetworkManager.Singleton.Shutdown();

                // Give Netcode a moment to actually finish shutting down.
                int timeoutMs = 3000;
                int elapsedMs = 0;

                while (NetworkManager.Singleton != null &&
                       NetworkManager.Singleton.IsListening &&
                       elapsedMs < timeoutMs)
                {
                    await Task.Delay(100);
                    elapsedMs += 100;
                }

#if UNITY_EDITOR
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    Debug.LogWarning("NetworkManager was still listening after shutdown timeout.");
#endif
            }
            
            private async Task WaitForNetworkShutdownAsync()
            {
                if (NetworkManager.Singleton == null)
                    return;

                int timeoutMs = 3000;
                int elapsedMs = 0;

                while (NetworkManager.Singleton.IsListening && elapsedMs < timeoutMs)
                {
                    await Task.Delay(100);
                    elapsedMs += 100;
                }

#if UNITY_EDITOR
                if (NetworkManager.Singleton.IsListening)
                    Debug.LogWarning("NetworkManager is still listening after leave/shutdown.");
#endif
            }
    }
    
    public enum SessionConnectionMode
    {
        Online,
        Lan
    }
}