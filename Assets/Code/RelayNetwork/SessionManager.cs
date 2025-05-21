using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class SessionManager : PersistentSingleton<SessionManager>
{
    [SerializeField] private Button createSessionBtn;
    [SerializeField] private Button joinSessionBtn;
    [SerializeField] private TMP_InputField sessionCodeInput;
    [SerializeField] private TextMeshProUGUI codeTextUI;
    [SerializeField] private LobbyUIManager lobbyUI;
    [SerializeField] private Button startGameButton;

    public readonly string playerColorPropertyKey = "playerColor";
    public readonly string playerCharacterPropertyKey = "playerCharacter";

    public Dictionary<ulong, string> clientIdToPlayerId = new();

    private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>
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

    async void Start()
    {
        // Initialize unity services and sign in player anonoymously
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in anonymously, PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        createSessionBtn.onClick.AddListener(StartSessionAsHost);
        joinSessionBtn.onClick.AddListener(JoinSessionByCode);
        startGameButton.onClick.AddListener(OnStartGamePressed);
    }

    private void OnDisable()
    {
        ActiveSession.Changed -= OnSessionChange;
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

    private void OnSessionChange()
    {
        if (ActiveSession != null)
        {
            lobbyUI.RefreshPlayerList(ActiveSession.Players.ToList(), colors);
            lobbyUI.RefreshCharacterSelectionUI(ActiveSession.Players.ToList(), colors);
            if (ActiveSession.IsHost && ActiveSession.PlayerCount > 1)
                startGameButton.interactable = AllPlayersHaveSelectedCharacters();
            if (ActiveSession.Properties.TryGetValue("startGame", out var startProp))
            {
                if (startProp.Value == "true")
                    FindFirstObjectByType<UIElements>().LoadScene("GameplayTest");
            }
        }
    }

    async void StartSessionAsHost()
    {
        // Set session options
        var options = new SessionOptions
        {
            MaxPlayers = 4,
            IsPrivate = true,
        }.WithRelayNetwork();

        // Create the session and makes the player the host
        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);

        codeTextUI.text = ActiveSession.Code;

        var colorName = GetAvailableColorName();
        ActiveSession.CurrentPlayer.SetProperty(playerColorPropertyKey, new PlayerProperty(colorName, VisibilityPropertyOptions.Member));
        ActiveSession.CurrentPlayer.SetProperty(playerCharacterPropertyKey, new PlayerProperty("None", VisibilityPropertyOptions.Member));
        await ActiveSession.SaveCurrentPlayerDataAsync();

        var localClientId = NetworkManager.Singleton.LocalClientId;
        var unityPlayerId = ActiveSession.CurrentPlayer.Id;

        clientIdToPlayerId[localClientId] = unityPlayerId;

        Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
        ActiveSession.Changed += OnSessionChange;
    }

    async void JoinSessionByCode()
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCodeInput.text);
        
        codeTextUI.text = ActiveSession.Code;
        Debug.Log($"Session {ActiveSession.Id} joined");

        var colorName = GetAvailableColorName();
        ActiveSession.CurrentPlayer.SetProperty(playerColorPropertyKey, new PlayerProperty(colorName, VisibilityPropertyOptions.Member));
        ActiveSession.CurrentPlayer.SetProperty(playerCharacterPropertyKey, new PlayerProperty("None", VisibilityPropertyOptions.Member));
        await ActiveSession.SaveCurrentPlayerDataAsync();

        var localClientId = NetworkManager.Singleton.LocalClientId;
        var unityPlayerId = ActiveSession.CurrentPlayer.Id;

        clientIdToPlayerId[localClientId] = unityPlayerId;

        ActiveSession.Changed += OnSessionChange;
    }

    async Task LeaveSession()
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

        ActiveSession.AsHost().SetProperty("startGame", new SessionProperty("true", VisibilityPropertyOptions.Member));
        await ActiveSession.AsHost().SavePropertiesAsync();
    }
}