using Code.Gameplay.Character.Framework;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Crouch : NetworkBehaviour, Feature<PlayerController>
    {
        private PlayerController _playerController;
        
        [Header("Settings")]
        [SerializeField] private float _crouchHeightMultiplier;
        private float _initialYScale;
        private float _initialXSize;
        private float _initialYSize;
        
        [Header("Runtime")]
        [SerializeField] private bool _isCrouching;
        public bool IsCrouching => _isCrouching;
        public bool CanCrouch => _playerController.IsGrounded && !_playerController.OnDeparture; //Add Stun Check
        [SerializeField] private bool _startingCrouch;
        public bool StartingCrouch => _startingCrouch;
        
        [Header("Server Side")]
        [SerializeField] private bool _serverCrouchInput;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnCrouch += OnCrouch;
            InputReader.Instance.OnCrouchReleased += OnCrouchReleased;
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnCrouch -= OnCrouch;
            InputReader.Instance.OnCrouchReleased -= OnCrouchReleased;
        }

        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
            _initialYScale = _playerController.LocalScale.y;
            _initialXSize = _playerController.Size.x;
            _initialYSize = _playerController.Size.y;
        }

        public void UpdateFeature()
        {
            if (IsServer)
            {
                if(_startingCrouch && _playerController.IsGrounded)
                    _startingCrouch = false;
                
                ManageCrouch();
            }
        }

        public void FixedUpdateFeature() { }

        private void OnCrouch()
        {
            SubmitClientCrouchInputServerRpc(true);
        }

        private void OnCrouchReleased()
        {
            SubmitClientCrouchInputServerRpc(false);
        }
        
        [ServerRpc]
        private void SubmitClientCrouchInputServerRpc(bool crouchInput, ServerRpcParams serverRpcParams = default)
        {
            _serverCrouchInput = crouchInput;
        }

        private void ManageCrouch()
        {
            if (!_isCrouching && _serverCrouchInput && CanCrouch)
            {
                _isCrouching = true;
                _startingCrouch = true;
                CrouchAction();
            }
            else if(_isCrouching && !_serverCrouchInput && !_playerController.HeadBlocked)
            {
                _isCrouching = false;
                UncrouchAction();
            }
            else if(_isCrouching && !CanCrouch && !_startingCrouch && !_playerController.HeadBlocked)
            {
                _isCrouching = false;
                UncrouchAction();
            }
        }

        private void CrouchAction()
        {
            _playerController.LocalScale =
                new(_playerController.LocalScale.x, _initialYScale * _crouchHeightMultiplier);
            _playerController.Size =
                new(_initialXSize * _crouchHeightMultiplier, _initialYSize * _crouchHeightMultiplier);
        }
        
        private void UncrouchAction()
        {
            _playerController.LocalScale = new(_playerController.LocalScale.x, _initialYScale);
            _playerController.Size = new(_initialXSize, _initialYSize);
        }
    }
}