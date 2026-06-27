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
        [SerializeField] private Button applyNameButton;

        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private TMP_InputField nameInputField;
        private string lastName = String.Empty;
        
        private void Awake()
        {
            practiceModeSessionButton.onClick.AddListener(() => SessionManager.Instance.StartSessionAsHost(true));
            createSessionButton.onClick.AddListener(() => SessionManager.Instance.StartSessionAsHost(false));
            joinSessionButton.onClick.AddListener(() => SessionManager.Instance.JoinSessionByCode(codeInputField.text));
            applyNameButton.onClick.AddListener(() =>
            {
                SessionManager.Instance.LocalPlayerDisplayName = nameInputField.text;
            });
        }

        private void Start()
        {
            nameInputField.text = SessionManager.Instance.LocalPlayerDisplayName;
        }

        private void Update()
        {
            //if (createSessionButton.interactable && joinSessionButton.interactable) return;
            if (SessionManager.Instance.HasLocalPlayerDisplayName)
            {
                createSessionButton.interactable = true;
                joinSessionButton.interactable = true;
            }
            else
            {
                createSessionButton.interactable = false;
                joinSessionButton.interactable = false;
            }
        }

        private void OnDisable()
        {
            practiceModeSessionButton.onClick.RemoveAllListeners();
            createSessionButton.onClick.RemoveAllListeners();
            joinSessionButton.onClick.RemoveAllListeners();
            applyNameButton.onClick.RemoveAllListeners();
        }
    }
}