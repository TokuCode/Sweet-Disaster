using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class AmmoGauge : NetworkBehaviour
    {
        private Shoot _shoot;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _ammoText;

        private void Update() => Text();

        private void Text()
        {
            if (PlayerController.Singleton == null) return;

            PlayerController.Singleton.Dependencies.TryGetFeature(out _shoot);
            
            if (_shoot == null)
            {
                _ammoText.text = string.Empty;
                return;
            }

            if (_shoot.IsReloading)
            {
                _ammoText.text = "Reloading";
                return;
            }
            
            _ammoText.text = $"{_shoot.CurrentAmmo}/{_shoot.MagazineSize}";
        }
    }
}