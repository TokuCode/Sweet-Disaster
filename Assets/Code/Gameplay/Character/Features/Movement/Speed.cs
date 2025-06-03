using System.Collections;
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
            Stunned
        }
        
        [Header("Settings")]
        [SerializeField] private float _maxSpeedIdle;
        [SerializeField] private float _accelerationIdle;
        [SerializeField] private float _maxSpeedCrouching;
        [SerializeField] private float _accelerationCrouching;
        [SerializeField] private float _maxSpeedStunned;
        [SerializeField] private float _transitionTime;
        
        [Header("Runtime")]
        [SerializeField] private MovementState _movementState;
        [SerializeField] private float _desiredMaxSpeed;
        [SerializeField] private bool _enableTransition;
        [SerializeField] private float _maxSpeed;
        public float MaxSpeed => _maxSpeed;
        [SerializeField] private float _acceleration;
        public float Acceleration => _acceleration;

        public override void UpdateFeature()
        {
            if(!IsOwner && !IsServer) return;
            
            SpeedManagement();
        }

        public override void FixedUpdateFeature() { }

        private void SpeedManagement()
        {
            _enableTransition = false;

            //TODO Add Stun State
            
            bool isMovementBlocked = false;
            if (_dependencies.TryGetFeature(out Movement movement))
            {
                isMovementBlocked = movement.IsMovementBlocked;
            }
            
            bool isCrouching = false;
            if (_dependencies.TryGetFeature(out Crouch crouch))
            {
                isCrouching = crouch.IsCrouching;
            }

            bool isGrounded = true;
            bool onSlope = false;
            if (_dependencies.TryGetFeature(out PhysicsCheck check))
            {
                isGrounded = check.IsGrounded;
                onSlope = check.OnSlope;
            }

            if (isMovementBlocked)
            {
                _movementState = MovementState.Blocked;
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