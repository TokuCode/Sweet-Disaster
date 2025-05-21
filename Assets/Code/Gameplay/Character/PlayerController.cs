using Code.Gameplay.Character.Features;
using Code.Gameplay.Character.Framework;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class PlayerController : Controller<PlayerController>
    {
        [Header("Features")]
        [SerializeField] private PhysicsCheck _physicsCheck;
        public bool IsGrounded => _physicsCheck.IsGrounded;
        public float LastTimeOnGround => _physicsCheck.LastTimeOnGround;
        public bool OnSlope => _physicsCheck.OnSlope;
        public bool HeadBlocked => _physicsCheck.HeadBlocked;
        public Vector2 ProjectOnSlope(Vector2 inputDirection) => _physicsCheck.ProjectOnSlopeDirection(inputDirection);
        [SerializeField] private Movement _movement;
        public bool IsMovementBlocked => _movement.IsMovementBlocked;
        public void BlockMovement() => _movement.BlockMovement();
        public void UnblockMovement() => _movement.UnblockMovement();
        [SerializeField] private Jump _jump;
        public bool OnDeparture => _jump.OnDeparture;
        [SerializeField] private Friction _friction;
        public bool IsTurning => _friction.IsTurning;
        public bool ApplyingFriction => _friction.ApplyingFriction;
        [SerializeField] private Crouch _crouching;
        public bool IsCrouching => _crouching.IsCrouching;
        public bool StartingCrouch => _crouching.StartingCrouch;
        [SerializeField] private Speed _speed;
        public float MaxSpeed => _speed.MaxSpeed;
        public float Acceleration => _speed.Acceleration;
        
        [Header("References")]
        [SerializeField] private Transform _transform;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private CapsuleCollider2D _capsule;

        public override void OnNetworkSpawn()
        {
            _features.Add(_physicsCheck);
            _features.Add(_movement);
            _features.Add(_jump);
            _features.Add(_friction);
            _features.Add(_crouching);
            _features.Add(_speed);
            
            base.OnNetworkSpawn();
        }
        
        public Vector3 CenterPosition => _transform.position + Vector3.up * _capsule.size.y / 2;
        public Vector3 LocalScale
        {
            get => _transform.localScale;
            set => _transform.localScale = value;
        }
        public Vector2 Size
        {
            get => _capsule.size;
            set => _capsule.size = value;
        }
        public Vector2 Velocity
        {
            get => _rigidbody.linearVelocity;
            set => _rigidbody.linearVelocity = value;
        }
        public float GravityScale
        {
            get => _rigidbody.gravityScale;
            set => _rigidbody.gravityScale = value;
        }
        
        public void AddForce(Vector2 force) => _rigidbody.AddForce(force);
        public void AddImpulse(Vector2 impulse) => _rigidbody.AddForce(impulse, ForceMode2D.Impulse);
    }
}