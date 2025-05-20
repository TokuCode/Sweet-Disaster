using Code.Gameplay.Character.Framework;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Friction : NetworkBehaviour, Feature<PlayerController>
    {
        private PlayerController _playerController;
        
        [Header("Settings")]
        [SerializeField] private float _groundFriction;
        [SerializeField] private float _airFriction;
        
        [Header("Runtime")]
        [SerializeField] private bool _applyingFriction;
        public bool ApplyingFriction => _applyingFriction;

        [Header("Server Side")] 
        [SerializeField] private float _serverMoveDirection;
        public bool IsTurning => _serverMoveDirection > 0 && _playerController.Velocity.x < 0 ||
                                  _serverMoveDirection < 0 && _playerController.Velocity.x > 0;

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
                ManageFriction();
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
        
        private void ManageFriction()
        {
            _applyingFriction = false;

            //Add Stun Check
            
            if(!_playerController.IsGrounded)
                ApplyFriction(_airFriction);
            else if(IsTurning || _serverMoveDirection == 0)
                ApplyFriction(_groundFriction);
        }

        private void ApplyFriction(float friction)
        {
            var velocity = _playerController.Velocity.x;
            _playerController.AddImpulse(-Vector2.right * (velocity * friction * Time.fixedDeltaTime));
            _applyingFriction = true;
        }
    }
}