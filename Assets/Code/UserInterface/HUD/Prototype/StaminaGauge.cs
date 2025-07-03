using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class StaminaGauge : MonoBehaviour
    {
        private Shield shield;
        
        [Header("UI Elements")]
        [SerializeField] private Image _staminaBar;
        
        [Header("Stamina Parameters")]
        [SerializeField] private Color _idleColor;
        [SerializeField] private Color _depletedColor;

        private void Update()
        { 
            UpdateSlider();  
            UpdateColor();
        } 
    
        private void UpdateSlider()
        {
            if(PlayerController.Singleton == null) return;
            
            PlayerController.Singleton.Dependencies.TryGetFeature(out shield);
            
            if (shield == null) return;
    
            var stamina = shield.CurrentShieldStamina;
            var maxStamina = shield.MaxShieldStamina;
    
            _staminaBar.fillAmount = Mathf.Clamp01(stamina / maxStamina);
        }
    
        private void UpdateColor()
        {
            if(PlayerController.Singleton == null) return;
            
            PlayerController.Singleton.Dependencies.TryGetFeature(out shield);
            
            if (shield == null) return;
    
            _staminaBar.color = shield.IsStaminaDepleted ? _depletedColor : _idleColor;
        }
    }
}
