using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.Network_UI
{
    public class NetworkManagerUI : MonoBehaviour
    {
        [Header("Button")]
        [SerializeField] private Button _serverButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private Button _hostButton;
        
        private void Awake()
        {
            _serverButton.onClick.AddListener(OnServerButtonClicked);
            _clientButton.onClick.AddListener(OnClientButtonClicked);
            _hostButton.onClick.AddListener(OnHostButtonClicked);
        }
        
        private void OnServerButtonClicked()
        {
            NetworkManager.Singleton.StartServer();
        }
        
        private void OnClientButtonClicked()
        {
            NetworkManager.Singleton.StartClient();
        }
        
        private void OnHostButtonClicked()
        {
            NetworkManager.Singleton.StartHost();
        }
    }
}