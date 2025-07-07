using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;
using SessionManager = Code.Networking.Session.SessionManager;
using System.Threading.Tasks;
using Code.Helpers.UI;
using Code.Helpers.Singleton;
using System;
using System.Collections;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyUIManager : Singleton<LobbyUIManager>
    {
        [Header("Refreshables")] 
        [SerializeField] private List<Refreshable> refreshables = new();

        [Header("Lobby general")]
        [SerializeField] private TextMeshProUGUI codeText;

        [SerializeField] private Button backButton;
        [SerializeField] private Button nextMapButton;
        [SerializeField] private Button prevMapButton;

        [Header("Maps")] 
        [SerializeField] private List<string> mapNames;
        [SerializeField] private Image mapImage;
        private int _currentMapIndex;

        private SessionManager _sessionManager;

        private bool _canSelectCharacter = true;
        [SerializeField] private float buttonCooldown;

        protected override void Awake()
        {
            base.Awake();
            
            if (SessionManager.Instance == null) return;
            _sessionManager = SessionManager.Instance;
            
            // Buttons
            backButton.onClick.AddListener(_sessionManager.LeaveSession);
            backButton.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
            nextMapButton.onClick.AddListener(() => ChangeMap(1));
            prevMapButton.onClick.AddListener(() => ChangeMap(-1));

            //nextMapButton.interactable = _sessionManager.ActiveSession.IsHost;
            //prevMapButton.interactable = _sessionManager.ActiveSession.IsHost;
        }

        private void Start()
        {
            // Set code
            codeText.text = _sessionManager.ActiveSession.Code;
            
            // Build the lobby
            RefreshLobbyElements();
            
            // Listen to changes to refresh the player list, character selection UI, and start game button
            _sessionManager.ActiveSession.Changed += RefreshLobbyElements;
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveAllListeners();

            if (_sessionManager.ActiveSession == null) return;
            _sessionManager.ActiveSession.Changed -= RefreshLobbyElements;
        }
        
        private void RefreshLobbyElements()
        {
            foreach (var refreshable in refreshables)
                refreshable.Refresh();
        }

        private async void ChangeMap(float buttonDir)
        {
            if (!_sessionManager.ActiveSession.IsHost) return;
            
            if (buttonDir == 0) return;

            buttonDir = Mathf.Sign(buttonDir);

            _currentMapIndex = (_currentMapIndex + (int)buttonDir + mapNames.Count) % mapNames.Count;

            try
            {
                _sessionManager.ActiveSession.AsHost().SetProperty(_sessionManager.MapPropertyKey,
                    new SessionProperty(mapNames[_currentMapIndex], VisibilityPropertyOptions.Member));

                await _sessionManager.ActiveSession.AsHost().SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIUtilities.Instance.MessagePopUp("Hubo un problema guardando las propiedades", true);
            }
        }
        
        public bool AllPlayersHaveSelectedCharacters()
        {
            foreach (var player in _sessionManager.ActiveSession.Players)
            {
                if (string.IsNullOrEmpty(_sessionManager.playerInfo.GetPropertyValue(player, _sessionManager.PlayerCharacterKey)))
                    return false;
            }
            return true;
        }
        
        private async Task<bool> TrySelectCharacter(string characterName)
        {
            _canSelectCharacter = false;
            
            bool isTaken = _sessionManager.ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var prop) &&
                prop.Value == characterName);

            if (isTaken) return false;

            try
            {
                _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerCharacterKey,
                    new PlayerProperty(characterName, VisibilityPropertyOptions.Member));
                await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();

                StartCoroutine(WaitForCooldown());
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
                UIUtilities.Instance.MessagePopUp("Hubo un problema guardando las propiedades del jugador", true);
                StartCoroutine(WaitForCooldown());
                return false;
            }
        }

        private IEnumerator WaitForCooldown()
        {
            yield return new WaitForSeconds(buttonCooldown);
            _canSelectCharacter = true;
        }
        
        public async void OnCharacterSelected(CharacterButtonUI character)
        {
            if (!_canSelectCharacter) return;
            bool success = await TrySelectCharacter(character.characterName);
            
#if UNITY_EDITOR
            if (!success) Debug.Log("Character already taken");
            else Debug.Log($"My Character: {character.characterName}");
#endif
        }
        
        public void CopyToClipboard() => GUIUtility.systemCopyBuffer = codeText.text;
    }
}