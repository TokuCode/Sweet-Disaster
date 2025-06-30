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
            NoWeapon,
            Gun,
            Bomb,
            Reloading,
            Melee,
            Shield
        }

        private Bomb bomb;
        private Shoot shoot;
        private Shield shield;
        private Melee melee;
        
        [Header("Settings")]
        [SerializeField] private float _cooldown;
        private float _timer;
        [SerializeField] private float _maxActiveWeaponTime;

        [Header("Runtime")]
        [SerializeField] private NetworkVariable<Weapon> _activeWeapon = new(Weapon.Gun, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public Weapon ActiveWeapon => _activeWeapon.Value;
        public event Action<int> WeaponChanged;
        private Weapon _lastActiveWeapon;
        public Weapon LastActiveWeapon => _lastActiveWeapon;
        private float _lastActiveTime;

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out bomb);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out shield);
            _dependencies.TryGetFeature(out melee);
        }

        public override void UpdateFeature()
        {
            if (!IsOwner) return;
            
            SetActiveWeapon();
        }

        private void SetActiveWeapon()
        {
            bool overwriteActiveLastWeapon = false;

            if (melee.IsAttacking)
            {
                _activeWeapon.Value = Weapon.Melee;
                overwriteActiveLastWeapon = true;
            }
            else if (bomb.IsThrowing)
            {
                _activeWeapon.Value = Weapon.Bomb;
                overwriteActiveLastWeapon = true;
            }
            else if (shoot.IsShooting)
            {
                _activeWeapon.Value = Weapon.Gun;
                overwriteActiveLastWeapon = true;
            }
            else if (shield.IsShieldActive)
            {
                _activeWeapon.Value = Weapon.Shield;
            }
            else if (shoot.IsReloading)
            {
                _activeWeapon.Value = Weapon.Reloading;
            }
            else if(Time.time - _lastActiveTime < _maxActiveWeaponTime)
            {
                _activeWeapon.Value = _lastActiveWeapon;
            }
            else
            {
                _activeWeapon.Value = Weapon.NoWeapon;
            }

            if (_activeWeapon.Value != _lastActiveWeapon && overwriteActiveLastWeapon)
            {
                _lastActiveTime = Time.time;
                _lastActiveWeapon = _activeWeapon.Value;
            }
            
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