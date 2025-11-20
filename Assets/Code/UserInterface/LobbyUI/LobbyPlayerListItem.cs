using Code.Gameplay;
using Code.Networking.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using System;
using Code.Helpers.UI;
using System.Linq;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyPlayerListItem : NetworkBehaviour
    {
        private SessionManager _sessionManager;
        
        // Player info
        public string playerId;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image outlineColor;
        
        // Characters
        [SerializeField] private List<CharacterScriptable> characters;
        [SerializeField] private Button lockButton;
        private bool _characterLocked;
        
        // Splash image
        [SerializeField] private Image splash;
        [SerializeField] private TMP_Text characterName;
        private int _currentCharacterIndex;
        private int _currentSkinIndex;
        
        public NetworkVariable<int> CharacterIndex = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<int> SkinIndex = new(writePerm: NetworkVariableWritePermission.Server);
        
        // Swap buttons
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        
        public void Set(string id, string playerName, Color color)
        {
            playerId = id;
            nameText.text = playerName;
            outlineColor.color = color;

            if (playerId != SessionManager.Instance.ActiveSession.CurrentPlayer.Id) return;
            
            nextButton.gameObject.SetActive(true);
            prevButton.gameObject.SetActive(true);
            if (_characterLocked) return;
            lockButton.gameObject.SetActive(true);
        }

        public void ResetItem()
        {
            playerId = null;
            nameText.text = "+";
            outlineColor.color = Color.white;
            
            nextButton.gameObject.SetActive(false);
            prevButton.gameObject.SetActive(false);
            lockButton.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (SessionManager.Instance == null) return;
            _sessionManager = SessionManager.Instance;
            
            lockButton.onClick.AddListener(LockCharacter);
            nextButton.onClick.AddListener(() => ChangeCharacter(1));
            prevButton.onClick.AddListener(() => ChangeCharacter(-1));
            
            splash.sprite = characters[0].skinsArray[0].lobbySplashImage;
            splash.preserveAspect = true;
            characterName.text = characters[0].skinsArray[0].skinName;
            
            CharacterIndex.OnValueChanged += (oldVal, newVal) => UpdateCharacterUI(newVal, SkinIndex.Value);
            SkinIndex.OnValueChanged += (oldVal, newVal) => UpdateCharacterUI(CharacterIndex.Value, newVal);
            // Apply immediately on join
            UpdateCharacterUI(CharacterIndex.Value, SkinIndex.Value);
            
            _sessionManager.ActiveSession.PlayerPropertiesChanged += CheckCharacterAvailability;
            _sessionManager.ActiveSession.PlayerLeaving += OnPlayerLeft;
        }

        public override void OnNetworkDespawn()
        {
            if (_sessionManager.ActiveSession == null) return;
            _sessionManager.ActiveSession.PlayerPropertiesChanged -= CheckCharacterAvailability;
            _sessionManager.ActiveSession.PlayerLeaving -= OnPlayerLeft;
        }

        private void OnPlayerLeft(string id)
        {
            if (playerId == id)
            {
                SetAlpha(.8f);
                SetAlphaRpc(.8f);
            }
        }

        private void ChangeCharacter(int direction)
        {
            if (!_characterLocked)
            {
                _currentCharacterIndex += direction;

                if (_currentCharacterIndex >= characters.Count)
                    _currentCharacterIndex = 0;
                else if (_currentCharacterIndex < 0)
                    _currentCharacterIndex = characters.Count - 1;

                _currentSkinIndex = 0;
                CheckCharacterAvailability();
            }
            else
            {
                var skins = characters[_currentCharacterIndex].skinsArray;
                _currentSkinIndex += direction;

                if (_currentSkinIndex >= skins.Length)
                    _currentSkinIndex = 0;
                else if (_currentSkinIndex < 0)
                    _currentSkinIndex = skins.Length - 1;
            }
            
            UpdateCharacterUI(_currentCharacterIndex, _currentSkinIndex);
            RequestChangeCharacterRpc(_currentCharacterIndex, _currentSkinIndex);
            //UpdateCharacterUIRpc(_currentCharacterIndex, _currentSkinIndex);
        }
        
        private void UpdateCharacterUI(int charIndex, int skinIndex)
        {
            var character = characters[charIndex];
            var skin = character.skinsArray[skinIndex];

            splash.sprite = skin.lobbySplashImage;
            splash.preserveAspect = true;
            characterName.text = skin.skinName;
        }
        
        [Rpc(SendTo.Server)]
        private void RequestChangeCharacterRpc(int charIndex, int skinIndex)
        {
            CharacterIndex.Value = charIndex;
            SkinIndex.Value = skinIndex;
        }

        [Rpc(SendTo.NotMe)]
        private void UpdateCharacterUIRpc(int charIndex, int skinIndex, RpcParams rpcParams = default)
        {
            UpdateCharacterUI(charIndex, skinIndex);
        }

        public void CheckCharacterAvailability()
        {
            bool isTaken = _sessionManager.ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var prop) &&
                prop.Value == characters[_currentCharacterIndex].characterName);

            lockButton.interactable = !isTaken;
        }
        
        private async void LockCharacter()
        {
            bool success = await TrySelectCharacter(characters[_currentCharacterIndex].characterName);
            
            if (!success) Debug.Log("Character already taken");
            else
            {
                _characterLocked = true;
                Debug.Log($"My Character: {characters[_currentCharacterIndex].characterName}");
                lockButton.gameObject.SetActive(false);

                SetAlpha(1);
                SetAlphaRpc(1);
            }
        }

        private void SetAlpha(float alpha)
        {
            Color c = splash.color;
            c.a = Mathf.Clamp01(alpha);
            splash.color = c;
        }

        [Rpc(SendTo.NotMe)]
        private void SetAlphaRpc(float alpha)
        {
            SetAlpha(alpha);
        }
        
        private async Task<bool> TrySelectCharacter(string characterName)
        {
            bool isTaken = _sessionManager.ActiveSession.Players.Any(p =>
                p.Properties.TryGetValue(_sessionManager.PlayerCharacterKey, out var prop) &&
                prop.Value == characterName);

            if (isTaken) return false;

            try
            {
                _sessionManager.ActiveSession.CurrentPlayer.SetProperty(_sessionManager.PlayerCharacterKey,
                    new PlayerProperty(characterName, VisibilityPropertyOptions.Member));
                await _sessionManager.ActiveSession.SaveCurrentPlayerDataAsync();
                
                //StartCoroutine(WaitForCooldown());
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
                UIUtilities.Instance.MessagePopUp("Hubo un problema guardando las propiedades del jugador", true);
                //StartCoroutine(WaitForCooldown());
                return false;
            }
        }
    }
}