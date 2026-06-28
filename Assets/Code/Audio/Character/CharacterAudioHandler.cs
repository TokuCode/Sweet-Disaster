using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Code.Audio.Character
{
    public class CharacterAudioHandler : NetworkBehaviour
    {
        [SerializeField] private List<AudioInfo> audios;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            foreach (var audioInfo in audios)
            {
                audioInfo.source = gameObject.AddComponent<AudioSource>();
                audioInfo.source.loop = audioInfo.loop;
                audioInfo.source.clip = audioInfo.clip;
                audioInfo.source.volume = audioInfo.volume;
                audioInfo.source.pitch = audioInfo.pitch;
                audioInfo.source.outputAudioMixerGroup = audioInfo.mixer;

                audioInfo.source.playOnAwake = false;
                audioInfo.source.spatialBlend = .75f;
            }
        }
        
        private AudioInfo GetAudio(string audioName)
        {
            foreach (var audioInfo in audios)
            {
                if (audioInfo.name == audioName) 
                    return audioInfo;
            }
#if UNITY_EDITOR
            Debug.LogWarning($"No audio found with name {audioName}");
#endif
            return null;
        }

        public void NetworkPlay(string audioName)
        {
            Play(audioName);
            NetworkPlayAudioRpc((FixedString32Bytes)audioName);
        }
        
        private void Play(string audioName)
        {
            var audioInfo = GetAudio(audioName);
            audioInfo?.source.Play();
        }

        [Rpc(SendTo.NotMe)]
        private void NetworkPlayAudioRpc(FixedString32Bytes audioName) => Play(audioName.ToString());
    }
}