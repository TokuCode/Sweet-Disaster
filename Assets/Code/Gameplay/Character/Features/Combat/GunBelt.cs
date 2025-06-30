using System;
using Code.Gameplay.Character.Framework;
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

        private Bomb bomb;
        
        [Header("Settings")]
        [SerializeField] private float _cooldown;
        private float _timer;

        [Header("Runtime")]
        [SerializeField] private NetworkVariable<Weapon> _activeWeapon = new(Weapon.Gun, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public Weapon ActiveWeapon => _activeWeapon.Value;
        public event Action<int> WeaponChanged;

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out bomb);
        }

        public override void UpdateFeature()
        {
            if (!IsOwner) return;
            
            if(_timer > 0) _timer -= Time.deltaTime;
            else if (InputReader.Instance.Switch && !bomb.IsThrowing)
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
            
            WeaponChanged?.Invoke(index);
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }
    }
}