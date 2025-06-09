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

namespace Code.Networking.Session
{
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        private ISession _activeSession;
        [SerializeField] private int maxPlayers = 2;
        private Dictionary<string, ulong> playerIdToClientId = new();
        
        public ISession ActiveSession
        {
            get => _activeSession;
            set
            {
                _activeSession = value;
                Debug.Log($"Active session: {_activeSession}");
            }
        }

        public PlayerInfo playerInfo;
        
        // Session properties keys
        public readonly string MapPropertyKey = "map";
        public readonly string WinnerPropertyKey = "winner";
        public readonly string RestartGame = "restartKey";
        
        // Player properties keys
        public readonly string PlayerNameKey = "playerName";
        public readonly string PlayerColorKey = "playerColor";
        public readonly string PlayerCharacterKey = "playerCharacter";
        public readonly string PlayerReadyToRestart = "playerReadyToRestart";
        
        public Dictionary<string, ulong> PlayerIdToClientId => playerIdToClientId;

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
        
        public async void StartSessionAsHost()
        {
            try
            {
                UIUtilities.Instance.MessagePopUp("Creando la sesión...", false);

                var options = new SessionOptions
                {
                    MaxPlayers = maxPlayers,
                    IsPrivate = true,
                
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { RestartGame, new SessionProperty("false", VisibilityPropertyOptions.Member) },
                        { WinnerPropertyKey, new SessionProperty("None", VisibilityPropertyOptions.Member) },
                        { MapPropertyKey, new SessionProperty("default", VisibilityPropertyOptions.Member) }
                    },
                    
                    PlayerProperties = GetPlayerProperties()
                }.WithRelayNetwork();
                
                ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options); // Create the session as host
                Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");

                if (!ActiveSession.IsHost) return;
                NetworkManager.Singleton.SceneManager.LoadScene("LobbyTest", LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("No se pudo crear la sesión", true);
            }
        }
        
        public async void JoinSessionByCode(string code)
        {
            try
            {
                UIUtilities.Instance.MessagePopUp("Uniéndose a la sesión...", false); ;

                ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code); // Join the player to the session by code
                
                ActiveSession.CurrentPlayer.SetProperties(GetPlayerProperties());
                await ActiveSession.SaveCurrentPlayerDataAsync();
                
                Debug.Log($"Session {ActiveSession.Id} joined");
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

        private Dictionary<string, PlayerProperty> GetPlayerProperties()
        {
            return new Dictionary<string, PlayerProperty>
            {
                { PlayerNameKey, new PlayerProperty(playerInfo.GetRandomName(), VisibilityPropertyOptions.Member) },
                { PlayerColorKey, new PlayerProperty(playerInfo.GetAvailableColorName(), VisibilityPropertyOptions.Member) },
                { PlayerCharacterKey, new PlayerProperty("None", VisibilityPropertyOptions.Member) },
                { PlayerReadyToRestart, new PlayerProperty("false", VisibilityPropertyOptions.Member) }
            };
        }
    }
}