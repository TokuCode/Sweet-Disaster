using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Shield : Feature, IProcess<AttackEvent>
    {
        private Health health;
        private Shoot shoot;
        private Bomb bomb;
        private Melee melee;
        
        [Header("Shield Parameters")]
        [SerializeField] private GameObject _shieldPrefab;
        private GameObject _shield;
        [SerializeField] private float _shieldAngle;
        [SerializeField] private NetworkVariable<bool> _isShieldActive = new(false, NetworkVariableReadPermission.Owner);
        [SerializeField] private float _deactivateShieldDelay;
        [SerializeField] private NetworkVariable<bool> _isDeactivatingShield = new(false, NetworkVariableReadPermission.Owner);
        public bool IsShieldActive => _isShieldActive.Value || _isDeactivatingShield.Value;
    
        [Header("Shield Stamina")] 
        [SerializeField] private float _maxShieldStamina;
        public float MaxShieldStamina => _maxShieldStamina;
        [SerializeField] private NetworkVariable<float> _currentShieldStamina = new(0, NetworkVariableReadPermission.Owner);
        public float CurrentShieldStamina => _currentShieldStamina.Value;
        [SerializeField] private float _shieldStaminaRegenRate;
        [SerializeField] private float _shieldStaminaSpendRate;
        [SerializeField] private bool _isStaminaDepleted;
        public bool IsStaminaDepleted => _isStaminaDepleted;
        [SerializeField] private float _minShieldStaminaForActivation;
        
        [Header("Shield Temperature")]
        [SerializeField] private float _maxShieldTemperature;
        [SerializeField] private float _heatPerDamagePercentage;
        [SerializeField] private float _heatDissipationRate;
        [SerializeField] private float _cooldownRate;
        [SerializeField] private float _selfDamageInExplosion;
        [SerializeField] private Vector2 _selfKnockbackInExplosion;
        [SerializeField] private GameObject _selfAttackVfx;
        [SerializeField] private float _selfAttackRadius;
        private NetworkVariable<bool> _isOnCooldown = new();
        public bool OnCooldown => _isOnCooldown.Value;
        private NetworkVariable<float> _shieldTemperature = new();
        public float Temperature => _shieldTemperature.Value;
        public float TemperatureProgress => _shieldTemperature.Value / _maxShieldTemperature;

        [Header("Temperature Zones")]
        [SerializeField] private float _safeZoneMax;
        public float SafeZoneMax => _safeZoneMax;
        public bool OnSafeZone => TemperatureProgress < SafeZoneMax;
        public bool OutSafeZone => TemperatureProgress >= SafeZoneMax;
        [SerializeField] private float _warningZoneMax;
        public float WarningZoneMax => _warningZoneMax;
        public bool OnWarningZone => TemperatureProgress < WarningZoneMax && TemperatureProgress >= SafeZoneMax;
        public bool OnDangerZone => TemperatureProgress >= WarningZoneMax;

        public override void ResetFeature()
        {
            if (IsServer)
            {
                _isShieldActive.Value = false;
                _isDeactivatingShield.Value = false;
                ActivateShieldObjectRpc(false);
                _currentShieldStamina.Value = _maxShieldStamina;
                _shieldTemperature.Value = 0;
                _isOnCooldown.Value = false;
            }
            _isStaminaDepleted = false;
            _shield.SetActive(false);
        }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out bomb);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out melee);
            health.AttackPipeline.Register(this);
            health.OnHealthChanged += OnHealthChanged;
            if (IsServer)
            {
                _currentShieldStamina.Value = _maxShieldStamina;
            }
        }

        private void Start()
        {
            CreateShield();
        }

        public override void Apply(ref InputPayload @event)
        {
            if (!IsOwner) return;
            
            bool shieldInput = @event.shield;
            
            if(shieldInput && !_isShieldActive.Value) TryActivateShield();
            else if(!shieldInput && _isShieldActive.Value) TryDeactivateShield();
        }

        public override void UpdateFeature()
        {
            if(!IsServer && !IsOwner) return;
            
            TemperatureManagement();
            
            if(!IsOwner) return;
            
            if(_isShieldActive.Value || _isDeactivatingShield.Value) SetShieldAtHandle();
        }

        public override void FixedUpdateFeature() { }

        public void TryActivateShield()
        {
            bool canShieldInternal = !_isShieldActive.Value && !_isDeactivatingShield.Value && !_isOnCooldown.Value;
            bool canShieldExternal = !shoot.IsShooting && !shoot.IsReloading && !bomb.IsThrowing &&
                                     !health.IsStunned && !melee.IsAttacking;
            
            if (canShieldInternal && canShieldExternal)
            {
                ActivateShield();
                if (IsHost)
                {
                    _isShieldActive.Value = true;
                    _isDeactivatingShield.Value = false;
                }
                else ActivateShieldServerRpc(true, false);
            }
        }
    
        public void TryDeactivateShield()
        {
            if (IsHost)
            {
                _isShieldActive.Value = false;
                _isDeactivatingShield.Value = true;
            }
            else ActivateShieldServerRpc(false, true);
            
            Invoke(nameof(DeactivateShield), _deactivateShieldDelay);
        }
        
        public void ActivateShield()
        {
            SetShieldAtHandleAndActivate();
        }

        private void SetShieldAtHandle()
        {
            _invoker.GunTipPosition.Request(out var handlePosition);
            var handleDirection = InputReader.Instance.HandleDirection;
            
            _shield.transform.position = handlePosition;
            _shield.transform.right = handleDirection; 
            
            if(!_invoker.CenterPosition.Request(out var center).success) return;
            
            var relativeHandle = handlePosition - center;
            SendShieldPositionRpc(relativeHandle, handleDirection); 
        }
        
        private void SetShieldAtHandleAndActivate()
        {
            _shield.SetActive(true);
            
            _invoker.GunTipPosition.Request(out var handlePosition);
            var handleDirection = InputReader.Instance.HandleDirection;
            
            _shield.transform.position = handlePosition;
            _shield.transform.right = handleDirection; 
            
            if(!_invoker.CenterPosition.Request(out var center).success) return;
            
            var relativeHandle = handlePosition - center;
            
            ActivateShieldAndSendPositionRpc(relativeHandle, handleDirection); 
        } 
        
        public void DeactivateShield()
        {
            if (IsServer)
            {
                _isShieldActive.Value = false;
                _isDeactivatingShield.Value = false;
            }
            else ActivateShieldServerRpc(false, false);
            
            _shield.SetActive(false);
            ActivateShieldObjectRpc(false);
        }
        
        public void StaminaManagement()
        {
            if (_isShieldActive.Value)
            {
                if (_currentShieldStamina.Value > 0)
                {
                    if(IsHost) _currentShieldStamina.Value -= _shieldStaminaSpendRate * Time.deltaTime;
                    else ChangeStaminaServerRpc(-_shieldStaminaSpendRate * Time.deltaTime);
                }
                else
                {
                    _isStaminaDepleted = true;
                    TryDeactivateShield();
                }
            }
            else
            {
                if (_currentShieldStamina.Value < _maxShieldStamina)
                {
                    if(IsHost) _currentShieldStamina.Value += _shieldStaminaRegenRate * Time.deltaTime;
                    else ChangeStaminaServerRpc(_shieldStaminaRegenRate * Time.deltaTime);
                }
                else
                    _isStaminaDepleted = false;
            }
        }

        private void TemperatureManagement()
        {
            if(!IsServer) return;
            
            if (!_isShieldActive.Value || _isOnCooldown.Value)
            {
                if (_shieldTemperature.Value > 0)
                {
                    float dissipastion = _isOnCooldown.Value ? _cooldownRate : _heatDissipationRate;
                    _shieldTemperature.Value = Mathf.Max(_shieldTemperature.Value - dissipastion * Time.deltaTime, 0);
                }
                else if(_isOnCooldown.Value) _isOnCooldown.Value = false;
            }
        }

        private void SelfShieldExplosion()
        {
            if(!IsServer) return;

            _invoker.GunTipPosition.Request(out var gunTipPosition);
            _invoker.PlayerNumber.Request(out var playerNumber);
            
            AttackVFX(gunTipPosition, _selfAttackRadius);
            ReplicateVFXRpc(gunTipPosition, _selfAttackRadius); 
            
            health.Attack(new AttackEvent
            {
                DamagePercentage = _selfDamageInExplosion,
                KnockbackForce = _selfKnockbackInExplosion.x,
                KnockbackUpForce = _selfKnockbackInExplosion.y,
                SourcePosition = gunTipPosition,
                Success = true,
                SenderId = playerNumber,
                ReceiverId = playerNumber,
                Unblockeable = true
            });

            _isOnCooldown.Value = true;
            TryDeactivateShield();
        }

        private void AttackVFX(Vector3 position, float radius)
        {
            var go = ObjectPoolManager.Instance.Get(_selfAttackVfx, position, Quaternion.identity);
            go.SetActive(true);
            go.GetComponent<AttackVFX>().Init(radius);
        }
        
        [Rpc(SendTo.NotMe)]
        private void ReplicateVFXRpc(Vector3 position, float radius)
        {
            AttackVFX(position, radius);
        } 
        
        public void ShieldBash()
        {
            if (!IsServer && IsOwner)
            {
                RequestShieldBashOnServerRpc();
                return;
            }

            _isOnCooldown.Value = true;
            TryDeactivateShield();
        }

        [ServerRpc]
        private void RequestShieldBashOnServerRpc()
        {
            ShieldBash();
        }
        
        private void OnHealthChanged(object sender, OnHealthChangedEventArgs args)
        {
            if (_isShieldActive.Value)
            {
                DeactivateShield();
            }
        }

        public void Apply(ref AttackEvent @event)
        {
            if (!_isShieldActive.Value || @event.Unblockeable) return;
            
            var direction = InputReader.Instance.HandleDirection;
            var diff = @event.SourcePosition - InputReader.Instance.HandlePosition;
            var angle = Vector3.Angle(direction, diff);

            bool blocked = angle <= _shieldAngle;
            @event.Success = !blocked;

            if (blocked) HeatShield(@event.DamagePercentage);
            
            _invoker.PlayerNumber.Request(out int clientId);
            bool selfAttack = @event.SenderId == clientId;
            
            if(blocked && !selfAttack) bomb.AccelerateReload(GunBelt.Weapon.Shield);
        }

        public void HeatShield(float damage)
        {
            if(IsServer) HeatShieldAction(damage);
            else HeatShieldRequestToServerRpc(damage);
        }
        
        private void HeatShieldAction(float damagePercentage)
        {
            if(!IsServer) return;
            _shieldTemperature.Value = Mathf.Min(_shieldTemperature.Value + _heatPerDamagePercentage * damagePercentage * 100, _maxShieldTemperature);
            if (_shieldTemperature.Value >= _maxShieldTemperature && !_isOnCooldown.Value) SelfShieldExplosion();
        }

        [ServerRpc]
        private void HeatShieldRequestToServerRpc(float damagePercentage)
        {
            HeatShieldAction(damagePercentage);
        }

        [ServerRpc]
        private void ActivateShieldServerRpc(bool isShieldActive, bool isDeactivatingShield)
        {
            _isShieldActive.Value = isShieldActive;
            _isDeactivatingShield.Value = isDeactivatingShield;
        }

        [Rpc(SendTo.NotMe)]
        private void ActivateShieldObjectRpc(bool isShieldObjActive)
        {
            _shield.SetActive(isShieldObjActive);
        }

        [Rpc(SendTo.NotMe)]
        private void SendShieldPositionRpc(Vector3 handlePositionRelative, Vector3 handleDirection)
        {
            if(!_invoker.CenterPosition.Request(out var center).success)return;
            if(!_shield.activeSelf) return;
            
            _shield.transform.position = center + handlePositionRelative;
            _shield.transform.right = handleDirection;
        }

        [Rpc(SendTo.NotMe)]
        private void ActivateShieldAndSendPositionRpc(Vector3 handlePositionRelative, Vector3 handleDirection)
        {
            if(!_invoker.CenterPosition.Request(out var center).success)return;
            
            _shield.SetActive(true);
            _shield.transform.position = center + handlePositionRelative;
            _shield.transform.right = handleDirection;
        }

        [ServerRpc]
        private void ChangeStaminaServerRpc(float amount, bool relative = true)
        {
            if(relative) _currentShieldStamina.Value += amount;
            else _currentShieldStamina.Value = amount;
            
            _currentShieldStamina.Value = Mathf.Clamp(_currentShieldStamina.Value, 0, _maxShieldStamina);
        }

        private void CreateShield()
        {
            _shield = Instantiate(_shieldPrefab, transform.position, Quaternion.identity);
            _shield.GetComponent<ObjectShield>().Init(bomb, this);
            _shield.SetActive(false);
        }
    }
}
