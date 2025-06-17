using System;
using Code.Helpers.Singleton;
using Code.Helpers.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Systems.Input
{
    public class InputReader: Singleton<InputReader>, IControl
    {
        [SerializeField] private float _handleHeight;
        [SerializeField] private float _handleDistance;
        private Vector3 _playerPosition;

        public event Action OnShootPressed;
        public event Action OnShootReleased;
        
        public float Move => inputActions.Gameplay.Move.ReadValue<float>();
        public bool Jump { get; private set; }
        public bool Crouch { get; private set; }
        public bool Shoot { get; private set; }
        public bool Reload { get; private set; }
        public bool Shield { get; private set; }
        public bool Switch => inputActions.Gameplay.Switch.ReadValue<Vector2>().y != 0;
        public Vector3 HandlePosition { get; private set; }
        public Vector3 HandleDirection { get; private set; }
        private Vector3 mousePosition;
        
        
        PlayerControls inputActions;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();
                inputActions.Enable();
                
                inputActions.Gameplay.Jump.performed += OnJump;
                inputActions.Gameplay.Jump.canceled += OnJump;
                inputActions.Gameplay.Crouch.performed += OnCrouch;
                inputActions.Gameplay.Crouch.canceled += OnCrouch;
                inputActions.Gameplay.Shoot.performed += OnShoot;
                inputActions.Gameplay.Shoot.canceled += OnShoot;
                inputActions.Gameplay.Reload.performed += OnReload;
                inputActions.Gameplay.Reload.canceled += OnReload;
                inputActions.Gameplay.Shield.performed += OnShield;
                inputActions.Gameplay.Shield.canceled += OnShield;
                inputActions.Gameplay.Aim.performed += OnAim;
                inputActions.Gameplay.Aim.canceled += OnAim;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.performed)
                Jump = true;
            else if (context.canceled)
                Jump = false;
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if(context.performed)
                Crouch = true;
            else if (context.canceled)
                Crouch = false;
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            mousePosition = context.ReadValue<Vector2>();
            CalculateHandle();
        }

        public void OnShoot(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnShootPressed?.Invoke(); 
                Shoot = true;
            }
            else if (context.canceled)
            {
                OnShootReleased?.Invoke();
                Shoot = false;
            }
        }

        public void OnReload(InputAction.CallbackContext context)
        {
            if(context.performed)
                Reload = true;
            else if (context.canceled)
                Reload = false;
        }

        public void OnShield(InputAction.CallbackContext context)
        {
            if(context.performed)
                Shield = true;
            else if (context.canceled)
                Shield = false;
        }

        public void CachePlayerPosition(Vector3 playerPosition)
        {
            _playerPosition = playerPosition;
            CalculateHandle();
        }
        
        void CalculateHandle()
        {
            var playerAimPosition = _playerPosition + Vector3.up * _handleHeight;
            var mousePositionWorld = CameraUtils.ScreenToWorldPoint(mousePosition);
            
            HandleDirection = (mousePositionWorld - playerAimPosition).normalized;
            HandlePosition = HandleDirection * _handleDistance + playerAimPosition;
        }
    }
}