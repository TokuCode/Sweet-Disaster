using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Friction : Feature
    {
        [Header("Settings")]
        [SerializeField] private float _groundFriction;
        [SerializeField] private float _airFriction;
        
        [Header("Runtime")]
        [SerializeField] private bool _applyingFriction;
        public bool ApplyingFriction => _applyingFriction;
        private float _cachedMoveDirection;
        public bool IsTurning(Vector2 velocity) => _cachedMoveDirection > 0 && velocity.x < 0 ||
                                  _cachedMoveDirection < 0 && velocity.x > 0;

        public override void UpdateFeature() { }

        public override void FixedUpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            ManageFriction();
        }

        private void ManageFriction()
        {
            _applyingFriction = false;

            if(!_dependencies.TryGetFeature(out PhysicsCheck check)) return;
            if(!_invoker.Velocity.Request(out Vector2 velocity).success) return;

            if(!_dependencies.TryGetFeature(out Health health)) return;
            if(health.IsStunned) return;
            
            if(!check.IsGrounded)
                ApplyFriction(_airFriction, velocity);
            else if(IsTurning(velocity) || _cachedMoveDirection == 0)
                ApplyFriction(_groundFriction, velocity);
        }

        private void ApplyFriction(float friction, Vector2 velocity)
        {
            _invoker.AddForce.Perform(new (-Vector2.right, velocity.x * friction * Time.fixedDeltaTime, ForceMode2D.Impulse));
            _applyingFriction = true;
        }

        public override void Apply(ref InputPayload @event)
        {
            _cachedMoveDirection = @event.move;
        }
    }
}