using System;
using Code.Helpers.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Systems.Input
{
    public class InputReader : Singleton<InputReader>
    {
        private PlayerControls _controls;
        
        [Header("Input Parameters")]
        [SerializeField] private float _moveInput;
        public float MoveInput => _moveInput;
        [SerializeField] private bool _jumpInput;
        public bool JumpInput => _jumpInput;
        [SerializeField] private bool _crouchInput;
        public bool CrouchInput => _crouchInput;
        [SerializeField] private Vector2 _aimInput;
        public Vector2 AimInput => _aimInput;
        [SerializeField] private bool _shootInput;
        public bool ShootInput => _shootInput;
        [SerializeField] private bool _reloadInput;
        public bool ReloadInput => _reloadInput;
        [SerializeField] private bool _switchInput;
        public bool SwitchInput => _switchInput;
        [SerializeField] private bool _shieldInput;
        public bool ShieldInput => _shieldInput;

        public event Action<float> OnMove;
        public event Action OnJump;
        public event Action OnJumpReleased;
        public event Action OnCrouch;
        public event Action OnCrouchReleased;
        public event Action<Vector2> OnAim;
        public event Action<bool> OnSwitch;
        public event Action OnShoot;
        public event Action OnShootRelease;
        public event Action OnReload;

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            if(context.performed) 
                _moveInput = context.ReadValue<float>();
            else if(context.canceled)
                _moveInput = 0;
            
            OnMove?.Invoke(_moveInput);
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnJump?.Invoke();
                _jumpInput = true;
            }
            else if (context.canceled)
            {
                OnJumpReleased?.Invoke();
                _jumpInput = false;
            }
        }

        private void OnCrouchInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnCrouch?.Invoke();
                _crouchInput = true;
            }
            else if (context.canceled)
            {
                OnCrouchReleased?.Invoke();
                _crouchInput = false;
            }
        }

        private void OnAimInput(InputAction.CallbackContext context)
        {
            _aimInput = context.ReadValue<Vector2>();
            OnAim?.Invoke(_aimInput);
        }

        private void OnShootInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnShoot?.Invoke();
                _shootInput = true;
            }
            else if (context.canceled)
            {
                OnShootRelease?.Invoke();
                _shootInput = false;
            }
        }

        private void OnReloadInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnReload?.Invoke();
                _reloadInput = true;
            }
            else if (context.canceled)
                _reloadInput = false;
        }

        private void OnSwitchInput(InputAction.CallbackContext context)
        {
            if (context.performed)
                _switchInput = context.ReadValue<Vector2>().y != 0;
            else if (context.canceled)
                _switchInput = false;
            
            OnSwitch?.Invoke(_switchInput);
        }

        private void OnShieldInput(InputAction.CallbackContext context)
        {
            if (context.performed)
                _shieldInput = true;
            else if (context.canceled)
                _shieldInput = false;
        }

        private void Awake()
        {
            _controls = new();
            
            _controls.Gameplay.Move.performed += OnMoveInput;
            _controls.Gameplay.Move.canceled += OnMoveInput;
            _controls.Gameplay.Jump.performed += OnJumpInput;
            _controls.Gameplay.Jump.canceled += OnJumpInput;
            _controls.Gameplay.Crouch.performed += OnCrouchInput;
            _controls.Gameplay.Crouch.canceled += OnCrouchInput;
            _controls.Gameplay.Aim.performed += OnAimInput;
            _controls.Gameplay.Aim.canceled += OnAimInput;
            _controls.Gameplay.Shoot.performed += OnShootInput;
            _controls.Gameplay.Shoot.canceled += OnShootInput;
            _controls.Gameplay.Reload.performed += OnReloadInput;
            _controls.Gameplay.Reload.canceled += OnReloadInput;
            _controls.Gameplay.Switch.performed += OnSwitchInput;
            _controls.Gameplay.Switch.canceled += OnSwitchInput;
            _controls.Gameplay.Shield.performed += OnShieldInput;
            _controls.Gameplay.Shield.canceled += OnShieldInput;
        }

        private void OnEnable()
        {
            _controls.Enable();
        }

        private void OnDisable()
        {
            _controls.Disable();
        }
    }
}