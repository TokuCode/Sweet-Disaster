using System;
using Code.Gameplay.Character.Framework;
using Code.Helpers.Utils;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Gameplay.Character.Features
{
    public class Handle : NetworkBehaviour, Feature<PlayerController>
    {
        public enum Weapon
        {
            Gun,
            Bomb
        }
        
        private PlayerController _playerController;
        
        [Header("Settings")]
        [SerializeField] private float _handleDistance;
        [SerializeField] private float _handleHeight;
        [SerializeField] private float _switchCooldown;
        
        [Header("Runtime")]
        [SerializeField] private Vector3 _handlePosition;
        public Vector3 HandlePosition => _handlePosition;
        [SerializeField] private Vector3 _handleDirection;
        public Vector3 HandleDirection => _handleDirection;
        [SerializeField] private float _switchTimer;
        private NetworkVariable<Weapon> _currentWeapon = new (Weapon.Gun, NetworkVariableReadPermission.Owner);
        public Weapon CurrentWeapon => _currentWeapon.Value;

        [Header("Server Side")] 
        [SerializeField] private bool _serverSwitchRequested;
        
        [Header("Client Side")]
        [SerializeField] private Vector2 _mousePosition;
        [SerializeField] private Vector2 _clientHandlePosition;
        [SerializeField] private Vector2 _clientHandlerDirection;

        public override void OnNetworkSpawn()
        {
            if(!IsOwner) return;
            
            InputReader.Instance.OnAim += OnAim;
            InputReader.Instance.OnSwitch += OnSwitch;
        }
        
        public override void OnNetworkDespawn()
        {
            if(!IsOwner) return;
            
            InputReader.Instance.OnAim -= OnAim;
            InputReader.Instance.OnSwitch -= OnSwitch;
        }

        public void InitializeFeature(Controller<PlayerController> controller)
        {
            _playerController = (PlayerController)controller;
        }

        public void UpdateFeature()
        {
            if (IsOwner)
            {
                CalculateHandlePosition();
                SubmitClientHandleInfoServerRpc(_clientHandlerDirection, _clientHandlePosition);
            }
            
            if (IsServer)
            {
                if(_switchTimer > 0) _switchTimer -= Time.deltaTime;
                else if (_serverSwitchRequested) //Add Shield Check
                {
                    _switchTimer = _switchCooldown;
                    _serverSwitchRequested = false;
                    ChangeWeapon();
                }
            }
        }

        public void FixedUpdateFeature() { }

        public void OnSwitch(bool input)
        {
            RequestSwitchServerRpc(input);
        }

        public void OnAim(Vector2 input)
        {
            _mousePosition = input;
        }
        
        [ServerRpc]
        private void RequestSwitchServerRpc(bool input, ServerRpcParams serverRpcParams = default)
        {
            _serverSwitchRequested = input;
        }

        [ServerRpc]
        private void SubmitClientHandleInfoServerRpc(Vector3 handleDirection, Vector3 handlePosition, ServerRpcParams serverRpcParams = default)
        {
            _handleDirection = handleDirection;
            _handlePosition = handlePosition;
        }

        private void CalculateHandlePosition()
        {
            var playerPosition = _playerController.CenterPosition + Vector3.up * _handleHeight;
            var mousePositionWorld = CameraUtils.ScreenToWorldPoint(_mousePosition);
            
            _clientHandlerDirection = (mousePositionWorld - playerPosition).normalized;
            _clientHandlePosition = playerPosition + _handleDirection * _handleDistance;
        }
        
        private void ChangeWeapon()
        {
            int currentWeaponIndex = (int)_currentWeapon.Value;
            int nextWeaponIndex = (currentWeaponIndex + 1) % Enum.GetValues(typeof(Weapon)).Length;
            _currentWeapon.Value = (Weapon)nextWeaponIndex;
        }
        
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsOwner) return;
            
            var playerPosition = _playerController.CenterPosition + Vector3.up * _handleHeight;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerPosition, _handleDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_clientHandlePosition, .1f);
        }
    }
}