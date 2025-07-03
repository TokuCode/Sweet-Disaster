using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class MeleeCooldownSlider : PlayerHUDBase
    {
        [Header("UI Elements")]
        [SerializeField] private Image _slider;

        protected override void Update()
        {
            base.Update();
            if(!Assigned) return;
            
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            Player.Dependencies.TryGetFeature(out Melee melee);

            if (!melee.OnCooldown)
            {
                _slider.fillAmount = 0f;
                return;
            }

            _slider.fillAmount = melee.CooldownProgress;
        }
    }
}