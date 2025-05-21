using Code.Gameplay.Character.Framework;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Movement : NetworkBehaviour, Feature<PlayerController>
    {
        private PlayerController _playerController;
        
        [Header("Settings")]
        [SerializeField] private float _airMultiplier;
        
        [Header("Runtime")]
        [SerializeField] private bool _isMovementBlocked;
        public bool IsMovementBlocked => _isMovementBlocked;
        
        [Header("Server Side")]
        [SerializeField] private float _serverMoveDirection;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnMove += OnMove;
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnMove -= OnMove;
        }

        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
        }

        public void UpdateFeature() { }

        public void FixedUpdateFeature()
        {
            if (IsServer)
            {
                ApplyServerMovement();
                LimitServerMovement();
            }
        }

        private void OnMove(float input)
        {
            SubmitClientInputServerRpc(input);
        }

        [ServerRpc]
        private void SubmitClientInputServerRpc(float input, ServerRpcParams serverRpcParams = default)
        {
            _serverMoveDirection = input;
        }
        
        private void ApplyServerMovement()
        {
            if (_isMovementBlocked) return;

            if (Mathf.Abs(_serverMoveDirection) <= .1f) return;
            
            Vector2 direction = Vector2.right;
             if (_playerController && !_playerController.OnDeparture)
                direction = _playerController.ProjectOnSlope(direction);
            
            Vector2 movement = direction * (_serverMoveDirection * _playerController.Acceleration);
            float multiplier = _playerController.IsGrounded ? 1f : _airMultiplier;
            _playerController.AddForce(movement * multiplier);
        }
        
        private void LimitServerMovement()
        {
            float _maxSpeed = _playerController.MaxSpeed;
            if(_playerController.OnSlope && !_playerController.OnDeparture)
            {
                if (_playerController.Velocity.magnitude > _maxSpeed)
                    _playerController.Velocity = _playerController.Velocity.normalized * _maxSpeed;
                return;
            }
            
            if(Mathf.Abs(_playerController.Velocity.x) > _maxSpeed)
                _playerController.Velocity = new (Mathf.Sign(_playerController.Velocity.x) * _maxSpeed, _playerController.Velocity.y);
        }
        
        public void BlockMovement() => _isMovementBlocked = true;
        public void UnblockMovement() => _isMovementBlocked = false;
    }
}