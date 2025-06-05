using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Movement : Feature
    {
        [Header("Settings")]
        [SerializeField] private float _airMultiplier;
        
        [Header("Runtime")]
        [SerializeField] private NetworkVariable<bool> _isMovementBlocked = new (false, NetworkVariableReadPermission.Owner);
        public bool IsMovementBlocked => _isMovementBlocked.Value;

        public override void UpdateFeature() {}
        
        public override void FixedUpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            LimitMovement();
        }

        private void Move(float moveInput)
        {
            if (!_dependencies.TryGetFeature(out Speed speed)) return;
            float acceleration = speed.Acceleration;
            
            if (_isMovementBlocked.Value) return;

            if (Mathf.Abs(moveInput) <= .1f) return;

            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            
            bool onDeparture = false;
            if (_dependencies.TryGetFeature(out Jump jump))
            {
                onDeparture = jump.OnDeparture;
            }
            
            Vector2 direction = Vector2.right;
             if (check.OnSlope && !onDeparture)
                direction = check.ProjectOnSlopeDirection(direction);
            
            Vector2 movement = direction * (moveInput * acceleration);
            float multiplier = check.IsGrounded ? 1f : _airMultiplier;
            _invoker.AddForce.Perform(new(movement * multiplier, ForceMode2D.Force));
        }
        
        private void LimitMovement()
        {
            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            if (!_invoker.Velocity.Request(out Vector2 velocity).success) return;
            if (!_dependencies.TryGetFeature(out Speed speed)) return;

            float maxSpeed = speed.MaxSpeed;
            
            bool onDeparture = false;
            if (_dependencies.TryGetFeature(out Jump jump))
            {
                onDeparture = jump.OnDeparture;
            }
            
            if(check.OnSlope && !onDeparture)
            {
                if (velocity.magnitude > maxSpeed)
                    _invoker.Velocity.Perform(velocity.normalized * maxSpeed);
                return;
            }
            
            if(Mathf.Abs(velocity.x) > maxSpeed)
                _invoker.Velocity.Perform(new (Mathf.Sign(velocity.x) *maxSpeed, velocity.y));
        }
        
        public void BlockMovement() => _isMovementBlocked.Value = true;
        public void UnblockMovement() => _isMovementBlocked.Value = false;

        public override void Apply(ref InputPayload @event)
        {
            Move(@event.move);
        }
    }
}