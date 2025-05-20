using Code.Gameplay.Character.Framework;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Jump : NetworkBehaviour, Feature<PlayerController>
    {
        private PlayerController _playerController;
        
        [Header("Settings")] 
        [SerializeField] private float _jumpImpulse;
        [SerializeField] private float _jumpCooldown;
        [SerializeField] private float _coyoteTime;
        [SerializeField] private float _fallGravityMultiplier; 
        [SerializeField] private float _lowJumpGravityMultiplier;
        
        [Header("Runtime")]
        [SerializeField] private bool _onDeparture;
        public bool OnDeparture => _onDeparture;
        public bool CanJump => !_playerController.IsCrouching; //Add Shield, Stun, Throw Checks

        [Header("Server Side")] 
        [SerializeField] private bool _serverJumpHold;
        [SerializeField] private bool _serverJumpRequested;
        private NetworkVariable<float> _jumpCooldownTimer = new (0f, NetworkVariableReadPermission.Owner);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnJump += OnJump;
            InputReader.Instance.OnJumpReleased += OnJumpReleased;
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            InputReader.Instance.OnJump -= OnJump;
            InputReader.Instance.OnJumpReleased -= OnJumpReleased;
        }

        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
        }

        public void UpdateFeature()
        {
            if (IsServer)
            {
                if(_jumpCooldownTimer.Value > 0) _jumpCooldownTimer.Value -= Time.deltaTime;
                else if (_playerController.IsGrounded) _onDeparture = false;
                
                _playerController.GravityScale = _playerController.OnSlope ? 0f : 1f;

                if (_serverJumpRequested)
                {
                    _serverJumpRequested = false;
                    TryServerJump();
                }
            }
        }

        public void FixedUpdateFeature()
        {
            if (IsServer)
            {
                BetterServerJump();
            }
        }

        private void OnJump()
        {
            SubmitClientJumpRequestServerRpc();
            SubmitClientJumpHoldServerRpc(true);
        }

        private void OnJumpReleased()
        {
            SubmitClientJumpHoldServerRpc(false);
        }

        [ServerRpc]
        private void SubmitClientJumpRequestServerRpc(ServerRpcParams serverRpcParams = default)
        {
            _serverJumpRequested = true;
        }
        
        [ServerRpc]
        private void SubmitClientJumpHoldServerRpc(bool input, ServerRpcParams serverRpcParams = default)
        {
            _serverJumpHold = input;
        }

        private void TryServerJump()
        {
            if(_jumpCooldownTimer.Value > 0 || !CanJump) return;
            
            float timeSinceGrounded = Time.time - _playerController.LastTimeOnGround;
            if (timeSinceGrounded > _coyoteTime) return;

            JumpAction();
            _jumpCooldownTimer.Value = _jumpCooldown;
            _onDeparture = true;
        }
        
        private void JumpAction() => _playerController.AddImpulse(Vector2.up * _jumpImpulse);

        private void BetterServerJump()
        {
            if (_playerController.IsGrounded && !_playerController.OnSlope) return;

            if (_playerController.Velocity.y < 0)
                _playerController.AddImpulse(Vector2.up *
                                             (Physics2D.gravity.y * (_fallGravityMultiplier - 1) *
                                              Time.fixedDeltaTime));
            else if (_playerController.Velocity.y > 0 && !_serverJumpHold)
                _playerController.AddImpulse(Vector2.up *
                                             (Physics2D.gravity.y * (_lowJumpGravityMultiplier - 1) *
                                              Time.fixedDeltaTime));
        }
    }
}