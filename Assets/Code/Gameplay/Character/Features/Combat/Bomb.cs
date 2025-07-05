using System;
using System.Runtime.Remoting.Messaging;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Bomb : Feature
    {
        private Shoot shoot;
        private Health health;
        private Movement move;
        private Crouch crouch;
        private Shield shield;
        private Melee melee;
        private WillToLive will;
        
        [Header("Throw Parameters")] 
        [SerializeField] private float _throwChargeTimeSeconds;
        public float ThrowChargeTimeSeconds => _throwChargeTimeSeconds;
        [SerializeField] private float _throwChargeTimer;
        public float ThrowChargeTimer => _throwChargeTimer;
        [SerializeField] private float _throwMinForce;
        [SerializeField] private float _throwMaxForce;
        [SerializeField] private bool _isThrowing;
        public bool IsThrowing => _isThrowing;
        [SerializeField] private float _timeOnMaxThrowCharge;
        private float _timerOnMaxThrowCharge;
    
        [Header("Resource Management")] 
        [SerializeField] private float _cooldownTimeSeconds;
        [SerializeField] private float _cooldownTimer;
        [SerializeField] private bool _isOnCooldown;
        public bool IsOnCooldown => _isOnCooldown;
        [SerializeField] private int _startBombCount;
        private NetworkVariable<int> _bombCount = new (0, NetworkVariableReadPermission.Owner);
        public int BombCount => _bombCount.Value;
        private bool _bombsRequested;
    
        [Header("Bomb Parameters")] 
        [SerializeField] private GameObject _bombPrefab;
        private NetworkObject _bombNo;

        [Header("Bomb reload")] 
        [SerializeField] private float _bombReloadTime;
        [SerializeField] private float _minHorizontalSpeedToReload;
        private CountdownTimer _bombReloadTimer;
        public float BombReloadProgress => 1 - _bombReloadTimer.Progress;
        [SerializeField] private float _onBulletHitAccelerateTime;
        [SerializeField] private float _onBombHitAccelerateTime;
        [SerializeField] private float _onShieldHitAccelerateTime;
        [SerializeField] private float _onMeleeHitAccelerateTime;
        [SerializeField] private float _onWillToLiveHitAccelerateTime;
        [SerializeField] private float _onActiveReloadHitAccelerateTime;

        public void OnShootPressed() => StartThrowing();
        public void OnShootReleased() => EndThrowing();

        public override void ResetFeature()
        {
            CancelThrowing();
            _isOnCooldown = false;
            if (IsServer)
            {
                _bombCount.Value = _startBombCount;
            }

            if (IsOwner)
            {
                _bombReloadTimer.Reset();
            }
        }

        public override void InitializeFeature(Controller controller)
        {
            if (IsServer) _bombCount.Value = _startBombCount;
            if (IsOwner)
            {
                InputReader.Instance.OnThrowPressed += OnShootPressed;
                InputReader.Instance.OnThrowReleased += OnShootReleased;
                InputReader.Instance.OnShootPressed += CancelThrowing;
            }
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out health);
            _dependencies.TryGetFeature(out move);
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out shield);
            _dependencies.TryGetFeature(out melee);
            _dependencies.TryGetFeature(out will);
            health.OnStun += OnStun;
            will.OnMinigameSucces += () => AccelerateReload(GunBelt.Weapon.Will);
            shoot.OnActiveReload += () => AccelerateReload(GunBelt.Weapon.Reloading);
            AttackBus.Singleton.Event += OnAttackGlobal;
            _bombReloadTimer = new(_bombReloadTime);
            _bombReloadTimer.OnTimerStop += ReloadBomb; 
            _bombReloadTimer.Start();
        }
    
        public override void UpdateFeature()
        {
            if(!IsOwner && !IsServer) return;
            
            if(_cooldownTimer > 0) _cooldownTimer -= Time.deltaTime;
            else if(_isOnCooldown) ResetThrow();
                
            if (_isThrowing) ThrowCharge();

            if (!IsOwner) return;
            
            if(!_bombsRequested) RequestBombsAuthority();
            
            ReloadBombHandler(Time.deltaTime);
        }

        private void ReloadBombHandler(float deltaTime)
        {
            _invoker.Velocity.Request(out var velocity);

            if (Mathf.Abs(velocity.x) < _minHorizontalSpeedToReload || health.IsStunned) return;
            
            _bombReloadTimer.Tick(deltaTime);
        }

        public override void FixedUpdateFeature() { }
    
        private void StartThrowing()
        {
            bool canThrowInternal = _bombCount.Value > 0 && !_isOnCooldown && !_isThrowing;
            bool canThrowExternal = !shoot.IsShooting && !crouch.IsCrouching && !shoot.IsReloading && !health.IsStunned && !shield.IsShieldActive && !melee.IsAttacking;
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

        private void CancelThrowing()
        {
            if (!_isThrowing) return;
            
            _isThrowing = false;
            
            if(IsServer) move.UnblockMovement();
            else move.RequestMovement(false);  
        }
    
        private void ResetThrow() => _isOnCooldown = false;
    
        private void ThrowCharge()
        {
            if (_throwChargeTimer < _throwChargeTimeSeconds)
            {
                _throwChargeTimer += Time.deltaTime;
                _timerOnMaxThrowCharge = _timeOnMaxThrowCharge;
            }
            else
            {
                _throwChargeTimer = _throwChargeTimeSeconds;
                if(_timerOnMaxThrowCharge > 0) _timerOnMaxThrowCharge -= Time.deltaTime;
                else
                {
                    _throwChargeTimer = 0;
                }
            }
        }
        
        private void BombAction()
        {
            var direction = InputReader.Instance.HandleDirection;
            _invoker.GunTipPosition.Request(out var position);
            var throwForce = direction.normalized * Mathf.Lerp(_throwMinForce, _throwMaxForce, Mathf.Clamp01(_throwChargeTimer / _throwChargeTimeSeconds));
            
            ThrowAction(position, direction, throwForce, out int id);
            _invoker.PlayerNumber.Request(out int clientId);
            ReplicateThrowBombRpc(position, direction, throwForce, id, DateTime.Now, clientId);
        } 
        
        private void ThrowAction(Vector3 position, Vector3 direction, Vector3 throwForce, out int bombId)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bombNo = NonNetworkObjectPool.Singleton.GetNetworkObject(_bombPrefab, position, rotation, out bombId);
            int absBombId = NonNetworkObjectPool.Singleton.GetAbsoluteId(_bombPrefab, bombId);
            
            var bomb = _bombNo.gameObject.GetComponent<ObjectBomb>();
            NonPooledSync.Singleton.AddBomb(bomb);
            _invoker.PlayerNumber.Request(out int clientId);
            bomb.Init(gameObject.tag, throwForce, absBombId, 0, clientId);
        }

        private void ReplicateThrowAction(Vector3 position, Vector3 direction, Vector3 throwForce, int bombId, float latency, int senderId)
        {
            var rotation = DirectionToRotation.GetRotation(direction);
            
            _bombNo = NonNetworkObjectPool.Singleton.GetNetworkObjectById(_bombPrefab, position, rotation, bombId, senderId);
            int absBombId = NonNetworkObjectPool.Singleton.GetAbsoluteId(_bombPrefab, bombId, senderId);
            
            var bomb = _bombNo.gameObject.GetComponent<ObjectBomb>();
            NonPooledSync.Singleton.AddBomb(bomb);
            bomb.Init(gameObject.tag, throwForce, absBombId, latency, senderId);
        }
        
        [ServerRpc]
        private void RequestBombDepletionToServerRpc()
        {
            _bombCount.Value--;
        }

        [ServerRpc]
        private void RequestBombReloadToServerRpc()
        {
            _bombCount.Value++;
        }

        private void ReloadBomb()
        {
            if (IsHost) _bombCount.Value++;
            else RequestBombReloadToServerRpc();
            _bombReloadTimer.Start();
        }

        public void AccelerateReload(GunBelt.Weapon weapon)
        {
            float accelerateTime = weapon switch
            {
                GunBelt.Weapon.Gun => _onBulletHitAccelerateTime,
                GunBelt.Weapon.Bomb => _onBombHitAccelerateTime,
                GunBelt.Weapon.Shield => _onShieldHitAccelerateTime,
                GunBelt.Weapon.Melee => _onMeleeHitAccelerateTime,
                GunBelt.Weapon.Will => _onWillToLiveHitAccelerateTime,
                GunBelt.Weapon.Reloading => _onActiveReloadHitAccelerateTime,
                _ => 0f
            };
            
            if(accelerateTime > 0f) _bombReloadTimer.Tick(accelerateTime);
        }

        public void RequestBlockReloadAccelerate(GunBelt.Weapon weapon, int attackerId)
        {
            if(IsOwner) BlockReloadAccelerate(weapon, attackerId);
            else RequestBlockReloadAccelerateRpc((int)weapon, attackerId);
        }

        [Rpc(SendTo.Owner)]
        private void RequestBlockReloadAccelerateRpc(int weapon, int attackerId)
        {
            BlockReloadAccelerate((GunBelt.Weapon)weapon, attackerId);
        }

        public void BlockReloadAccelerate(GunBelt.Weapon weapon, int attackerId)
        {
            _invoker.PlayerNumber.Request(out int clientId);
            
            if(clientId == attackerId) return;
            
            
        }
        
        [Rpc(SendTo.NotMe)]
        private void ReplicateThrowBombRpc(Vector3 position, Vector3 direction, Vector3 throwForce, int objectId, DateTime timestamp, int cliendId)
        {
            float latency = MilisecondsUtils.CalculateLatency(timestamp);
            ReplicateThrowAction(position, direction, throwForce, objectId, latency, cliendId);
        } 
        
        public override void Apply(ref InputPayload @event) { }

        public void RequestBombsAuthority()
        {
            if(!IsOwner || !NonNetworkObjectPool.Singleton.init) return;

            var allBombs = NonNetworkObjectPool.Singleton.GetAllNetworkObjects(_bombPrefab);
            foreach (var bombNo in allBombs)
            {
                bombNo.RequestOwnership();
            }

            _bombsRequested = true;
        }

        private void OnAttackGlobal(AttackEvent attack)
        {
            if(!IsOwner) return;
            
            if (!attack.Success) return;

            _invoker.PlayerNumber.Request(out int clientId);
            if(clientId != attack.SenderId || clientId == attack.ReceiverId) return;
            
            AccelerateReload((GunBelt.Weapon)attack.Weapon);
        }
    }
}
