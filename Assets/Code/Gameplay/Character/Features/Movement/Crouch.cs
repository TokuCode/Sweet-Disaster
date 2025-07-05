using Code.Gameplay.Character.Framework;
using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Crouch : Feature
    {
        private PhysicsCheck physics;
        private Jump jump;
        private Health health;
        
        [Header("Settings")]
        [SerializeField] private float _crouchHeightMultiplier;
        private float _initialYScale;
        private float _initialXSize;
        private float _initialYSize;
        
        [Header("Runtime")]
        [SerializeField] private bool _isCrouching;
        public bool IsCrouching => _isCrouching;
        public bool CanCrouch(PhysicsCheck check, Jump jump, Health health) => (check.IsGrounded || check.OnSlope) && !jump.OnDeparture && !health.IsStunned; 
        [SerializeField] private bool _startingCrouch;
        public bool StartingCrouch => _startingCrouch;

        public override void ResetFeature()
        {
            _startingCrouch = false;
            UncrouchAction();
        }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);

            _dependencies.TryGetFeature(out physics);
            _dependencies.TryGetFeature(out jump);
            _dependencies.TryGetFeature(out health);

            if (!_invoker.LocalScale.Request(out Vector3 localScale).success) return;
            if (!_invoker.Size.Request(out Vector2 size).success) return;
            
            _initialYScale = localScale.y;
            _initialXSize = size.x;
            _initialYSize = size.y;
        }

        public override void UpdateFeature()
        {
            if (!IsOwner && !IsServer) return;

            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
                
            if(_startingCrouch && check.IsGrounded) 
                _startingCrouch = false;
        }

        public override void FixedUpdateFeature() { }

        private void ManageCrouch(bool crouchInput)
        {
            bool canCrouch = CanCrouch(physics, jump, health);
            
            if (!_isCrouching && crouchInput && canCrouch)
            {
                CrouchAction();
            }
            else if(_isCrouching && !crouchInput && !physics.HeadBlocked)
            {
                UncrouchAction();
            }
            else if(_isCrouching && !canCrouch && !_startingCrouch && !physics.HeadBlocked)
            {
                UncrouchAction();
            }
        }

        private void CrouchAction()
        {
            if(!_invoker.LocalScale.Request(out Vector3 localScale).success) return;

            _startingCrouch = true;
            _isCrouching = true;
            
            _invoker.LocalScale.Perform(new(localScale.x, _initialYScale * _crouchHeightMultiplier));
            _invoker.Size.Perform(new(_initialXSize * _crouchHeightMultiplier, _initialYSize * _crouchHeightMultiplier));
        }
        
        private void UncrouchAction()
        {
            if(!_invoker.LocalScale.Request(out Vector3 localScale).success) return;
            
            _isCrouching = false;
            
            _invoker.LocalScale.Perform(new(localScale.x, _initialYScale));
            _invoker.Size.Perform(new(_initialXSize, _initialYSize));
        }

        public override void Apply(ref InputPayload @event)
        {
            ManageCrouch(@event.crouch);
        }
    }
}