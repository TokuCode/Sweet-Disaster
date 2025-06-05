using Code.Helpers.Singleton;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Code.Helpers.UI;
using System.Linq;

namespace Code.Networking.Session
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
        Map,
        Winner
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
        
        private readonly Dictionary<Color, string> playerColorToTag = new()
        {
            { Color.blue, "P1" },
            { Color.red, "P2" },
            { Color.yellow, "P3" },
            { Color.green, "P4" }
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
            { SessionPropertyKeys.Map, "Map" },
            { SessionPropertyKeys.Winner, "winner" }
        };

        private readonly string[] randomNames =
        {
            "PanConWifi",
            "TioPapita",
            "LagMan3000",
            "CucharaNinja",
            "Tiramisu",
            "SinManaNiGloria",
            "DonCeviche",
            "PatitoDeFuego",
            "ElTamalAsesino",
            "Albondigón3000",
            "ChispaDeTuna",
            "CalabazaEspía",
            "SeñorTaco"
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
        
        public IReadOnlyDictionary<Color, string> PlayerColorToTag => playerColorToTag;

        public IReadOnlyDictionary<PlayerPropertyKeys, string> PlayerKeys => playerKeys;

        public IReadOnlyDictionary<SessionPropertyKeys, string> SessionKeys => sessionKeys;

        public event Action ActiveSessionAvailable;
        //public event Action MatchEnded;

        private async void Start() =>
            await InitializeServices(); // Initialize unity services and sign in player anonymously

        private void OnDisable() => Destroy(gameObject);
        
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

                // Set session options
                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = true,
                }.WithRelayNetwork();

                ActiveSession =
                    await MultiplayerService.Instance.CreateSessionAsync(options); // Create the session as host
                Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");

                await OnCreateOrJoin();
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
                UIUtilities.Instance.MessagePopUp("Uniéndose a la sesión...", false);

                ActiveSession =
                    await MultiplayerService.Instance
                        .JoinSessionByCodeAsync(code); // Join the player to the session by code
                Debug.Log($"Session {ActiveSession.Id} joined");

                await OnCreateOrJoin();
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

        private async Task OnCreateOrJoin()
        {
            await SetPlayerProperties();
            ActiveSession.Changed += OnGameHasBeenStarted;
            ActiveSessionAvailable?.Invoke();
        }
        
        public async void NotifyGameIsSetToStart()
        {
            if (ActiveSession == null) return;
            
            ActiveSession.AsHost().SetProperty(SessionKeys[SessionPropertyKeys.PlayersReady], new SessionProperty("true", VisibilityPropertyOptions.Member));
            await ActiveSession.AsHost().SavePropertiesAsync();
        }

        private void OnGameHasBeenStarted()
        {
            if (ActiveSession == null) return;
            if (ActiveSession.Properties.TryGetValue(SessionKeys[SessionPropertyKeys.PlayersReady], out var value))
            {
                if (value.Value == "true")
                {
                    UIUtilities.Instance.FadeIn(UIUtilities.Instance.TransitionPanel, UIUtilities.Instance.TransitionDuration);
                    if (ActiveSession.IsHost)
                        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
            }
        }

        private async Task SetPlayerProperties()
        {
            if (ActiveSession == null) return;

            var colorName = GetAvailableColorName();
            
            ActiveSession.CurrentPlayer.SetProperties(new Dictionary<string, PlayerProperty>
            {
                {
                    PlayerKeys[PlayerPropertyKeys.PlayerName],
                    new PlayerProperty(GetRandomName(), VisibilityPropertyOptions.Member)
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

        private string GetRandomName()
        {
            var takenNames = new HashSet<string>();

            if (ActiveSession != null)
            {
                foreach (var player in ActiveSession.Players)
                {
                    if (player.Properties.TryGetValue(PlayerKeys[PlayerPropertyKeys.PlayerName], out var prop))
                        takenNames.Add(prop.Value);
                }
            }
            
            if (takenNames.Count >= randomNames.Length)
                return "Unnamed";

            string candidate;
            do
            {
                candidate = randomNames[UnityEngine.Random.Range(0, randomNames.Length)];
            } 
            while (takenNames.Contains(candidate));

            return candidate;
        }
        
        public async Task<bool> TrySelectCharacter(string characterName)
        {
            bool isTaken = ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(playerKeys[PlayerPropertyKeys.PlayerCharacter], out var prop) &&
                prop.Value == characterName);

            if (isTaken) return false;

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