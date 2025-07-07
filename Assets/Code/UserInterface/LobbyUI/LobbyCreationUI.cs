using System;
using UnityEngine;
using UnityEngine.UI;
using Code.Networking.Session;
using TMPro;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyCreationUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button createSessionButton;
        [SerializeField] private Button joinSessionButton;
        [SerializeField] private Button practiceModeSessionButton;

        [SerializeField] private TMP_InputField codeInputField;
        
        private void Awake()
        {
            practiceModeSessionButton.onClick.AddListener(() => SessionManager.Instance.StartSessionAsHost(true));
            createSessionButton.onClick.AddListener(() => SessionManager.Instance.StartSessionAsHost(false));
            joinSessionButton.onClick.AddListener(() => SessionManager.Instance.JoinSessionByCode(codeInputField.text));
        }

        private void OnDisable()
        {
            createSessionButton.onClick.RemoveAllListeners();
            joinSessionButton.onClick.RemoveAllListeners();
        }
    }
}