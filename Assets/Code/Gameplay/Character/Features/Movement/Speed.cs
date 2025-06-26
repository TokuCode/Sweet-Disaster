using System.Collections;
using Code.Gameplay.Character.Framework;
using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Speed : Feature
    {
        public enum MovementState
        {
            Idle,
            OnAir,
            Sliding,
            Crouching,
            Blocked,
            Stunned,
            Shield
        }

        private PhysicsCheck check;
        private Health health;
        private Movement move;
        private Crouch crouch;
        private Shield shield;
        
        [Header("Settings")]
        [SerializeField] private float _maxSpeedIdle;
        [SerializeField] private float _accelerationIdle;
        [SerializeField] private float _maxSpeedCrouching;
        [SerializeField] private float _accelerationCrouching;
        [SerializeField] private float _maxSpeedStunned;
        [SerializeField] private float _maxSpeedShield;
        [SerializeField] private float _accelerationShield;
        [SerializeField] private float _transitionTime;
        
        [Header("Runtime")]
        [SerializeField] private MovementState _movementState;
        [SerializeField] private float _desiredMaxSpeed;
        [SerializeField] private bool _enableTransition;
        [SerializeField] private float _maxSpeed;
        public float MaxSpeed => _maxSpeed;
        [SerializeField] private float _acceleration;
        public float Acceleration => _acceleration;

        public float AccelerationIdle
        {
            get => _accelerationIdle; 
            set => _accelerationIdle = value;
        }
        public float MaxSpeedIdle
        {
            get => _maxSpeedIdle;
            set => _maxSpeedIdle = value;
        }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out check);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out move);
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out shield);
        }

        public override void UpdateFeature()
        {
            if(!IsOwner && !IsServer) return;
            
            SpeedManagement();
        }

        public override void FixedUpdateFeature() { }

        private void SpeedManagement()
        {
            _enableTransition = false;

            bool isStunned = health.IsStunned;
            bool isMovementBlocked = move.IsMovementBlocked;
            bool isCrouching = crouch.IsCrouching;
            bool isGrounded = check.IsGrounded;
            bool onSlope = check.OnSlope;
            bool isShielding = shield.IsShieldActive;

            if (isStunned)
            {
                _movementState = MovementState.Stunned;
                _desiredMaxSpeed = _maxSpeedStunned;
            }
            else if (isMovementBlocked)
            {
                _movementState = MovementState.Blocked;
            }
            
            else if (isShielding)
            {
                _movementState = MovementState.Shield;
                _desiredMaxSpeed = _maxSpeedShield;
                _acceleration = _accelerationShield;
                _enableTransition = true;
            }
            
            else if(isCrouching)
            {
                _movementState = MovementState.Crouching;
                _desiredMaxSpeed = _maxSpeedCrouching;
                _acceleration = _accelerationCrouching;
                _enableTransition = true;
            }
            
            else if (onSlope)
            {
                _movementState = MovementState.Sliding;
                _desiredMaxSpeed = _maxSpeedIdle;
                _acceleration = _accelerationIdle;
                _enableTransition = true;
            }
            
            else if (isGrounded)
            {
                _movementState = MovementState.Idle;
                _desiredMaxSpeed = _maxSpeedIdle;
                _acceleration = _accelerationIdle;
                _enableTransition = true;
            }
            
            else
            {
                _movementState = MovementState.OnAir;
                _desiredMaxSpeed = _maxSpeedIdle;
                _acceleration = _accelerationIdle;
                _enableTransition = true;
            }

            if (Mathf.Abs(_desiredMaxSpeed - _maxSpeed) > .1f)
            {
                if (_enableTransition)
                {
                    StopAllCoroutines();
                    StartCoroutine(SpeedTransition());
                }
                else 
                {
                    _maxSpeed = _desiredMaxSpeed;
                }
            }
        }

        private IEnumerator SpeedTransition()
        {
            float time = 0;
            float startSpeed = _maxSpeed;
            while (time < _transitionTime)
            {
                time += Time.deltaTime;
                _maxSpeed = Mathf.Lerp(startSpeed, _desiredMaxSpeed, time / _transitionTime);
                yield return null;
            }
            
            _maxSpeed = _desiredMaxSpeed;
        }

        public override void Apply(ref InputPayload @event) { }
    }
}