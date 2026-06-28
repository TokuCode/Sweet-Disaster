using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class StrengthSlider : PlayerHUDBase
    {
        [Header("UI Elements")]
        [SerializeField] private Image _strength;
        [SerializeField] private Image background;

        protected override void Update()
        {
            base.Update();
            if(!Assigned) return;
            
            UpdateStrength();
        }

        private void UpdateStrength()
        {
            Player.Dependencies.TryGetFeature(out Bomb bomb);

            if (!bomb.IsThrowing)
            {
                _strength.fillAmount = 0;
                background.gameObject.SetActive(false);
                return;
            }
            
            background.gameObject.SetActive(true);
            float ratio = bomb.ThrowChargeTimer / bomb.ThrowChargeTimeSeconds;
            _strength.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}