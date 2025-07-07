using System;
using Unity.Netcode;
using UnityEngine;
using Code.Gameplay.Character.Features;
using Code.Gameplay.Character;
using Code.Networking.Session;
using TMPro;

namespace Code.UserInterface.HUD
{
    public class Settings : MonoBehaviour
    {
        private Speed _speed;
        private Jump _jump;
        private Shoot _shoot;
        
        [SerializeField] private GameObject settingsGo;
        
        [Header("Movement Settings")]
        [SerializeField] private TMP_InputField idleAccelField;
        [SerializeField] private TMP_InputField maxSpeedField;
        
        [Header("Jump Settings")]
        [SerializeField] private TMP_InputField impulseField;
        [SerializeField] private TMP_InputField gravityField;
        
        [Header("Dispersion Settings")]
        [SerializeField] private TMP_InputField movementDispersionField;
        [SerializeField] private TMP_InputField airDispersionField;
        
        /*
        private void Update()
        {
            if (SessionManager.Instance == null) return;
            
            if (!SessionManager.Instance.IsPracticeMode) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                settingsGo.SetActive(!settingsGo.activeInHierarchy);
                Time.timeScale = settingsGo.activeInHierarchy ? 0 : 1;
            }
        }
        */

        public void ApplyJumpSettingsFromUI()
        {
            ApplySettings();
            //ApplySettingsRpc();
        }

        private void ApplySettings()
        {
            if(PlayerController.Singleton == null) return;

            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _speed)) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _jump)) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _shoot)) return;
            
            /*
            if (float.TryParse(idleAccelField.text, out float idleAccel))
                _speed.AccelerationIdle = idleAccel;

            if (float.TryParse(maxSpeedField.text, out float maxSpeed))
                _speed.MaxSpeedIdle = maxSpeed;
            
            if (float.TryParse(impulseField.text, out float impulse))
                _jump.JumpImpulse = impulse;
            
            if (float.TryParse(gravityField.text, out float gravity))
                _jump.FallGravityMultiplier = gravity;

            if (float.TryParse(movementDispersionField.text, out float movementDispersion))
                _shoot.MovementImprecisionPerSpeedUnit =  movementDispersion;
                
            if (float.TryParse(airDispersionField.text, out float airDispersion))
                _shoot.AirImprecision = airDispersion;
                */
        }
        
        /*
        [Rpc(SendTo.NotMe)]
        private void ApplySettingsRpc()
        {
            ApplySettings();
        }
        */
    }
}