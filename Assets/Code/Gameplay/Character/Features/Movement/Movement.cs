using Code.Gameplay.Character.Command;
using Code.Gameplay.Character.Framework;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Movement : Feature
    {
        [Header("Settings")]
        [SerializeField] private float _airMultiplier;
        [SerializeField] private float _acceleration;
        [SerializeField] private float _maxSpeed;
        
        [Header("Runtime")]
        [SerializeField] private bool _isMovementBlocked;
        public bool IsMovementBlocked => _isMovementBlocked;

        public override void UpdateFeature() {}
        
        public override void FixedUpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            LimitMovement();
        }

        private void Move(float moveInput)
        {
            if (_isMovementBlocked) return;

            if (Mathf.Abs(moveInput) <= .1f) return;

            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            
            Vector2 direction = Vector2.right;
             if (check.OnSlope) //TODO Add Departure Check
                direction = check.ProjectOnSlopeDirection(direction);
            
            Vector2 movement = direction * (moveInput * _acceleration);
            float multiplier = check.IsGrounded ? 1f : _airMultiplier;
            _invoker.AddForce.Perform(new(movement * multiplier, ForceMode2D.Force));
        }
        
        private void LimitMovement()
        {
            if (!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            if (!_invoker.Velocity.Request(out Vector2 velocity).success) return;
            
            if(check.OnSlope) //TODO Add Departure Check
            {
                if (velocity.magnitude > _maxSpeed)
                    _invoker.Velocity.Perform(velocity.normalized * _maxSpeed);
                return;
            }
            
            if(Mathf.Abs(velocity.x) > _maxSpeed)
                _invoker.Velocity.Perform(new (Mathf.Sign(velocity.x) * _maxSpeed, velocity.y));
        }
        
        public void BlockMovement() => _isMovementBlocked = true;
        public void UnblockMovement() => _isMovementBlocked = false;

        public override void Apply(ref InputPayload @event)
        {
            Move(@event.moveInput);
        }
    }
}