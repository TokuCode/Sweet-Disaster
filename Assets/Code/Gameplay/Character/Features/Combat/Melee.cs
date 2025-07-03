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
        [SerializeField] private float _radius;
        [SerializeField] private float _extraDistance;
        [SerializeField] private float _damage;
        [SerializeField] private Vector2 _knockback;
        [SerializeField] private float _cooldown;
        private CountdownTimer _cooldownTimer;
        public float CooldownProgress => _cooldownTimer.Progress;
        private bool _onCooldown;
        public bool OnCooldown => _onCooldown;
        [SerializeField] private float _windUpTime; //TODO Replace with animation events
        [SerializeField] private float _followUpTime;
        [SerializeField] private LayerMask _attackLayer;
        [SerializeField] private GameObject _vfxPrefab;
        
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
            if(IsOwner) InputReader.Instance.OnMeleePressed += TryMelee;
            _cooldownTimer = new (_cooldown);
            _cooldownTimer.OnTimerStop += ResetMelee;
        }

        private void TryMelee()
        {
            bool canAttackInternal = !_isAttacking && !_onCooldown;
            bool canAttackExternal = !crouch.IsCrouching && !health.IsStunned && !shield.IsShieldActive && !bomb.IsThrowing && !shoot.IsShooting && !shoot.IsReloading;
            if (canAttackInternal && canAttackExternal)
            {
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
            position += InputReader.Instance.HandleDirection * _extraDistance;
            
            AttackVFX(position);
            ReplicateVFXRpc(position);

            var colliders = Physics2D.OverlapCircleAll(position, _radius, _attackLayer);
            foreach (var collider in colliders)
            {
                if(collider.gameObject.CompareTag(gameObject.tag)) continue;
                
                PlayerController player = collider.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.Invoker.ClientId.Request(out int otherClientId);
                    
                    if (player.Dependencies.TryGetFeature(out Health health))
                    {
                        _invoker.ClientId.Request(out int clientId);
                        health.RequestAttackInOwner(new ()
                        {
                            DamagePercentage = _damage,
                            KnockbackForce = _knockback.x,
                            KnockbackUpForce = _knockback.y,
                            SourcePosition = centerPosition,
                            Success = true,
                            SenderId = clientId,
                            ReceiverId = otherClientId
                        });
                    }
                }
                
                Dummy dummy = collider.gameObject.GetComponent<Dummy>();
                if (dummy != null)
                {
                    dummy.Attack(new ()
                    {
                        DamagePercentage = _damage,
                        KnockbackForce = _knockback.x,
                        KnockbackUpForce = _knockback.y,
                        SourcePosition = transform.position,
                        Success = true,
                        Weapon = (int)GunBelt.Weapon.Melee
                    });
                }  
            }
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

        private void AttackVFX(Vector3 position)
        {
            var go = ObjectPoolManager.Instance.Get(_vfxPrefab, position, Quaternion.identity);
            go.SetActive(true);
            go.GetComponent<AttackVFX>().Init();
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateVFXRpc(Vector3 position)
        {
            AttackVFX(position);
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }
    }
}