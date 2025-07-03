using Code.Gameplay.Character.Features;
using Code.Systems.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class StaminaSlider : PlayerHUDBase
    {
        [Header("UI Elements")] 
        [SerializeField] private GameObject _barLeftGo;
        [SerializeField] private GameObject _barRightGo;
        [SerializeField] private Image _barLeft;
        [SerializeField] private Image _barRight;
        
        [Header("Color")]
        [SerializeField] private Color _idleColor;
        [SerializeField] private Color _depletedColor;
        
        protected override void Update()
        {
            base.Update();
            if (!Assigned) return;
            
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            Player.Dependencies.TryGetFeature(out Shield shield);

            if (!shield.IsShieldActive && !shield.IsStaminaDepleted)
            {
                _barLeftGo.SetActive(false);
                _barRightGo.gameObject.SetActive(false);
                return;
            }
            
            float xDir = InputReader.Instance.HandleDirection.x;
            if (xDir > 0)
            {
                _barLeftGo.gameObject.SetActive(true);
                _barRightGo.gameObject.SetActive(false);
            }
            else
            {
                _barLeftGo.gameObject.SetActive(false);
                _barRightGo.gameObject.SetActive(true);
            }
            
            Image activeBar = _barLeftGo.gameObject.activeSelf ? _barLeft : _barRight;
            
            float staminaProgress = shield.CurrentShieldStamina / shield.MaxShieldStamina;
            activeBar.fillAmount = Mathf.Clamp01(staminaProgress);
            activeBar.color = shield.IsStaminaDepleted ? _depletedColor : _idleColor;
        }
    }
}