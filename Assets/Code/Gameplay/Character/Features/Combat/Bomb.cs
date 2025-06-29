using System;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Bomb : Feature
    {
        private PlayerController _playerController;
        
        private Shoot shoot;
        private Health health;
        private Movement move;
        private Crouch crouch;
        private GunBelt belt;
        private Shield shield;
        
        [Header("Control")] 
        [SerializeField] private bool _active;
        public bool Active => _active;
        
        [Header("Throw Parameters")] 
        [SerializeField] private float _throwChargeTimeSeconds;
        public float ThrowChargeTimeSeconds => _throwChargeTimeSeconds;
        [SerializeField] private float _throwChargeTimer;
        public float ThrowChargeTimer => _throwChargeTimer;
        [SerializeField] private float _throwMinForce;
        [SerializeField] private float _throwMaxForce;
        [SerializeField] private bool _isThrowing;
        public bool IsThrowing => _isThrowing;
    
        [Header("Resource Management")] 
        [SerializeField] private float _cooldownTimeSeconds;
        [SerializeField] private float _cooldownTimer;
        [SerializeField] private bool _isOnCooldown;
        public bool IsOnCooldown => _isOnCooldown;
        [SerializeField] private int _startBombCount;
        private NetworkVariable<int> _bombCount = new (0, NetworkVariableReadPermission.Owner);
        public int BombCount => _bombCount.Value;
    
        [Header("Bomb Parameters")] 
        [SerializeField] private GameObject _bombPrefab;
        private NetworkObject _bombNo;

        public void OnShootPressed() => StartThrowing();
        public void OnShootReleased() => EndThrowing();
        
        public override void InitializeFeature(Controller controller)
        {
            _playerController = (PlayerController)controller;
            if (IsServer) _bombCount.Value = _startBombCount;
            if (IsOwner)
            {
                InputReader.Instance.OnShootPressed += OnShootPressed;
                InputReader.Instance.OnShootReleased += OnShootReleased;
            }
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out move);
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out belt);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out shield);
            health.OnStun += OnStun;
            RequestBombsAuthority();
        }
    
        public override void UpdateFeature()
        {
            if(!IsOwner && !IsServer) return;
            
            SetActive();
            
            if(_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;
            else if(_isOnCooldown) ResetThrow();
                
            if (_isThrowing) ThrowCharge();
        }

        public override void FixedUpdateFeature() { }

        private void SetActive()
        {
            _active = belt.ActiveWeapon == GunBelt.Weapon.Bomb;
        } 
    
        private void StartThrowing()
        {
            bool canThrowInternal = _bombCount.Value > 0 && !_isOnCooldown && !_isThrowing && _active;
            bool canThrowExternal = !shoot.IsShooting && !crouch.IsCrouching && !shoot.IsReloading && !health.IsStunned && !shield.IsShieldActive;
            if (canThrowInternal && canThrowExternal)
            {
                _isThrowing = true;
                _throwChargeTimer = 0f;
                if(IsServer) move.BlockMovement();
                else move.RequestMovement(true);
            }
        }

        public void OnStun(float duration, float healthRatio)
        {
            EndThrowing();
        }
        
        private void EndThrowing()
        {
            if (!_isThrowing) return;
            BombAction();
            if(IsHost) _bombCount.Value--;
            else RequestBombDepletionToServerRpc();
            
            _isThrowing = false;
            _cooldownTimer = _cooldownTimeSeconds;
            _isOnCooldown = true;
            
            if(IsServer) move.UnblockMovement();
            else move.RequestMovement(false);
        }
    
        private void ResetThrow() => _isOnCooldown = false;
    
        private void ThrowCharge()
        {
            if(_throwChargeTimer < _throwChargeTimeSeconds)
                _throwChargeTimer += Time.deltaTime;
            else 
                _throwChargeTimer = 0;
        }
        
        private void BombAction()
        {
            var direction = InputReader.Instance.HandleDirection;
            _invoker.GunTipPosition.Request(out var position);
            var throwForce = direction.normalized * Mathf.Lerp(_throwMinForce, _throwMaxForce, Mathf.Clamp01(_throwChargeTimer / _throwChargeTimeSeconds));
            
            ThrowAction(position, direction, throwForce, out int id);
            ReplicateThrowBombRpc(position, direction, throwForce, id, DateTime.Now);
        } 
        
        private void ThrowAction(Vector3 position, Vector3 direction, Vector3 throwForce, out int bombId)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bombNo = NonNetworkObjectPool.Singleton.GetNetworkObject(_bombPrefab, position, rotation, out bombId);
            
            var bomb = _bombNo.gameObject.GetComponent<ObjectBomb>();
            NonPooledSync.Singleton.AddBomb(bomb);
            bomb.Init(gameObject.tag, throwForce, bombId, 0);
        }

        private void ReplicateThrowAction(Vector3 position, Vector3 direction, Vector3 throwForce, int bombId, float latency)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bombNo = NonNetworkObjectPool.Singleton.GetNetworkObjectById(_bombPrefab, position, rotation, bombId);
            
            var bomb = _bombNo.gameObject.GetComponent<ObjectBomb>();
            NonPooledSync.Singleton.AddBomb(bomb);
            bomb.Init(gameObject.tag, throwForce, bombId, latency);
        }
        
        [ServerRpc]
        private void RequestBombDepletionToServerRpc()
        {
            _bombCount.Value--;
        } 
        
        [Rpc(SendTo.NotMe)]
        private void ReplicateThrowBombRpc(Vector3 position, Vector3 direction, Vector3 throwForce, int objectId, DateTime timestamp)
        {
            float latency = MilisecondsUtils.CalculateLatency(timestamp);
            ReplicateThrowAction(position, direction, throwForce, objectId, latency);
        } 
        
        public override void Apply(ref InputPayload @event) { }

        private void RequestBombsAuthority()
        {
            if(!IsOwner) return;

            var allBombs = NonNetworkObjectPool.Singleton.GetAllNetworkObjects(_bombPrefab);
            foreach (var bombNo in allBombs)
            {
                bombNo.RequestOwnership();
            }
        }
    }
}
