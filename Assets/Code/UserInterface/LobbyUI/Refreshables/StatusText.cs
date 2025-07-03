using System;
using Code.Networking.Session;
using UnityEngine;
using TMPro;
using WebSocketSharp;

namespace Code.UserInterface.LobbyUI
{
    public class StatusText : Refreshable
    {
        [SerializeField] private TextMeshProUGUI statusText;
        private SessionManager _sessionManager;
        private void Awake() => _sessionManager = SessionManager.Instance;

        public override void Refresh()
        {
            string charName = _sessionManager.playerInfo.GetPropertyValue(_sessionManager.ActiveSession.CurrentPlayer,
                _sessionManager.PlayerCharacterKey);
            
            if (!charName.IsNullOrEmpty())
            {
                if (_sessionManager.ActiveSession.PlayerCount > 1 || (_sessionManager.ActiveSession.PlayerCount == 1 && _sessionManager.IsPracticeMode))
                {
                    if (LobbyUIManager.Instance.AllPlayersHaveSelectedCharacters())
                        statusText.text = _sessionManager.ActiveSession.IsHost ? 
                            "La partida esta lista para ser iniciada" : "Esperando al anfitrión";
                    else statusText.text = "Esperando a los jugadores";
                }
                else statusText.text = "Esperando a los jugadores";
            }
            else statusText.text = "Elige tu personaje";
        }
    }
}