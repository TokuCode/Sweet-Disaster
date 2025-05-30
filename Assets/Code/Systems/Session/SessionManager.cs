using Code.Helpers.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Code.Systems.Session
{
    // Enums
    public enum PlayerPropertyKeys
    {
        PlayerName,
        PlayerColor,
        PlayerCharacter
    }

    public enum SessionPropertyKeys
    {
        PlayersReady,
        Map
    }
    
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        // Private members
        private ISession activeSession;
        private Dictionary<string, ulong> playerIdToClientId = new();
        private readonly Dictionary<string, Color> playerColors = new()
        {
            { "blue", Color.blue },
            { "red", Color.red },
            { "yellow", Color.yellow },
            { "green", Color.green }
        };
        private readonly Dictionary<PlayerPropertyKeys, string> playerKeys = new()
        {
            { PlayerPropertyKeys.PlayerName, "playerName" },
            { PlayerPropertyKeys.PlayerColor, "playerColor" },
            { PlayerPropertyKeys.PlayerCharacter, "playerCharacter" }
        };
        private readonly Dictionary<SessionPropertyKeys, string> sessionKeys = new()
        {
            { SessionPropertyKeys.PlayersReady, "playersReady" },
            { SessionPropertyKeys.Map, "Map" }
        };
        
        // Public members
        public ISession ActiveSession
        {
            get => activeSession;
            set
            {
                activeSession = value;
                Debug.Log($"Active session: {activeSession}");
            }
        }
        
        public Dictionary<string, ulong> PlayerIdToClientId => playerIdToClientId;
        
        public IReadOnlyDictionary<string, Color> PlayerColors => playerColors;
        
        public IReadOnlyDictionary<PlayerPropertyKeys, string> PlayerKeys => playerKeys;
        
        public IReadOnlyDictionary<SessionPropertyKeys, string> SessionKeys => sessionKeys;
        
        public event Action ActiveSessionAvailable;

        private async void Start() => await InitializeServices(); // Initialize unity services and sign in player anonymously
        
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
                UITransitionManager.Instance.MessagePopUp("No se pudo conectar a los servicios online", true);
            }
        }

        public async void StartSessionAsHost()
        {
            try
            {
                UITransitionManager.Instance.MessagePopUp("Creando la sesión...", false);

                // Set session options
                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = true,
                }.WithRelayNetwork();
                
                ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options); // Create the session as host
                Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");

                await OnCreateOrJoin();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UITransitionManager.Instance.MessagePopUp("No se pudo crear la sesión", true);
            }
        }
        
        public async void JoinSessionByCode(string code)
        {
            try
            {
                UITransitionManager.Instance.MessagePopUp("Uniéndose a la sesión...", false);
                
                ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code); // Join the player to the session by code
                Debug.Log($"Session {ActiveSession.Id} joined");

                await OnCreateOrJoin();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UITransitionManager.Instance.MessagePopUp("No se pudo unir a la sesión", true);
            }
        }
        
        public async void LeaveSession()
        {
            if (ActiveSession == null) return;
            try
            {
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

        private async Task OnCreateOrJoin()
        {
            await SetPlayerProperties();
            ActiveSessionAvailable?.Invoke();
        }
        
        public void OnStartGamePressed()
        {
            if (ActiveSession == null) return;
            UITransitionManager.Instance.FadeIn(UITransitionManager.Instance.gameObject, UITransitionManager.Instance.transitionDuration);
            if (ActiveSession.IsHost)
                NetworkManager.Singleton.SceneManager.LoadScene("Gameplay-Test", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private async Task SetPlayerProperties()
        {
            if (ActiveSession == null) return;

            var colorName = GetAvailableColorName();
            
            ActiveSession.CurrentPlayer.SetProperties(new Dictionary<string, PlayerProperty>
            {
                {
                    PlayerKeys[PlayerPropertyKeys.PlayerName],
                    new PlayerProperty("Player", VisibilityPropertyOptions.Member)
                },
                {
                    PlayerKeys[PlayerPropertyKeys.PlayerColor],
                    new PlayerProperty(colorName, VisibilityPropertyOptions.Member)
                },
                {
                    PlayerKeys[PlayerPropertyKeys.PlayerCharacter],
                    new PlayerProperty("None", VisibilityPropertyOptions.Member)
                }
            });
            
            await ActiveSession.SaveCurrentPlayerDataAsync();
        }

        private string GetAvailableColorName()
        {
            var takenColors = new HashSet<string>();

            if (ActiveSession != null)
            {
                foreach (var player in ActiveSession.Players)
                {
                    if (player.Properties.TryGetValue(PlayerKeys[PlayerPropertyKeys.PlayerColor], out var prop))
                        takenColors.Add(prop.Value);
                }
            }
            foreach (var colorName in PlayerColors.Keys)
            {
                if (!takenColors.Contains(colorName))
                    return colorName;
            }
            return String.Empty;
        }
        
        public async Task<bool> TrySelectCharacter(string characterName)
        {
            bool isTaken = ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerCharacter], out var prop) &&
                prop.Value == characterName);

            if (isTaken)
                return false;

            ActiveSession.CurrentPlayer.SetProperty(playerKeys[PlayerPropertyKeys.PlayerCharacter], new PlayerProperty(characterName, VisibilityPropertyOptions.Member));
            await ActiveSession.SaveCurrentPlayerDataAsync();
            return true;
        }
        
        public bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in ActiveSession.Players)
            {
                if (!player.Properties.TryGetValue(PlayerKeys[PlayerPropertyKeys.PlayerCharacter], out var charProp) || 
                    string.IsNullOrEmpty(charProp.Value) || charProp.Value == "None")
                    return false;
            }
            return true;
        }
    }
}