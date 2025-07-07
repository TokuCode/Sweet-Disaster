using System;
using Code.Helpers.Singleton;
using Code.Helpers.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Systems.Input
{
    public class InputReader: Singleton<InputReader>, IControl, PlayerControls.IGameplayActions
    {
        private Camera _main;
        private bool control;
        private PlayerInput _playerInput;
        [SerializeField] private float _handleHeight;
        public float HandleHeight => _handleHeight;
        [SerializeField] private float _handleDistance;
        public float HandleDistance => _handleDistance;
        private Vector3 _playerPosition;

        public event Action OnShootPressed;
        public event Action OnShootReleased;
        
        public event Action OnThrowPressed;
        public event Action OnThrowReleased;
        
        public event Action OnMeleePressed;
        public event Action OnMeleeReleased;
        
        public event Action OnReloadPressed;
        
        public event Action OnShieldPressed;

        public event Action OnShieldBash;

        [SerializeField] private bool _onGamepad;
        public bool OnGamepad => _onGamepad;
        
        public float Move { get; private set; }
        public bool Jump { get; private set; }
        public bool Crouch { get; private set; }
        public bool Shoot { get; private set; }
        public bool Reload { get; private set; }
        public bool Shield { get; private set; }
        public bool Free { get; private set; }
        public bool Throw { get; private set; }
        public bool Melee { get; private set; }
        public Vector3 HandlePosition { get; private set; }
        public Vector3 HandleDirection { get; private set; }
        public void SetControl(bool control)
        {
            this.control = control;
            if(!control) ResetValues();
        }

        private void ResetValues()
        {
            Move = 0;
            Jump = false;
            Crouch = false;
            Shoot = false;
            Reload = false;
            Shield = false;
            Free = false;
            Throw = false;
            Melee = false;
        }

        private Vector3 pointerPosition;
        
        
        PlayerControls inputActions;

        protected override void Awake()
        {
            base.Awake();
            SetControl(true);
            _playerInput = GetComponent<PlayerInput>();
            _main = Camera.main;
        }

        private void OnEnable()
        {
            inputActions = new();
            inputActions.Enable();
            inputActions.Gameplay.SetCallbacks(this);
        }
        
        private void OnDisable()
        {
            inputActions.Dispose();
        }

        public void CheckControlScheme()
        {
            _onGamepad = _playerInput.currentControlScheme == inputActions.GamepadScheme.name;
        }

        private void Update()
        {
            CheckControlScheme();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            Move = context.ReadValue<float>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if(context.performed)
                Jump = true;
            else if (context.canceled)
                Jump = false;
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if(context.performed)
                Crouch = true;
            else if (context.canceled)
                Crouch = false;
        }

        public void OnAimPC(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if(_onGamepad) return;
            
            pointerPosition = context.ReadValue<Vector2>();
            CalculateHandleKeyboard();
        }

        public void OnAimGamepad(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if(!_onGamepad) return;
            
            pointerPosition = context.ReadValue<Vector2>();
            CalculateHandleGamepad();
        }

        public void OnShoot(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
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
            if (!control)
            {
                return;
            }

            if (context.performed)
            {
                OnReloadPressed?.Invoke();
                Reload = true;
            }
            else if (context.canceled)
                Reload = false;
        }

        public void OnThrow(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if (context.performed)
            {
                OnThrowPressed?.Invoke(); 
                Throw = true;
            }
            else if (context.canceled)
            {
                OnThrowReleased?.Invoke();
                Throw = false;
            }        
        }

        public void OnMelee(InputAction.CallbackContext context)
        {
            if (!control)
            {
                return;
            }
            
            if (context.performed)
            {
                OnMeleePressed?.Invoke(); 
                Melee = true;
            }
            else if (context.canceled)
            {
                OnMeleeReleased?.Invoke();
                Melee = false;
            }        
        }

        public void OnShield(InputAction.CallbackContext context)
        {
            if (!control) return;
            if (context.performed)
            {
                OnShieldPressed?.Invoke();
                Shield = true;
            }
            else if (context.canceled)
                Shield = false;
        }

        public void OnFreePlayer(InputAction.CallbackContext context)
        {
            if (!control) return;
            if(context.performed)
                Free = true;
            else if (context.canceled)
                Free = false;
        }

        void PlayerControls.IGameplayActions.OnShieldBash(InputAction.CallbackContext context)
        {
            if(!control) return;
            if (context.performed)
                OnShieldBash?.Invoke();
        }

        public void CachePlayerPosition(Vector3 playerPosition)
        {
            _playerPosition = playerPosition;
            CalculateHandle();
        }

        void CalculateHandle()
        {
            if(OnGamepad) CalculateHandleGamepad();
            else CalculateHandleKeyboard();
        }
        
        void CalculateHandleKeyboard()
        {
            if(OnGamepad) return;
            
            if(_main == null) return;
            
            var playerAimPosition = _playerPosition + Vector3.up * _handleHeight;
            var mousePositionWorld = CameraUtils.ScreenToWorldPoint(pointerPosition, _main);
            
            HandleDirection = (mousePositionWorld - playerAimPosition).normalized;
            if(HandleDirection == Vector3.zero) HandleDirection = Vector3.right;
            HandlePosition = HandleDirection * _handleDistance + playerAimPosition;
        }

        void CalculateHandleGamepad()
        {
            if(!OnGamepad) return;
            
            var playerAimPosition = _playerPosition + Vector3.up * _handleHeight;
            var directionActual = pointerPosition.normalized;
            if(directionActual != Vector3.zero) HandleDirection = directionActual;
            if(HandleDirection == Vector3.zero) HandleDirection = Vector3.right;
            HandlePosition = HandleDirection * _handleDistance + playerAimPosition;
        }
    }
}