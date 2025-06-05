using System;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Gameplay.Character.Features
{
    public class GunBelt : Feature
    {
        public enum Weapon
        {
            Gun,
            Bomb
        }
        
        [Header("Settings")]
        [SerializeField] private float _cooldown;
        private float _timer;

        [Header("Runtime")]
        [SerializeField] private NetworkVariable<Weapon> _activeWeapon = new(Weapon.Gun, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public Weapon ActiveWeapon => _activeWeapon.Value;
        
        public override void UpdateFeature()
        {
            if (!IsOwner) return;
            
            if(_timer > 0) _timer -= Time.deltaTime;
            else if (InputReader.Instance.Switch)
            {
                SwitchWeapon();
                _timer = _cooldown;
            }
        }

        private void SwitchWeapon()
        {
            var weapons = Enum.GetValues(typeof(Weapon));
            int index = (int)_activeWeapon.Value;
            index = (index + 1) % weapons.Length;
            _activeWeapon.Value = (Weapon)index;
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }
    }
}