using System;
using Code.Gameplay.Character.Framework;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Shield : Feature, IProcess<AttackEvent>
    {
        private Crouch crouch;
        private Health health;
        private Shoot shoot;
        private Bomb bomb;
        private Movement movement;
        
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
        
        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out movement);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out bomb);
            _dependencies.TryGetFeature(out health);
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
            if(!IsOwner) return;
            
            if(_isShieldActive.Value || _isDeactivatingShield.Value) SetShieldAtHandle();
            StaminaManagement();
        }

        public override void FixedUpdateFeature() { }

        public void TryActivateShield()
        {
            bool canShieldInternal = !_isStaminaDepleted && !_isShieldActive.Value && _currentShieldStamina.Value > _minShieldStaminaForActivation && !_isDeactivatingShield.Value;
            bool canShieldExternal = !shoot.IsShooting && !shoot.IsReloading &&
                                     !crouch.IsCrouching && !bomb.IsThrowing &&
                                     !health.IsStunned;
            
            if (canShieldInternal && canShieldExternal)
            {
                ActivateShield();
                if (IsHost)
                {
                    _isShieldActive.Value = true;
                    _isDeactivatingShield.Value = false;
                }
                else ActivateShieldServerRpc(true, false); }
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
            if(IsHost)movement.BlockMovement();
            else movement.RequestMovement(true);
        }

        private void SetShieldAtHandle()
        {
            var handlePosition = InputReader.Instance.HandlePosition;
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
            
            var handlePosition = InputReader.Instance.HandlePosition;
            var handleDirection = InputReader.Instance.HandleDirection;
            
            _shield.transform.position = handlePosition;
            _shield.transform.right = handleDirection; 
            
            if(!_invoker.CenterPosition.Request(out var center).success) return;
            
            var relativeHandle = handlePosition - center;
            
            ActivateShieldAndSendPositionRpc(relativeHandle, handleDirection); 
        } 
        
        public void DeactivateShield()
        {
            if (IsHost)
            {
                _isShieldActive.Value = false;
                _isDeactivatingShield.Value = false;
            }
            else ActivateShieldServerRpc(false, false);
            
            _shield.SetActive(false);
            ActivateShieldObjectRpc(false);
            
            if (!health.IsStunned)
            {
                if(IsHost) movement.UnblockMovement();
                else movement.RequestMovement(false);
            }
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
    
        private void OnHealthChanged(object sender, OnHealthChangedEventArgs args)
        {
            if (_isShieldActive.Value)
            {
                DeactivateShield();
            }
        }

        public void Apply(ref AttackEvent @event)
        {
            if (!_isShieldActive.Value) return;
            
            var direction = InputReader.Instance.HandleDirection;
            var diff = @event.SourcePosition - InputReader.Instance.HandlePosition;
            var angle = Vector3.Angle(direction, diff);
            
            @event.Success = angle > _shieldAngle; 
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
            _shield.SetActive(false);
        }
    }
}
