using System.Collections;
using Code.Gameplay.Character.Framework;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Speed : NetworkBehaviour, Feature<PlayerController>
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
        
        private PlayerController _playerController;
        
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
        
        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
        }

        public void UpdateFeature()
        {
            if(IsServer)
                SpeedManagement();
        }

        public void FixedUpdateFeature() { }
        
        private void SpeedManagement()
        {
            _enableTransition = false;

            //Add Stun State

            if (_playerController.IsMovementBlocked)
            {
                _movementState = MovementState.Blocked;
            }
            
            else if(_playerController.IsCrouching)
            {
                _movementState = MovementState.Crouching;
                _desiredMaxSpeed = _maxSpeedCrouching;
                _acceleration = _accelerationCrouching;
                _enableTransition = true;
            }
            
            else if (_playerController.OnSlope)
            {
                _movementState = MovementState.Sliding;
                _desiredMaxSpeed = _maxSpeedIdle;
                _acceleration = _accelerationIdle;
                _enableTransition = true;
            }
            
            else if (_playerController.IsGrounded)
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
    }
}