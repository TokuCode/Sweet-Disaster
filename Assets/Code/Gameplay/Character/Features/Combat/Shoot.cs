using System.Collections;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Shoot : NetworkBehaviour, Feature<PlayerController>
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
        [SerializeField] private float _bulletSpeed;
        [SerializeField] private float _bulletLifeTime;
        [SerializeField] private float _bulletDamage;
        [SerializeField] private int _bulletKnockbackLevel;
        
        [Header("Trajectory Settings")]
        [SerializeField] private float _baseImprecision;
        [SerializeField] private float _imprecision;
        [SerializeField] private float _imprecisionToAngleFactor;
        [SerializeField] private float _airImprecision;
        [SerializeField] private float _movementImprecisionPerSpeedUnit;

        [Header("Server Side")] 
        [SerializeField] private bool _serverShootRequested;
        [SerializeField] private bool _serverReloadRequested;
        [SerializeField] private bool _serverShootHoldInput;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnShoot += OnShoot;
            InputReader.Instance.OnShootRelease += OnShootRelease;
            InputReader.Instance.OnReload += OnReload;
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnShoot -= OnShoot;
            InputReader.Instance.OnShootRelease -= OnShootRelease;
            InputReader.Instance.OnReload -= OnReload;
        }

        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
            
            if(IsServer)
                _currentAmmo.Value = _magazineSize;
        }

        public void UpdateFeature()
        {
            if (IsServer)
            {
                SetActive();
                UpdateImprecision();
                
                if(_holdToShoot && _serverShootHoldInput)
                    TryShooting();
                else if(!_holdToShoot && _serverShootRequested)
                    TryShooting();
                
                if(_serverReloadRequested)
                    TryReload();
                
                if(_reloadTimer > 0) _reloadTimer -= Time.deltaTime;
                else if (_isReloading.Value) StopReloading();
            }
        }

        public void FixedUpdateFeature() { }

        private void OnShoot()
        {
            RequestShootServerRpc();
            SubmitShootHoldInputServerRpc(true);
        }
        
        private void OnShootRelease()
        {
            SubmitShootHoldInputServerRpc(false);
        }
        
        private void OnReload()
        {
            RequestReloadServerRpc();
        }
        
        [ServerRpc]
        private void RequestShootServerRpc(ServerRpcParams serverRpcParams = default)
        {
            _serverShootRequested = true;
        }
        
        [ServerRpc]
        private void RequestReloadServerRpc(ServerRpcParams serverRpcParams = default)
        {
            _serverReloadRequested = true;
        }
        
        [ServerRpc]
        public void SubmitShootHoldInputServerRpc(bool input, ServerRpcParams serverRpcParams = default)
        {
            _serverShootHoldInput = input;
        }

        private void TryShooting()
        {
            bool canShootInternal = _currentAmmo.Value > 0 & Time.time - _lastShotTime > _timeBetweenBursts & !_isShooting &
                !_isReloading.Value && _active;
            bool canShootExternal = !_playerController.IsCrouching; //Add Stun Shield Throw Checks
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
            BulletArgs bulletArgs = new()
            {
                OwnerTag = gameObject.tag,
                LifeTime = _bulletLifeTime,
                Damage = _bulletDamage,
                KnockbackLevel = _bulletKnockbackLevel,
                Speed = _bulletSpeed,
                Direction = ImprecisionDirection(_playerController.HandleDirection)
            };
            var instance = NetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, _playerController.HandlePosition, Quaternion.identity, bulletArgs);
            instance.Spawn();
        }

        private void UpdateImprecision()
        {
            _imprecision = _baseImprecision;
            
            float movementImprecision = _playerController.Velocity.x * _movementImprecisionPerSpeedUnit;
            _imprecision += movementImprecision;
            
            if(!_playerController.IsGrounded)
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

        private void ReloadAction() => _currentAmmo.Value = _magazineSize;

        private void SetActive() => _active = _playerController.CurrentWeapon == Handle.Weapon.Gun;
    }
}