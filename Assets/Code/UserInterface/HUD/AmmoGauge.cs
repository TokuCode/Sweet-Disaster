using System;
using Code.Gameplay.Character;
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
            var player = PlayerController.Singleton;
            
            if (!IsClient || player == null)
            {
                _ammoText.text = string.Empty;
                return;
            }

            if (player.IsReloading)
            {
                _ammoText.text = "Reloading";
                return;
            }
            
            _ammoText.text = $"{player.CurrentAmmo}/{player.MagazineSize}";
        }
    }
}