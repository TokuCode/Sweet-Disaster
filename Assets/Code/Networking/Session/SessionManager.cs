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
                _activeSession = value;
#if UNITY_EDITOR
                Debug.Log($"Active session: {_activeSession}");
#endif
            }
        }

        public PlayerInfo playerInfo;
        public bool IsPracticeMode { get; private set; }
        public bool ShouldRetrievePing { get; private set; } = true;
        
        // Session properties keys
        public readonly string MapPropertyKey = "map";
        
        // Player properties keys
        public readonly string PlayerNameKey = "playerName";
        public readonly string PlayerColorKey = "playerColor";
        public readonly string PlayerCharacterKey = "playerCharacter";
        
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
        
        public async void StartSessionAsHost(bool isPracticeMode)
        {
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
    }
}