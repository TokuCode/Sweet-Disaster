using System;
using Code.UserInterface.LobbyUI;
using UnityEngine;
using System.Collections.Generic;
using Code.Networking.Session;

namespace Code.UserInterface.PostGameUI
{
    public class PostGameUIManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> loserList;
        [SerializeField] private GameObject winner;

        private void Awake()
        {
            if (SessionManager.Instance == null) return;
            if (SessionManager.Instance.ActiveSession == null) return;

            var winnerSlot = winner.GetComponent<PlayerSlotUI>();
            
            
        }
    }   
}