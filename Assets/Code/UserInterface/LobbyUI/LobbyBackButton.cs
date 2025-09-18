using System;
using Code.Networking.Session;
using UnityEngine;
using UnityEngine.UI;
using Code.Helpers.UI;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyBackButton : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (SessionManager.Instance == null || UIUtilities.Instance == null) return;
            
            backButton.onClick.AddListener(SessionManager.Instance.LeaveSession);
            backButton.onClick.AddListener(() => UIUtilities.Instance.LoadScene("MainMenu"));
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }
    }
}