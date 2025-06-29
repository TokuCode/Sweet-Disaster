using Code.Gameplay.Character.Framework;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Movement : Feature
    {
        private PhysicsCheck check;
        private Speed speed;
        private Jump jump;
        
        [Header("Settings")]
        [SerializeField] private float _airMultiplier;
        
        [Header("Runtime")]
        [SerializeField] private NetworkVariable<bool> _isMovementBlocked = new (false, NetworkVariableReadPermission.Owner);
        public bool IsMovementBlocked => _isMovementBlocked.Value;

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out check);
            _dependencies.TryGetFeature(out speed);
            _dependencies.TryGetFeature(out jump);
        }

        public override void UpdateFeature() {}
        
        public override void FixedUpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            LimitMovement();
        }

        private void Move(float moveInput)
        {
            float acceleration = speed.Acceleration;
            
            if (_isMovementBlocked.Value) return;

            if (Mathf.Abs(moveInput) <= .1f) return;

            bool onDeparture = jump.OnDeparture;
            
            Vector2 direction = Vector2.right;
             if (check.OnSlope && !onDeparture)
                direction = check.ProjectOnSlopeDirection(direction);
            
            Vector2 movement = direction * (moveInput * acceleration);
            float multiplier = check.IsGrounded ? 1f : _airMultiplier;
            _invoker.AddForce.Perform(new(movement * multiplier, ForceMode2D.Force));
        }
        
        private void LimitMovement()
        {
            if (!_invoker.Velocity.Request(out Vector2 velocity).success) return;

            float maxSpeed = speed.MaxSpeed;
            
            bool onDeparture = jump.OnDeparture;
            
            if(check.OnSlope && !onDeparture)
            {
                if (velocity.magnitude > maxSpeed)
                    _invoker.Velocity.Perform(velocity.normalized * maxSpeed);
                return;
            }
            
            if(Mathf.Abs(velocity.x) > maxSpeed)
                _invoker.Velocity.Perform(new (Mathf.Sign(velocity.x) * maxSpeed, velocity.y));
        }
        
        public void BlockMovement() => _isMovementBlocked.Value = true;
        public void UnblockMovement() => _isMovementBlocked.Value = false;
        public void RequestMovement(bool value) => RequestMovementServerRpc(value);
        
        [ServerRpc]
        private void RequestMovementServerRpc(bool value) => _isMovementBlocked.Value = value;

        public override void Apply(ref InputPayload @event)
        {
            Move(@event.move);
        }
    }
}