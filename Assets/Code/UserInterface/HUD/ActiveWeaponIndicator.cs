using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class ActiveWeaponIndicator : NetworkBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _indicator;

        private void Update() => Text();

        private void Text()
        {
            if (PlayerController.Singleton == null) return;
            
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out GunBelt belt))
            {
                _indicator.text = string.Empty;
                return;
            }

            _indicator.text = belt.ActiveWeapon.ToString();
        }
    }
}