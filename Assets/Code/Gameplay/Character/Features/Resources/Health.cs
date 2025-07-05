using System;
using Code.Gameplay.Character.Framework;
using Code.Helpers;
using UnityEngine;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.Input;
using Unity.Netcode;

namespace Code.Gameplay.Character.Features
{
    public class Health : Feature
    {
        private Movement move;
        
        [Header("Health Parameters")]
        [SerializeField] private NetworkVariable<float> _health = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public float HealthAmount => _health.Value;
        [SerializeField] private float _baseHealth;
        public float BaseHealth => _baseHealth;
        public float HealthRatio => _health.Value / _baseHealth;
        
        [Header("Stun")]
        [SerializeField] private NetworkVariable<bool> _isStunned = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsStunned => _isStunned.Value;
        [SerializeField] private float _stunMinDuration;
        [SerializeField] private float _stunDurationPerKnockback;
        [SerializeField] private float _stunTimer;
        
        public event EventHandler<OnHealthChangedEventArgs> OnHealthChanged;
        public event Action<float, float> OnStun;
        public event Action OnUnStun;

        [Header("Directional Influence")] 
        [SerializeField] private float _directionalInfluence;
        
        public Pipeline<AttackEvent> AttackPipeline { get; private set; }

        public override void ResetFeature()
        {
            if (IsOwner)
            {
                _health.Value = 0;
                _isStunned.Value = false;
            }
        }

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
            if (!IsOwner) return;
            
            if(_stunTimer > 0) _stunTimer -= Time.deltaTime;
            else if(_isStunned.Value) UnStun();
        }
    
        private void Damage(float percentage)
        {
            if (!IsOwner) return;
            _health.Value += percentage * _baseHealth;
            OnHealthChangedEventArgs args = new ()
            {
                Health = _health.Value,
                MaxHealth = _baseHealth,
                HealthRatio = _health.Value / _baseHealth
            };
            OnHealthChanged?.Invoke(this, args);
        }
    
        private void Knockback(Vector3 direction, float knockbackForce, float knockbackUpForce, float healthRatio)
        {
            float knockback = knockbackForce * healthRatio * 10f;
            float knockbackUp = knockbackUpForce * healthRatio * 10f;
            Vector3 handleDirection = InputReader.Instance.HandleDirection;
            handleDirection.x = Mathf.Abs(handleDirection.x) * Mathf.Sign(direction.x);
            Vector3 force = direction * knockback + Vector3.up * knockbackUp;
            var forceDirection = Vector3.Slerp(force.normalized, handleDirection, _directionalInfluence);
            force = forceDirection * force.magnitude;
            
            _invoker.Knockback.Perform(force);

            float stunTime = _stunMinDuration + _stunDurationPerKnockback * (knockback + knockbackUp);
            Stun(stunTime, healthRatio);
        }

        public void Stun(float duration, float healthRatio)
        {
            if(!IsOwner) return;
            
            _isStunned.Value = true;
            _stunTimer = duration;
            
            move.BlockMovement();
            
            OnStun?.Invoke(duration, healthRatio);
        }

        public void AccelerateStun(float duration)
        {
            if(!_isStunned.Value) return;

            _stunTimer = Mathf.Max(0, _stunTimer - duration);
            if (_stunTimer <= 0)
            {
                if(IsOwner) UnStun();
            }
        }
    
        public void UnStun()
        {
            _isStunned.Value = false;
            move.UnblockMovement();
            
            OnUnStun?.Invoke();
        }
        
        public void Attack(AttackEvent attackEvent)
        {
            AttackPipeline.Process(ref attackEvent);
            
            if (attackEvent.Success)
            {
                var direction = (transform.position - attackEvent.SourcePosition).With(y : 0).normalized;
                float newHealthRatio = _health.Value / _baseHealth + attackEvent.DamagePercentage;
                Damage(attackEvent.DamagePercentage);
                Knockback(direction, attackEvent.KnockbackForce, attackEvent.KnockbackUpForce, newHealthRatio);
            }
            
            if(IsOwner) AttackBus.Singleton.BroadcastEvent(attackEvent);
        }

        public void RequestAttackInOwner(AttackEvent attackEvent)
        {
            RequestAttackToClientRpc(attackEvent);
            RequestAttackToServerRpc(attackEvent);
        }

        [ClientRpc]
        private void RequestAttackToClientRpc(AttackEvent attackEvent)
        {
            if (!IsOwner) return;
            Attack(attackEvent);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestAttackToServerRpc(AttackEvent attackEvent)
        {
            if (!IsServer) return;
            Attack(attackEvent);
        }

        [ServerRpc]
        private void RequestUnStunToServerRpc()
        {
            UnStun();
        }
    }
}
