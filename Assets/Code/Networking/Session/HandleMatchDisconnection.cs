using System;
using UnityEngine;

namespace Code.Networking.Session
{
    public class HandleMatchDisconnection : MonoBehaviour
    {
        private SessionManager _sessionManager;

        private void Awake()
        {
            _sessionManager = SessionManager.Instance;

            _sessionManager.ActiveSession.PlayerHasLeft += OnPlayerLeft;
        }

        private void OnPlayerLeft(string id)
        {
            
        }
    }
}