using Unity.Collections;
using Unity.Netcode;

namespace Code.Audio
{
    public class NetworkAudioManager : NetworkBehaviour
    {
        public static NetworkAudioManager Instance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void OnDisable() => Destroy(gameObject);

        public void NetworkPlay(string audioName)
        {
            AudioManager.Instance.Play(audioName);
            NetworkPlayRpc((FixedString32Bytes)audioName);
        }

        [Rpc(SendTo.NotMe)]
        private void NetworkPlayRpc(FixedString32Bytes fixedAudioName)
        {
            var audioName = fixedAudioName.ToString();
            AudioManager.Instance.Play(audioName);
        }

        public void NetworkStop(string audioName)
        {
            AudioManager.Instance.Stop(audioName);
            NetworkStopRpc((FixedString32Bytes)audioName);
        }
            
        [Rpc(SendTo.NotMe)]
        private void NetworkStopRpc(FixedString32Bytes fixedAudioName)
        {
            var audioName = fixedAudioName.ToString();
            AudioManager.Instance.Stop(audioName);
        }
    }
}