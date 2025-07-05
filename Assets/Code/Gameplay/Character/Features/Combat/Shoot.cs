using System;
using System.Collections;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers;
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
        private Shield shield;
        private Bomb bomb;
        private Melee melee;
        

        [Header("Shooting Settings")]
        [SerializeField] private float _timeBetweenShots;

        [SerializeField] private int _burstCount;
        [SerializeField] private float _timeBetweenBursts;
        [SerializeField] private bool _holdToShoot;
        [SerializeField] private float _lastShotTime;
        public float LastShotTime => _lastShotTime;
        [SerializeField] private bool _isShooting;
        public bool IsShooting => _isShooting;

        [Header("Reloading Settings")]
        [SerializeField] private float _reloadTime;
        public float ReloadTime => _reloadTime;
        private NetworkVariable<float> _reloadTimer = new(0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public float ReloadTimer => _reloadTimer.Value;
        [SerializeField] private int _magazineSize;
        public int MagazineSize => _magazineSize;
        private NetworkVariable<int> _currentAmmo = new(0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public int CurrentAmmo => _currentAmmo.Value;
        private NetworkVariable<bool> _isReloading = new(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        public bool IsReloading => _isReloading.Value;
        
        [Header("Active Reload Settings")]
        [SerializeField] private float _activeReloadPosition;
        public float ActiveReloadPosition => _activeReloadPosition;
        [SerializeField] private float _activeReloadSpan;
        public float ActiveReloadSpan => _activeReloadSpan;
        private bool _failedActiveReload;
        public bool FailedActiveReload => _failedActiveReload;

        [Header("Projectile Settings")] 
        [SerializeField] private GameObject _bulletPrefab;

        private NetworkObject _bulletNetworkObject;

        [Header("Trajectory Settings")]
        [SerializeField] private float _baseImprecision;

        [SerializeField] private float _imprecision;
        [SerializeField] private float _imprecisionToAngleFactor;
        [SerializeField] private float _airImprecision;
        [SerializeField] private float _movementImprecisionPerSpeedUnit;

        [Header("Recoil Settings")]
        [SerializeField] private float _recoilForce;
        [SerializeField] private float _recoilImpulseAngle;

        [Header("Overshoot Settings")] 
        [SerializeField] private float _damageAmpPerTemperature;

        [Header("Server Side")] 
        [SerializeField] private bool _reloadRequested;

        public event Action OnActiveReload;
        
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

        public override void ResetFeature()
        {
            if (IsOwner)
            {
                CancelShooting();
                CancelReloading();
                _currentAmmo.Value = _magazineSize;
            }
        }

        public override void InitializeFeature(Controller controller)
        {

            if (IsOwner)
            {
                InputReader.Instance.OnShootPressed += OnShootPressed;
                InputReader.Instance.OnReloadPressed += TryActiveReload;
                _currentAmmo.Value = _magazineSize;
            }
            
            base.InitializeFeature(controller);
            
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out shield);
            _dependencies.TryGetFeature(out bomb);
            _dependencies.TryGetFeature(out melee);

            health.OnStun += OnStun;
        }

        public override void UpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            if(!_isReloading.Value && _failedActiveReload) _failedActiveReload = false;
            
            if (_holdToShoot && InputReader.Instance.Shoot && IsOwner)
                TryShooting();
            
            if (!IsOwner) return;
            
            if(_reloadTimer.Value > 0) _reloadTimer.Value -= Time.deltaTime;
            else if (_isReloading.Value) StopReloading();
        }

        public override void FixedUpdateFeature() { }

        private void OnShootPressed()
        {
            if(!_holdToShoot) TryShooting();
        }
        
        private void TryShooting()
        {
            bool canShootInternal = _currentAmmo.Value > 0 & Time.time - _lastShotTime > _timeBetweenBursts & !_isShooting &
                !_isReloading.Value;
            bool canShootExternal = !crouch.IsCrouching && !health.IsStunned && !shield.IsShieldActive && !bomb.IsThrowing && !melee.IsAttacking;
            if (canShootInternal && canShootExternal)
                StartCoroutine(ShootingSequence());
            else if (_currentAmmo.Value <= 0)
            {
                TryReload();
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
                _currentAmmo.Value--;
                if (_currentAmmo.Value <= 0)
                {
                    TryReload();
                    break;
                }

                yield return new WaitForSeconds(_timeBetweenShots);
                
                if (_currentAmmo.Value <= 0) break;
            }
            
            _isShooting = false;
        }

        private void CancelShooting()
        {
            if(!_isShooting) return;
            
            StopAllCoroutines();
            _isShooting = false;
        }

        private void ShootAction(int burstIndex)
        {
            var direction = InputReader.Instance.HandleDirection;
            if(burstIndex > 0) direction = ImprecisionDirection(direction);
            _invoker.GunTipPosition.Request(out var position);
            
            float damageMultiplier = 1 + _damageAmpPerTemperature * shield.TemperatureProgress; 
            
            FireAction(position, direction, out int id, damageMultiplier);
            _invoker.PlayerNumber.Request(out int clientId);
            ReplicateFireGunRpc(position, direction, id, DateTime.Now, clientId, damageMultiplier);
            
            Recoil(direction);
        }

        private void FireAction(Vector3 position, Vector3 direction, out int bulletId, float damageMultiplier)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bulletNetworkObject = NonNetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, position, rotation, out bulletId);
            
            var bullet = _bulletNetworkObject.gameObject.GetComponent<ObjectBullet>();
            
            _invoker.PlayerNumber.Request(out int clientId);

            
            bullet.Initialize(direction, gameObject.tag, 0, clientId, damageMultiplier);
        }

        private void ReplicateFireAction(Vector3 position, Vector3 direction, int bulletId, float latency, int senderId, float damageMultiplier)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bulletNetworkObject = NonNetworkObjectPool.Singleton.GetNetworkObjectById(_bulletPrefab, position, rotation, bulletId, senderId);
            
            var bullet = _bulletNetworkObject.gameObject.GetComponent<ObjectBullet>();
            bullet.Initialize(direction, gameObject.tag, latency, senderId, damageMultiplier); 
        }

        private void Recoil(Vector3 direction)
        {
            float minY = -Mathf.Cos(_recoilImpulseAngle * Mathf.Deg2Rad);
            if (_invoker.Velocity.Request(out var velocity).success)
            { 
                if (direction.y <= minY && velocity.y < 0)
                    _invoker.Velocity.Perform(velocity.With(y: 0));
            }
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
            if(_currentAmmo.Value < _magazineSize && !_isReloading.Value)
            {
                _isReloading.Value = true;
                _reloadTimer.Value = _reloadTime;
            }
        }

        private void StopReloading()
        {
            ReloadAction();
            _isReloading.Value = false;
        }

        private void OnStun(float stunDuration, float healthRatio)
        {
            CancelReloading();
        }

        private void CancelReloading()
        {
            if (!_isReloading.Value) return;
            
            _isReloading.Value = false;
        }

        private void ReloadAction()
        { 
            _currentAmmo.Value = _magazineSize;   
        }
        
        public override void Apply(ref InputPayload @event)
        {
            if (@event.reload && !_isReloading.Value && !_isShooting && !bomb.IsThrowing && !shield.IsShieldActive && !melee.IsAttacking) 
            {
                TryReload();
            }
        }

        public void TryActiveReload()
        {
            if(_failedActiveReload || !_isReloading.Value) return;

            float progress = 1 - _reloadTimer.Value/_reloadTime;
            if (Mathf.Abs(progress - _activeReloadPosition) <= _activeReloadSpan / 2f)
            {
                ActiveReloadAction();
            }
            else _failedActiveReload = true;
        }

        private void ActiveReloadAction()
        {
            StopReloading();
            OnActiveReload?.Invoke();
        }

        [ServerRpc]
        private void ActiveReloadSuccessServerRpc()
        {
            ActiveReloadAction();
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
            if (_currentAmmo.Value <= 0)
                TryReload();
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateFireGunRpc(Vector3 position, Vector3 direction, int objectId, DateTime timestamp, int clientId, float damageMultiplier)
        {
            float latency = MilisecondsUtils.CalculateLatency(timestamp);
            ReplicateFireAction(position, direction, objectId, latency, clientId, damageMultiplier);
        }
    }
}