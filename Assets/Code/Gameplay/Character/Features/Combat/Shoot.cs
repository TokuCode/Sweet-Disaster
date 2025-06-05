using System.Collections;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Shoot : Feature
    {
        private PlayerController _playerController;
        
        [Header("Control")]
        [SerializeField] private bool _active;
        public bool Active => _active;
        
        [Header("Shooting Settings")]
        [SerializeField] private float _timeBetweenShots;
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
        
        [Header("Reloading Settings")]
        [SerializeField] private float _reloadTime;
        [SerializeField] private float _reloadTimer;
        [SerializeField] private int _magazineSize;
        public int MagazineSize => _magazineSize;
        private NetworkVariable<int> _currentAmmo = new (0, NetworkVariableReadPermission.Owner);
        public int CurrentAmmo => _currentAmmo.Value;
        private NetworkVariable<bool> _isReloading = new (false, NetworkVariableReadPermission.Owner);
        public bool IsReloading => _isReloading.Value;
        
        [Header("Projectile Settings")]
        [SerializeField] private GameObject _bulletPrefab;
        
        [Header("Trajectory Settings")]
        [SerializeField] private float _baseImprecision;
        [SerializeField] private float _imprecision;
        [SerializeField] private float _imprecisionToAngleFactor;
        [SerializeField] private float _airImprecision;
        [SerializeField] private float _movementImprecisionPerSpeedUnit;

        [Header("Server Side")] 
        [SerializeField] private bool _shootRequested;
        [SerializeField] private bool _reloadRequested;

        public override void InitializeFeature(Controller controller)
        {
            if(IsServer) _currentAmmo.Value = _magazineSize;
            base.InitializeFeature(controller);
        }

        public override void UpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            SetActive();
            UpdateImprecision();
            
            if (!IsServer) return;
            
            if(_reloadTimer > 0) _reloadTimer -= Time.deltaTime;
            else if (_isReloading.Value) StopReloading();

            if (_shootRequested)
            {
                TryShooting();
                _shootRequested = false;
            }

            if (_reloadRequested)
            {
                TryReload();
                _reloadRequested = false;
            }
        }

        public override void FixedUpdateFeature() { }
        
        private void TryShooting()
        {
            if (!_dependencies.TryGetFeature(out Crouch crouch)) return;
            if (!_dependencies.TryGetFeature(out Health health)) return;
            
            bool canShootInternal = _currentAmmo.Value > 0 & Time.time - _lastShotTime > _timeBetweenBursts & !_isShooting &
                !_isReloading.Value && _active;
            bool canShootExternal = !crouch.IsCrouching && !health.IsStunned;
            if (canShootInternal && canShootExternal)
                StartCoroutine(ShootingSequence());
            else if (_currentAmmo.Value <= 0) TryReload();
        }

        private IEnumerator ShootingSequence()
        {
            _isShooting = true;

            for (int i = 0; i < _burstCount; i++)
            {
                ShootAction();
                _lastShotTime = Time.time;
                _currentAmmo.Value--;

                if (_currentAmmo.Value == 0)
                    break;

                yield return new WaitForSeconds(_timeBetweenShots);
            }
            
            _isShooting = false;
            
            if(_currentAmmo.Value < 0) TryReload();
        }

        private void ShootAction()
        {
            if (!_invoker.CenterPosition.Request(out Vector3 centerposition).success) return;
            var position = centerposition + Vector3.up * InputReader.Instance.HandleHeight + cachedHandlePosition;
            var instance = NetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, position, Quaternion.identity);
            instance.Spawn();
            var bullet = instance.gameObject.GetComponent<ObjectBullet>();
            bullet?.Set(gameObject.tag, ImprecisionDirection(cachedHandleDirection));
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
            if (!_dependencies.TryGetFeature(out GunBelt belt)) return;
            _active = belt.ActiveWeapon == GunBelt.Weapon.Gun;
        }

        [ServerRpc]
        private void CacheHandleForServerRpc(Vector3 position, Vector3 direction)
        {
            cachedHandlePosition = position;
            cachedHandleDirection = direction;
        }
        
        public override void Apply(ref InputPayload @event)
        {
            CacheHandleForServerRpc(@event.handlePosition, @event.handleDirection);
            
            bool shootHold = @event.shootRequested;
            bool shootPressed = @event.shootRequested && !lastShootInput;

            if (_holdToShoot && shootHold)
                RequestShootToServerRpc();
            else if (!_holdToShoot && shootPressed)
                RequestShootToServerRpc();
            
            if(@event.reloadRequested || _currentAmmo.Value == 0)
                RequestReloadToServerRpc();

            lastShootInput = shootHold;
        }

        [ServerRpc]
        private void RequestShootToServerRpc()
        {
            _shootRequested = true;
        }
        
        [ServerRpc]
        private void RequestReloadToServerRpc()
        {
            _reloadRequested = true;
        }
    }
}