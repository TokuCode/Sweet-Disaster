using System;
using Unity.Netcode;
using UnityEngine;
using Code.Gameplay.Character.Features;
using Code.Gameplay.Character;
using TMPro;

namespace Code.UserInterface.HUD
{
    public class JumpSettings : NetworkBehaviour
    {
        private Jump _jump;
        
        [SerializeField] private TMP_InputField impulseField;
        [SerializeField] private TMP_InputField cooldownField;
        [SerializeField] private TMP_InputField fallGravityField;
        [SerializeField] private TMP_InputField lowJumpGravityField;

        public void ApplyJumpSettingsFromUI()
        {
            if(PlayerController.Singleton == null) return;
            
            PlayerController.Singleton.Dependencies.TryGetFeature(out _jump);
            
            if(_jump == null) return;
            
            if (float.TryParse(impulseField.text, out float impulse))
                _jump.JumpImpulse = impulse;

            if (float.TryParse(cooldownField.text, out float cooldown))
                _jump.JumpCooldown = cooldown;

            if (float.TryParse(fallGravityField.text, out float fallGravity))
                _jump.FallGravityMultiplier = fallGravity;

            if (float.TryParse(lowJumpGravityField.text, out float lowJumpGravity))
                _jump.LowJumpGravityMultiplier = lowJumpGravity;
        }
    }
}