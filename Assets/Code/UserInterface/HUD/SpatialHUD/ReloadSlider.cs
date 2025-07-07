
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class ReloadSlider : PlayerHUDBase
    {
        [Header("UI Elements")] 
        [SerializeField] private Slider _reloadSlider;
        [SerializeField] private Image _activeReloadStartBar;
        [SerializeField] private Image _activeReloadEndBar;
        
        protected override void Update()
        {
            base.Update();
            if(!Assigned) return;
            
            UpdateReloadBar();
        }

        private void UpdateReloadBar()
        {
            Player.Dependencies.TryGetFeature(out Shoot shoot);

            if (!shoot.IsReloading)
            {
                _reloadSlider.gameObject.SetActive(false);
                return;
            }
            
            _reloadSlider.gameObject.SetActive(true);
            
            float reloadProgress = 1 - Mathf.Clamp01(shoot.ReloadTimer/shoot.ReloadTime);
            _reloadSlider.value = reloadProgress;

            if (shoot.FailedActiveReload)
            {
                _activeReloadStartBar.fillAmount = 0f;
                _activeReloadEndBar.fillAmount = 0f;
            }
            else
            {
                float position = shoot.ActiveReloadPosition;
                float span = shoot.ActiveReloadSpan;
                float start = Mathf.Clamp01(position - span/2);
                float end = Mathf.Clamp01(position + span/2);
                _activeReloadStartBar.fillAmount = start;
                _activeReloadEndBar.fillAmount = end;
            }
        }
    }
}
