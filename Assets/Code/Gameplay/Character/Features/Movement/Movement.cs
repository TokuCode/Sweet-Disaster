using Code.Gameplay.Character.Command;
using Code.Gameplay.Character.Framework;
using Code.Helpers;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Gameplay.Character.Features
{
    public class Movement : Feature
    {
        private const float baseSnapAngle = 45f;
        
        private PhysicsCheck check;
        private Speed speed;
        private Jump jump;
        
        [Header("Settings")]
        [SerializeField] private float _airMultiplier;
        
        [Header("Snap To Ground")]
        [SerializeField] private float _snapToGroundForce;
        [SerializeField] private float _boostSlopeForce;

        [Header("Runtime")]
        [SerializeField] private NetworkVariable<bool> _isMovementBlocked = new (false, NetworkVariableReadPermission.Owner);
        public bool IsMovementBlocked => _isMovementBlocked.Value;

        public override void ResetFeature()
        {
            if (IsServer)
            {
                _isMovementBlocked.Value = false;
            }
        }

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
            TurnMovement();
        }

        private void TurnMovement()
        {
            if (check.SlopeNormal == check.PreviousSlopeNormal) return;
            
            _invoker.Velocity.Request(out Vector2 velocity);
            float turnSign = Mathf.Sign(DirectionAngle(check.PreviousSlopeNormal, check.SlopeNormal));
            float xVelocitySign = Mathf.Sign(velocity.x);
            float snap = Vector3.Angle(check.PreviousSlopeNormal, check.SlopeNormal) * _snapToGroundForce / baseSnapAngle; 
            float boost = Vector3.Angle(check.PreviousSlopeNormal, check.SlopeNormal) * _boostSlopeForce / baseSnapAngle;
            Vector2 force = Vector2.down * (xVelocitySign * turnSign * snap) + Vector2.right * (xVelocitySign * boost);
            _invoker.AddForce.Perform(new(force, ForceMode2D.Impulse));
        }

        private float DirectionAngle(Vector2 from, Vector2 to)
        {
            float angleFrom = -Vector2.SignedAngle(Vector2.up, from);
            float angleTo = -Vector2.SignedAngle(Vector2.up, to);
            return angleTo - angleFrom;
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
             
            Vector2 movement = direction.normalized * (moveInput * acceleration);
            float multiplier = check.IsGrounded || check.OnSlope ? 1f : _airMultiplier;
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