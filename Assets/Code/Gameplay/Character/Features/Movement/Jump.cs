using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Jump : Feature
    {
        private Crouch crouch;
        private Health health;
        private PhysicsCheck check;
        
        [Header("Settings")] 
        [SerializeField] private float _jumpImpulse;
        [SerializeField] private float _jumpCooldown;
        [SerializeField] private float _coyoteTime;
        [SerializeField] private float _fallGravityMultiplier; 
        [SerializeField] private float _lowJumpGravityMultiplier;
        
        [Header("Runtime")]
        [SerializeField] private bool _onDeparture;
        public bool OnDeparture => _onDeparture;
        public bool CanJump(Crouch crouch, Health health) => !crouch.IsCrouching && !health.IsStunned;
        private bool _cachedJumpInput;
        private float _jumpCooldownTimer;

        public float JumpImpulse
        {
            get => _jumpImpulse;
            set => _jumpImpulse = value;
        }
        public float JumpCooldown 
        {
            get => _jumpCooldown;
            set => _jumpCooldown = value;
        }

        public float FallGravityMultiplier
        {
            get => _fallGravityMultiplier;
            set => _fallGravityMultiplier = value;
        }
        public float LowJumpGravityMultiplier
        {
            get => _lowJumpGravityMultiplier;
            set => _lowJumpGravityMultiplier = value;
        }

        public override void UpdateFeature()
        {
            if (!IsOwner && !IsServer) return;

            _dependencies.TryGetFeature(out check);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out crouch);
            
            if(_jumpCooldownTimer > 0) _jumpCooldownTimer -= Time.deltaTime;
            else if (check.IsGrounded) _onDeparture = false;
            
            _invoker.GravityScale.Perform(check.OnSlope ? 0f : 1f);
        }

        public override void FixedUpdateFeature()
        {
            if (!IsOwner && !IsServer) return;
            
            BetterServerJump();
        }

        public void TryServerJump()
        {

            bool canJump = CanJump(crouch, health);
            
            if(_jumpCooldownTimer > 0 || !canJump) return;
            
            float timeSinceGrounded = Time.time - check.LastTimeOnGround;
            if (timeSinceGrounded > _coyoteTime) return;

            JumpAction();
            _jumpCooldownTimer = _jumpCooldown;
            _onDeparture = true;
        }
        
        private void JumpAction()
        {
            float compensation = 0;
            if (_invoker.Velocity.Request(out var velocity).success)
            {
                if(velocity.y < 0 && !check.OnSlope) compensation = -velocity.y;
            }
            _invoker.AddForce.Perform(new(Vector2.up, _jumpImpulse + compensation, ForceMode2D.Impulse));
        }

        private void BetterServerJump()
        {
            if (!_onDeparture) return;
            
            if(!_invoker.Velocity.Request(out Vector2 velocity).success) return;

            if (velocity.y < 0)
                _invoker.AddForce.Perform(new(Vector2.up, Physics2D.gravity.y * (_fallGravityMultiplier - 1) * Time.fixedDeltaTime, ForceMode2D.Impulse));
            else if (velocity.y > 0 && !_cachedJumpInput)
                _invoker.AddForce.Perform(new(Vector2.up, Physics2D.gravity.y * (_lowJumpGravityMultiplier - 1) * Time.fixedDeltaTime, ForceMode2D.Impulse));
        }

        public override void Apply(ref InputPayload @event)
        {
            bool jumpRequested = @event.jump & !_cachedJumpInput;
            if (jumpRequested) TryServerJump();
                
            _cachedJumpInput = @event.jump;
        }
    }
}