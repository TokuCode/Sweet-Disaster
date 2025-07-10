using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Code.UserInterface.Settings
{
    public class GameSettings : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        
        [SerializeField] private Slider generalSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private readonly string _generalVolumeKey = "MasterVolume";
        private readonly string _musicVolumeKey = "MusicVolume";
        private readonly string _sfxVolumeKey = "SfxVolume";
        
        [SerializeField] private float generalVolumeDefault;
        [SerializeField] private float musicVolumeDefault;
        [SerializeField] private float sfxVolumeDefault;
        
        private void Start()
        {
            InitSlider(_generalVolumeKey, generalSlider, generalVolumeDefault);
            InitSlider(_musicVolumeKey, musicSlider, musicVolumeDefault);
            InitSlider(_sfxVolumeKey, sfxSlider, sfxVolumeDefault);
            
            SetGeneralVolume(generalSlider.value);
            SetMusicVolume(musicSlider.value);
            SetSfxVolume(sfxSlider.value);
        }

        private void InitSlider(string key, Slider slider, float defaultValue)
        {
            slider.value = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
        }

        public void SetGeneralVolume(float value)
        {
            mixer.SetFloat(_generalVolumeKey, Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat(_generalVolumeKey, generalSlider.value);
        }

        public void SetMusicVolume(float value)
        {
            mixer.SetFloat(_musicVolumeKey, Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat(_musicVolumeKey, musicSlider.value);
        }

        public void SetSfxVolume(float value)
        {
            mixer.SetFloat(_sfxVolumeKey, Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat(_sfxVolumeKey, sfxSlider.value);
        }

        public void RestoreDefaults()
        {
            generalSlider.value = generalVolumeDefault;
            musicSlider.value = musicVolumeDefault;
            sfxSlider.value = sfxVolumeDefault;
        }
    }
}