using System;
using UnityEngine;
using UnityEngine.UI;
using Code.Networking.Session;
using TMPro;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyCreationUI : MonoBehaviour
    {
        private const string LanModePrefKey = "LobbyCreation_LanMode";
        private const string LastJoinValuePrefKey = "LobbyCreation_LastJoinValue";
        
        [Header("Buttons")]
        [SerializeField] private Button createSessionButton;
        [SerializeField] private Button joinSessionButton;
        [SerializeField] private Button practiceModeSessionButton;
        [SerializeField] private Button applyNameButton;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private TMP_InputField nameInputField;

        [Header("LAN / Online")]
        [SerializeField] private Toggle lanModeToggle;
        [SerializeField] private TMP_Text joinInputLabel;

        private void Awake()
        {
            practiceModeSessionButton.onClick.AddListener(StartPractice);
            createSessionButton.onClick.AddListener(CreateSession);
            joinSessionButton.onClick.AddListener(JoinSession);
            applyNameButton.onClick.AddListener(ApplyName);

            if (lanModeToggle != null)
                lanModeToggle.onValueChanged.AddListener(OnLanModeChanged);
        }

        private void Start()
        {
            nameInputField.text = SessionManager.Instance.LocalPlayerDisplayName;

            if (lanModeToggle != null)
                lanModeToggle.isOn = PlayerPrefs.GetInt(LanModePrefKey, 0) == 1;

            if (codeInputField != null)
                codeInputField.text = PlayerPrefs.GetString(LastJoinValuePrefKey, string.Empty);

            UpdateModeVisuals(false);
            UpdateButtonState();
        }

        private void Update()
        {
            UpdateButtonState();
        }

        private void CreateSession()
        {
            if (lanModeToggle != null && lanModeToggle.isOn)
            {
                SessionManager.Instance.StartLanSessionAsHost(false);
            }
            else
            {
                SessionManager.Instance.StartSessionAsHost(false);
            }
        }

        private void JoinSession()
        {
            string joinValue = codeInputField.text.Trim();

            PlayerPrefs.SetString(LastJoinValuePrefKey, joinValue);
            PlayerPrefs.Save();

            if (lanModeToggle != null && lanModeToggle.isOn)
            {
                SessionManager.Instance.JoinLanSessionByIp(joinValue);
            }
            else
            {
                SessionManager.Instance.JoinSessionByCode(joinValue);
            }
        }

        private void StartPractice()
        {
            if (lanModeToggle != null && lanModeToggle.isOn)
            {
                SessionManager.Instance.StartLanSessionAsHost(true);
            }
            else
            {
                SessionManager.Instance.StartSessionAsHost(true);
            }
        }

        private void ApplyName()
        {
            SessionManager.Instance.LocalPlayerDisplayName = nameInputField.text.Trim();
        }

        private void OnLanModeChanged(bool isLan)
        {
            PlayerPrefs.SetInt(LanModePrefKey, isLan ? 1 : 0);
            PlayerPrefs.Save();

            UpdateModeVisuals();
            UpdateButtonState();
        }

        private void UpdateModeVisuals(bool clearJoinInput = true)
        {
            bool isLan = lanModeToggle != null && lanModeToggle.isOn;

            if (joinInputLabel != null)
                joinInputLabel.text = isLan ? "Host IP" : "Join Code";

            if (clearJoinInput && codeInputField != null)
                codeInputField.text = string.Empty;

            if (codeInputField != null && codeInputField.placeholder is TMP_Text placeholder)
                placeholder.text = isLan ? "Example: 192.168.10.1" : "Enter join code";
        }

        private void UpdateButtonState()
        {
            bool hasName = SessionManager.Instance.HasLocalPlayerDisplayName;
            bool hasJoinValue = !string.IsNullOrWhiteSpace(codeInputField.text);

            createSessionButton.interactable = hasName;
            //practiceModeSessionButton.interactable = hasName;
            joinSessionButton.interactable = hasName && hasJoinValue;
        }

        private void OnDisable()
        {
            practiceModeSessionButton.onClick.RemoveListener(StartPractice);
            createSessionButton.onClick.RemoveListener(CreateSession);
            joinSessionButton.onClick.RemoveListener(JoinSession);
            applyNameButton.onClick.RemoveListener(ApplyName);

            if (lanModeToggle != null)
                lanModeToggle.onValueChanged.RemoveListener(OnLanModeChanged);
        }
    }
}