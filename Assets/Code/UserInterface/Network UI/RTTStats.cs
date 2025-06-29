using System;
using Code.Networking.Session;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace Code.UserInterface.Network_UI
{
    public class RTTStats : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        [SerializeField] private GameObject highPingObject;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            if (SessionManager.Instance == null ||
                !SessionManager.Instance.ShouldRetrievePing ||
                SessionManager.Instance.ActiveSession == null ||
                NetworkManager.Singleton == null ||
                NetworkManager.Singleton.NetworkConfig == null ||
                NetworkManager.Singleton.NetworkConfig.NetworkTransport == null)
                return;
            
            _text.text = $"Ping: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.CurrentSessionOwner)}ms";
            highPingObject.SetActive(NetworkManager.Singleton.NetworkConfig.NetworkTransport.
                GetCurrentRtt(NetworkManager.Singleton.CurrentSessionOwner) >= 180);
        }
    }
}