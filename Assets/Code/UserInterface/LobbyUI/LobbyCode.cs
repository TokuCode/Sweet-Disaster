using System;
using Code.Networking.Session;
using TMPro;
using UnityEngine;

namespace Code.UserInterface.LobbyUI
{
    public class LobbyCode : MonoBehaviour
    {
        [SerializeField] private TMP_Text codeText;
        
        private void Start()
        {
            codeText.text = SessionManager.Instance.JoinDisplayCode;
        }
        
        public void CopyToClipboard() => GUIUtility.systemCopyBuffer = codeText.text;
    }
}