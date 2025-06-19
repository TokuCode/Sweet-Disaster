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
        [SerializeField] private TMP_InputField impulseField;
        [SerializeField] private TMP_InputField cooldownField;
        [SerializeField] private TMP_InputField fallGravityField;
        [SerializeField] private TMP_InputField lowJumpGravityField;
        
        
        public void ApplyJumpSettingsFromUI()
        {
            if (PlayerController.Singleton == null) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Jump jumpFeature)) return;

            if (float.TryParse(impulseField.text, out float impulse))
                jumpFeature.JumpImpulse = impulse;

            if (float.TryParse(cooldownField.text, out float cooldown))
                jumpFeature.JumpCooldown = cooldown;

            if (float.TryParse(fallGravityField.text, out float fallGravity))
                jumpFeature.FallGravityMultiplier = fallGravity;

            if (float.TryParse(lowJumpGravityField.text, out float lowJumpGravity))
                jumpFeature.LowJumpGravityMultiplier = lowJumpGravity;
        }
    }
}