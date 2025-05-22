using Code.Helpers.Singleton;
using Code.Systems.PlayerSpawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Systems.Session
{
    public class SessionManager : PersistentSingleton<SessionManager>
    {
        [SerializeField] private Button createSessionBtn;
        [SerializeField] private Button joinSessionBtn;
        [SerializeField] private TMP_InputField sessionCodeInput;
        [SerializeField] private TextMeshProUGUI codeTextUI;
        [SerializeField] private Button startGameButton;
        [SerializeField] private PopMessages popMessages;
        private UIElements uiElements;

        public event Action SessionUpdated;

        public readonly string playerNamePropertyKey = "playerName";
        public readonly string playerColorPropertyKey = "playerColor";
        public readonly string playerCharacterPropertyKey = "playerCharacter";
        public readonly string startGamePropertyKey = "startGame";
        public readonly string playerAuthIdPropertyKey = "playerAuthId";

        public Dictionary<string, ulong> playerIdToClientId = new();

        public readonly Dictionary<string, Color> colors = new Dictionary<string, Color>
        {
            { "blue", Color.blue },
            { "red", Color.red },
            { "yellow", Color.yellow },
            { "green", Color.green }
        };

        private ISession activeSession;
        public ISession ActiveSession
        {
            get => activeSession;
            set
            {
                activeSession = value;
                Debug.Log($"Active session: {activeSession}");
            }
        }

        protected override void Awake()
        {
            base.Awake();
            //popMessages = FindFirstObjectByType<PopMessages>();
            uiElements = FindFirstObjectByType<UIElements>();
        }
        private async void Start()
        {
            // Initialize unity services and sign in player anonoymously
            await InitializeServices();

            createSessionBtn.onClick.AddListener(StartSessionAsHost);
            joinSessionBtn.onClick.AddListener(JoinSessionByCode);
            startGameButton.onClick.AddListener(OnStartGamePressed);
        }
        private void OnDisable()
        {
            if (ActiveSession != null)
                ActiveSession.Changed -= OnSessionChange;
            //createSessionBtn.onClick.RemoveListener(StartSessionAsHost);
            //joinSessionBtn.onClick.RemoveListener(JoinSessionByCode);
            //startGameButton.onClick.RemoveListener(OnStartGamePressed);
        }

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
                popMessages.PopMessage("No se pudo conectar a los servicios online", true);
            }
        }
        private async void StartSessionAsHost()
        {
            try
            {
                popMessages.PopMessage("Creando la sesión...", false);

                // Set session options
                var options = new SessionOptions
                {
                    MaxPlayers = 2,
                    IsPrivate = true,
                }.WithRelayNetwork();

                // Create the session and makes the player the host
                ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");

                await OnCreateOrJoin();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                popMessages.PopMessage("No se pudo crear la sesión", true);
            }
        }
        private async void JoinSessionByCode()
        {
            try
            {
                popMessages.PopMessage("Uniéndose a la sesión...", false);

                // Join the player to the session by code
                ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCodeInput.text);
                Debug.Log($"Session {ActiveSession.Id} joined");

                await OnCreateOrJoin();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                popMessages.PopMessage("No se pudo unir a la sesión", true);
            }
        }
        public async void LeaveSession()
        {
            if (ActiveSession != null)
            {
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
        }

        // Do on creating or joining a session
        private async Task OnCreateOrJoin()
        {
            // Update session and player info
            codeTextUI.text = ActiveSession.Code;

            await SetPlayerProperties();
            uiElements.PopDown(popMessages.gameObject);
            uiElements.PopDown(GameObject.Find("PreLobby"));

            // Update UI info
            ActiveSession.Changed += OnSessionChange;
        }
        private string GetAvailableColorName()
        {
            var takenColors = new HashSet<string>();

            if (ActiveSession != null)
            {
                foreach (var player in ActiveSession.Players)
                {
                    if (player.Properties.TryGetValue(playerColorPropertyKey, out var prop))
                        takenColors.Add(prop.Value);
                }
            }

            foreach (var colorName in colors.Keys)
            {
                if (!takenColors.Contains(colorName))
                    return colorName;
            }

            return "";
        }
        // Set name, unique color and null character
        private async Task SetPlayerProperties()
        {
            if (ActiveSession == null) return;

            // Set authentication ID as a player property
            ActiveSession.CurrentPlayer.SetProperty(playerAuthIdPropertyKey, new PlayerProperty(AuthenticationService.Instance.PlayerId, VisibilityPropertyOptions.Member));

            var colorName = GetAvailableColorName();
            ActiveSession.CurrentPlayer.SetProperty(playerColorPropertyKey, new PlayerProperty(colorName, VisibilityPropertyOptions.Member));
            ActiveSession.CurrentPlayer.SetProperty(playerCharacterPropertyKey, new PlayerProperty("None", VisibilityPropertyOptions.Member));

            await ActiveSession.SaveCurrentPlayerDataAsync();
        }

        private void OnSessionChange()
        {
            if (ActiveSession != null)
            {
                SessionUpdated?.Invoke();
                if (ActiveSession.IsHost && ActiveSession.PlayerCount > 1)
                    startGameButton.interactable = AllPlayersHaveSelectedCharacters();
                if (ActiveSession.Properties.TryGetValue(startGamePropertyKey, out var startProp))
                {
                    if (startProp.Value == "true")
                    {
                        uiElements.FadeIn(uiElements.gameObject, uiElements.transitionDuration);
                        if (ActiveSession.IsHost)
                            NetworkManager.Singleton.SceneManager.LoadScene("GameplayTest1", UnityEngine.SceneManagement.LoadSceneMode.Single);
                    }
                }
            }
        }
        private bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in ActiveSession.Players)
            {
                if (!player.Properties.TryGetValue(playerCharacterPropertyKey, out var charProp) || string.IsNullOrEmpty(charProp.Value) || charProp.Value == "None")
                    return false;
            }
            return true;
        }
        private async void OnStartGamePressed()
        {
            if (!ActiveSession.IsHost) return;

            ActiveSession.AsHost().SetProperty(startGamePropertyKey, new SessionProperty("true", VisibilityPropertyOptions.Member));
            await ActiveSession.AsHost().SavePropertiesAsync();
        }
        public async Task<bool> TrySelectCharacter(string characterName)
        {
            bool isTaken = ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(playerCharacterPropertyKey, out var prop) &&
                prop.Value == characterName);

            if (isTaken)
                return false;

            ActiveSession.CurrentPlayer.SetProperty(playerCharacterPropertyKey, new PlayerProperty(characterName, VisibilityPropertyOptions.Member));
            await ActiveSession.SaveCurrentPlayerDataAsync();
            return true;
        }
    }
}