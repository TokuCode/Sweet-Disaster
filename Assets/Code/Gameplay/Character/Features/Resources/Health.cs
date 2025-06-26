using System;
using Code.Gameplay.Character.Framework;
using UnityEngine;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.Knockback;
using Unity.Netcode;

namespace Code.Gameplay.Character.Features
{
    public class Health : Feature
    {
        private Movement move;
        
        [Header("Health Parameters")]
        [SerializeField] private NetworkVariable<float> _health = new (0, NetworkVariableReadPermission.Owner);
        public float HealthAmount => _health.Value;
        [SerializeField] private float _baseHealth;
        public float BaseHealth => _baseHealth;
        public float HealthRatio => _health.Value / _baseHealth;
        
        [Header("Stun")]
        [SerializeField] private NetworkVariable<bool> _isStunned = new (false, NetworkVariableReadPermission.Owner);
        public bool IsStunned => _isStunned.Value;
        [SerializeField] private float _stunMinDuration;
        [SerializeField] private float _stunDurationPerKnockback;
        [SerializeField] private float _stunTimer;
        
        public event EventHandler<OnHealthChangedEventArgs> OnHealthChanged;
        public event Action OnStun;
        public Pipeline<AttackEvent> AttackPipeline { get; private set; }
    
        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out move);
            AttackPipeline = new Pipeline<AttackEvent>();
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }

        public override void UpdateFeature()
        {
            if (!IsServer) return;
            
            if(_stunTimer > 0) _stunTimer -= Time.deltaTime;
            else if(_isStunned.Value) UnStun();
        }
    
        private void Damage(float percentage)
        {
            if (!IsServer) return;
            _health.Value += percentage * _baseHealth;
            OnHealthChangedEventArgs args = new ()
            {
                Health = _health.Value,
                MaxHealth = _baseHealth,
                HealthRatio = _health.Value / _baseHealth
            };
            OnHealthChanged?.Invoke(this, args);
        }
    
        private void Knockback(Vector3 direction, float knockbackForce, float knockbackUpForce, float damagePercentage)
        {
            float newHealthValue = _health.Value + damagePercentage * _baseHealth;
            float healthRatio = newHealthValue / _baseHealth;
            float knockback = knockbackForce * healthRatio * 10f;
            float knockbackUp = knockbackUpForce * healthRatio * 10f;
            Vector3 force = direction * knockback + Vector3.up * knockbackUp;

            if (IsOwner)
            {
                _invoker.AddForce.Perform(new(force, ForceMode2D.Impulse));
                Debug.Log("Explosion!!!");
            }
            //else AddKnockbackToClientRpc(force);

            float stunTime = _stunMinDuration + _stunDurationPerKnockback * knockback;
            Stun(stunTime);
        }

        public void Stun(float duration)
        {
            if(!IsServer) return;
            
            _isStunned.Value = true;
            _stunTimer = duration;
            
            move.BlockMovement();
            
            OnStun?.Invoke();
        }
    
        public void UnStun()
        {
            _isStunned.Value = false;
            move.UnblockMovement();
        }
        
        public void Attack(AttackEvent attackEvent)
        {
            AttackPipeline.Process(ref attackEvent);
            
            if (attackEvent.Success)
            {
                var direction = transform.position - attackEvent.SourcePosition;
                Knockback(direction.normalized, KnockbackTable.Instance.GetKnockbackForce(attackEvent.KnockbackLevel), KnockbackTable.Instance.GetKnockbackForce(attackEvent.KnockbackUpLevel), attackEvent.DamagePercentage);
                
                Damage(attackEvent.DamagePercentage);
            }
        }

        [ServerRpc]
        private void DamageServerRpc(float damagePercentage)
        {
            Damage(damagePercentage);
        }

        [ServerRpc]
        private void RequestStunServerRpc(float stunDuration)
        {
            Stun(stunDuration);
        }

        [ClientRpc]
        private void AddKnockbackToClientRpc(Vector3 force)
        {
            if (!IsOwner) return;
            _invoker.AddForce.Perform(new(force, ForceMode2D.Impulse));
        }
    }
}
