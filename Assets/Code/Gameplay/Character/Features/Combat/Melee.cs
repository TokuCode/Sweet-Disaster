using System.Collections;
using System.Collections.Generic;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects;
using Code.Helpers;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class Melee : Feature
    {
        private Shoot shoot;
        private Bomb bomb;
        private Shield shield;
        private Health health;
        private Crouch crouch;

        [Header("Attack")] 
        [SerializeField] private float _minRadius;
        [SerializeField] private float _maxRadius;
        [SerializeField] private float _minDamage;
        [SerializeField] private Vector2 _minKnockback;
        [SerializeField] private float _maxDamage;
        [SerializeField] private Vector2 _maxKnockback;
        [SerializeField] private float _cooldown;
        private CountdownTimer _cooldownTimer;
        public float CooldownProgress => _cooldownTimer.Progress;
        private bool _onCooldown;
        public bool OnCooldown => _onCooldown;
        [SerializeField] private float _windUpTime; //TODO Replace with animation events
        [SerializeField] private float _followUpTime;
        [SerializeField] private LayerMask _attackLayer;
        [SerializeField] private GameObject _vfxPrefab;

        [Header("ShieldBash")] 
        [SerializeField] private float _minPushback;
        [SerializeField] private float _maxPushback;
        [SerializeField] private float _pushbackImpulseAngle;
        
        [Header("Runtime")]
        [SerializeField] private bool _isAttacking;
        public bool IsAttacking => _isAttacking;

        public override void ResetFeature()
        {
            CancelMelee();
            _cooldownTimer.Stop();
            _isAttacking = false;
            _onCooldown = false;
        }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out shoot);
            _dependencies.TryGetFeature(out shield);
            _dependencies.TryGetFeature(out bomb);
            _dependencies.TryGetFeature(out crouch);
            _dependencies.TryGetFeature(out health);
            if(IsOwner) InputReader.Instance.OnShieldBash += TryMelee;
            _cooldownTimer = new (_cooldown);
            _cooldownTimer.OnTimerStop += ResetMelee;
        }

        private void TryMelee()
        {
            bool canAttackInternal = !_isAttacking && !_onCooldown;
            bool canAttackExternal =  shield.IsShieldActive && shield.OutSafeZone;
            if (canAttackInternal && canAttackExternal)
            {
                shield.ShieldBash();
                StartCoroutine(MeleeSequence());
            }
        }

        private IEnumerator MeleeSequence()
        {
            _isAttacking = true;
            
            yield return new WaitForSeconds(_windUpTime);
            
            MeleeAction();
            
            yield return new WaitForSeconds(_followUpTime);
            
            _isAttacking = false;
            _onCooldown = true;
            _cooldownTimer.Start();
        }

        private void CancelMelee()
        {
            if(!_isAttacking) return;
            
            StopAllCoroutines();
            _isAttacking = false;
        }

        private void MeleeAction()
        {
            _invoker.GunTipPosition.Request(out var position);
            _invoker.CenterPosition.Request(out var centerPosition);
            float radius = Mathf.Lerp(_minRadius, _maxRadius, shield.TemperatureProgress);
            float damage = Mathf.Lerp(_minDamage, _maxDamage, shield.TemperatureProgress);
            Vector2 knockback = Vector2.Lerp(_minKnockback, _maxKnockback, shield.TemperatureProgress);
            float pushback = Mathf.Lerp(_minPushback, _maxPushback, shield.TemperatureProgress); 
                
            position += InputReader.Instance.HandleDirection * radius;
            var pushbackDirection = (position - centerPosition).normalized;
            
            PushBack(pushbackDirection, pushback);
            
            AttackVFX(position, radius);
            ReplicateVFXRpc(position, radius);

            var colliders = Physics2D.OverlapCircleAll(position, radius, _attackLayer);
            foreach (var collider in colliders)
            {
                if(collider.gameObject.CompareTag(gameObject.tag)) continue;
                
                PlayerController player = collider.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.Invoker.PlayerNumber.Request(out int otherClientId);
                    
                    if (player.Dependencies.TryGetFeature(out Health health))
                    {
                        _invoker.PlayerNumber.Request(out int clientId);
                        health.RequestAttackInOwner(new ()
                        {
                            DamagePercentage = damage,
                            KnockbackForce = knockback.x,
                            KnockbackUpForce = knockback.y,
                            SourcePosition = centerPosition,
                            Success = true,
                            SenderId = clientId,
                            ReceiverId = otherClientId,
                            Unblockeable = false
                        });
                    }
                }
                
                Dummy dummy = collider.gameObject.GetComponent<Dummy>();
                if (dummy != null)
                {
                    dummy.Attack(new ()
                    {
                        DamagePercentage = damage,
                        KnockbackForce = knockback.x,
                        KnockbackUpForce = knockback.y,
                        SourcePosition = transform.position,
                        Success = true,
                        Weapon = (int)GunBelt.Weapon.Melee,
                        Unblockeable = false
                    });
                }  
            }
        }

        private void PushBack(Vector3 direction, float pushback)
        {
            float minY = -Mathf.Cos(_pushbackImpulseAngle * Mathf.Deg2Rad);
            if (_invoker.Velocity.Request(out var velocity).success)
            { 
                if (direction.y <= minY && velocity.y < 0)
                    _invoker.Velocity.Perform(velocity.With(y: 0));
            }
            _invoker.AddForce.Perform(new(-direction, pushback, ForceMode2D.Impulse));
        }

        private void ResetMelee()
        {
            _onCooldown = false;
        }

        public override void UpdateFeature()
        {
            if(!IsOwner) return;
            
            _cooldownTimer.Tick(Time.deltaTime);
        }

        private void AttackVFX(Vector3 position, float radius)
        {
            var go = ObjectPoolManager.Instance.Get(_vfxPrefab, position, Quaternion.identity);
            go.SetActive(true);
            go.GetComponent<AttackVFX>().Init(radius);
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateVFXRpc(Vector3 position, float radius)
        {
            AttackVFX(position, radius);
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }
    }
}