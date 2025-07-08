using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;

namespace Code.Audio
{
    public class AudioManager : NetworkBehaviour
    {
        public static AudioManager Instance;
        
        [SerializeField] private List<AudioInfo> audios;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            foreach (var audioInfo in audios)
            {
                audioInfo.source = gameObject.AddComponent<AudioSource>();
                audioInfo.source.loop = audioInfo.loop;
                audioInfo.source.clip = audioInfo.clip;
                audioInfo.source.volume = audioInfo.volume;
                audioInfo.source.pitch = audioInfo.pitch;
                audioInfo.source.outputAudioMixerGroup = audioInfo.mixer;

                audioInfo.source.playOnAwake = false;
                if (audioInfo.playOnAwake) audioInfo.source.Play();
            }
        }

        private void Start()
        {
            NetworkPlay("Music");
        }

        private void OnDisable() => Destroy(gameObject);

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

        public void Play(string audioName)
        {
            var audioInfo = GetAudio(audioName);
            audioInfo?.source.Play();
        }

        public void Stop(string audioName)
        {
            var audioInfo = GetAudio(audioName);
            audioInfo?.source.Stop();
        }
        
        public void Pause(string audioName)
        {
            var audioInfo = GetAudio(audioName);
            audioInfo?.source.Pause();
        }

        public void UnPause(string audioName)
        {
            var audioInfo = GetAudio(audioName);
            audioInfo?.source.UnPause();
        }
        
        public void NetworkPlay(string audioName) => PlayRpc((FixedString32Bytes)audioName);

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(FixedString32Bytes fixedAudioName)
        {
            var audioName = fixedAudioName.ToString();
            Play(audioName);
        }
    }
}