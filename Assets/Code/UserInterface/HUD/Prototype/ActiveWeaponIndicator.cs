using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class ActiveWeaponIndicator : NetworkBehaviour
    {
        private GunBelt _belt;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _indicator;

        private void Update() => Text();

        private void Text()
        {
            if (PlayerController.Singleton == null) return;
            
            PlayerController.Singleton.Dependencies.TryGetFeature(out _belt);
            
            if (_belt == null)
            {
                _indicator.text = string.Empty;
                return;
            }

            _indicator.text = _belt.ActiveWeapon.ToString();
        }
    }
}