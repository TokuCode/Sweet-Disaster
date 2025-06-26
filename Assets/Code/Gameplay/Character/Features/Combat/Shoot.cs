using System;
using System.Collections;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Gameplay.Character.Features
{
    public class Shoot : Feature
    {
        private PlayerController _playerController;

        private Crouch crouch;
        private Health health;
        private GunBelt belt;
        private Shield shield;

        [Header("Control")] [SerializeField] private bool _active;
        public bool Active => _active;

        [Header("Shooting Settings")] [SerializeField]
        private float _timeBetweenShots;

        [SerializeField] private int _burstCount;
        [SerializeField] private float _timeBetweenBursts;
        [SerializeField] private bool _holdToShoot;
        [SerializeField] private float _lastShotTime;
        public float LastShotTime => _lastShotTime;
        [SerializeField] private bool _isShooting;
        public bool IsShooting => _isShooting;
        private Vector3 cachedHandlePosition;
        private Vector3 cachedHandleDirection;
        private bool lastShootInput;

        [Header("Reloading Settings")] [SerializeField]
        private float _reloadTime;

        [SerializeField] private float _reloadTimer;
        [SerializeField] private int _magazineSize;
        public int MagazineSize => _magazineSize;
        private NetworkVariable<int> _currentAmmo = new(0, NetworkVariableReadPermission.Owner);
        public int CurrentAmmo => _currentAmmo.Value;
        private NetworkVariable<bool> _isReloading = new(false, NetworkVariableReadPermission.Owner);
        public bool IsReloading => _isReloading.Value;

        [Header("Projectile Settings")] [SerializeField]
        private GameObject _bulletPrefab;

        private NetworkObject _bulletNetworkObject;

        [Header("Trajectory Settings")] [SerializeField]
        private float _baseImprecision;

        [SerializeField] private float _imprecision;
        [SerializeField] private float _imprecisionToAngleFactor;
        [SerializeField] private float _airImprecision;
        [SerializeField] private float _movementImprecisionPerSpeedUnit;

        [Header("Recoil Settings")] [SerializeField]
        private float _recoilForce;

        [Header("Server Side")] [SerializeField]
        private bool _reloadRequested;

        public float MovementImprecisionPerSpeedUnit
        {
            get => _movementImprecisionPerSpeedUnit;
            set => _movementImprecisionPerSpeedUnit = value;
        }
        public float AirImprecision
        {
            get => _airImprecision;
            set => _airImprecision = value;
        }
        
        public override void InitializeFeature(Controller controller)
        {
            if(IsServer) _currentAmmo.Value = _magazineSize;

            if (IsOwner) InputReader.Instance.OnShootPressed += OnShootPressed;
            
            base.InitializeFeature(controller);
            
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out belt);
            _dependencies.TryGetFeature(out shield);
        }

        public override void UpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            SetActive();
            
            if (_holdToShoot && InputReader.Instance.Shoot && IsOwner)
                TryShooting();
            
            if (!IsServer) return;
            
            if(_reloadTimer > 0) _reloadTimer -= Time.deltaTime;
            else if (_isReloading.Value) StopReloading();

            if (_reloadRequested)
            {
                TryReload();
                _reloadRequested = false;
            }
        }

        public override void FixedUpdateFeature() { }

        private void OnShootPressed()
        {
            if(!_holdToShoot) TryShooting();
        }
        
        private void TryShooting()
        {
            bool canShootInternal = _currentAmmo.Value > 0 & Time.time - _lastShotTime > _timeBetweenBursts & !_isShooting &
                !_isReloading.Value && _active;
            bool canShootExternal = !crouch.IsCrouching && !health.IsStunned && !shield.IsShieldActive;
            if (canShootInternal && canShootExternal)
                StartCoroutine(ShootingSequence());
            else if (_currentAmmo.Value <= 0)
            {
                if(!IsServer) RequestReloadToServerRpc();
                else TryReload();
            }
        }

        private IEnumerator ShootingSequence()
        {
            _isShooting = true;
            UpdateImprecision();
            
            for (int i = 0; i < _burstCount; i++)
            {
                ShootAction(i);
                _lastShotTime = Time.time;
                if(IsHost)_currentAmmo.Value--;
                else RequestAmmoDepletionToServerRpc();

                if (_currentAmmo.Value == 0)
                {
                    _isShooting = false;
                    if(!IsServer) RequestReloadToServerRpc();
                    else TryReload();
                    break;
                }

                yield return new WaitForSeconds(_timeBetweenShots);
            }
            
            _isShooting = false;
        }

        private void ShootAction(int burstIndex)
        {
            var direction = InputReader.Instance.HandleDirection;
            if(burstIndex > 0) direction = ImprecisionDirection(direction);
            var position = InputReader.Instance.HandlePosition;
            
            FireAction(position, direction, out int id);
            ReplicateFireGunRpc(position, direction, id, DateTime.Now);
            
            Recoil(direction);
        }

        private void FireAction(Vector3 position, Vector3 direction, out int bulletId)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bulletNetworkObject = NonNetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, position, rotation, out bulletId);
            
            var bullet = _bulletNetworkObject.gameObject.GetComponent<ObjectBullet>();
            bullet.Initialize(direction, gameObject.tag, 0);
        }

        private void ReplicateFireAction(Vector3 position, Vector3 direction, int bulletId, float latency)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bulletNetworkObject = NonNetworkObjectPool.Singleton.GetNetworkObjectById(_bulletPrefab, position, rotation, bulletId);
            
            var bullet = _bulletNetworkObject.gameObject.GetComponent<ObjectBullet>();
            bullet.Initialize(direction, gameObject.tag, latency); 
        }

        private void Recoil(Vector3 direction)
        {
            _invoker.AddForce.Perform(new(-direction, _recoilForce, ForceMode2D.Impulse));
        }

        private void UpdateImprecision()
        {
            _imprecision = _baseImprecision;
            
            if(!_invoker.Velocity.Request(out var velocity).success) return;
            
            float movementImprecision = velocity.x * _movementImprecisionPerSpeedUnit;
            _imprecision += movementImprecision;

            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            
            if(!check.IsGrounded)
                _imprecision += _airImprecision;
        }

        private Vector3 ImprecisionDirection(Vector3 inputDirection)
        {
            float angleAmplitude = _imprecision * _imprecisionToAngleFactor;
            float randomAngle = Random.Range(-angleAmplitude, angleAmplitude);
            return Quaternion.Euler(0, 0, randomAngle) * inputDirection;
        }

        private void TryReload()
        {
            if(!_isShooting && _currentAmmo.Value < _magazineSize && !_isReloading.Value)
            {
                _isReloading.Value = true;
                _reloadTimer = _reloadTime;
            }
        }

        private void StopReloading()
        {
            ReloadAction();
            _isReloading.Value = false;
        }

        private void ReloadAction()
        { 
            _currentAmmo.Value = _magazineSize;   
        }

        private void SetActive()
        {
            _active = belt.ActiveWeapon == GunBelt.Weapon.Gun;
        }
        
        public override void Apply(ref InputPayload @event)
        {
            if (@event.reload && belt.ActiveWeapon == GunBelt.Weapon.Gun)
            {
                if(!IsServer) RequestReloadToServerRpc();
                else TryReload();
            }
        }

        [ServerRpc]
        private void RequestReloadToServerRpc()
        {
            _reloadRequested = true;
        }

        [ServerRpc]
        private void RequestAmmoDepletionToServerRpc()
        {
            _currentAmmo.Value--;
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateFireGunRpc(Vector3 position, Vector3 direction, int objectId, DateTime timestamp)
        {
            float latency = MilisecondsUtils.CalculateLatency(timestamp);
            ReplicateFireAction(position, direction, objectId, latency);
        }
    }
}