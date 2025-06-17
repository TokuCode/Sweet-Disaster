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
            if (SessionManager.Instance != null && SessionManager.Instance.ActiveSession != null)
            {
                _text.text = $"Ping: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.CurrentSessionOwner)}ms";
                if (NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.CurrentSessionOwner) >= 150)
                    highPingObject.SetActive(true);
                else highPingObject.SetActive(false);
            }
            else _text.text = String.Empty;
        }
    }
}