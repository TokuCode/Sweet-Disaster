using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class AmmoGauge : NetworkBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _ammoText;

        private void Update() => Text();

        private void Text()
        {
            if (PlayerController.Singleton == null) return;
            
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Shoot shoot))
            {
                _ammoText.text = string.Empty;
                return;
            }

            if (shoot.IsReloading)
            {
                _ammoText.text = "Reloading";
                return;
            }
            
            _ammoText.text = $"{shoot.CurrentAmmo}/{shoot.MagazineSize}";
        }
    }
}