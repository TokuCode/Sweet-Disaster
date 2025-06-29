using System;
using Code.Helpers;
using Code.Networking.Session;
using Code.Systems.Attack;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class Dummy : NetworkBehaviour
    {
        [SerializeField] private GameObject _explosionFXPrefab;
        [SerializeField] private Rigidbody2D[] _dummyParts;
        [SerializeField] private Vector3[] _positions;

        [Header("Dummy Settings")] 
        [SerializeField] private float _explosionRadius;
        [SerializeField] private float _maxDamage;
        [SerializeField] private float _baseHealth;
        [SerializeField] private float _currentDamage;
        [SerializeField] private float _stunTime;
        private CountdownTimer _stunTimer;
        public float CurrentDamage => _currentDamage;

        private void Awake()
        {
            if (!SessionManager.Instance.IsPracticeMode)
            {
                Destroy(gameObject);
                return;
            }

            _stunTimer = new(_stunTime);
            _stunTimer.OnTimerStop += ResetDummyParts;
        }
        
        private void Update()
        {
            _stunTimer.Tick(Time.deltaTime);
        }

        public void Attack(AttackEvent attackEvent)
        {
            if (attackEvent.Success)
            {
                var direction = transform.position - attackEvent.SourcePosition;
                Damage(attackEvent.DamagePercentage);
                Knockback(direction.normalized, attackEvent.KnockbackForce, attackEvent.KnockbackUpForce);
            }
        }
        
        private void Damage(float percentage)
        {
            if (!IsServer) return;
            _currentDamage += percentage * _baseHealth;

            if (_currentDamage > _maxDamage)
            {
                ExplosionFX();
                Reset();
            }
        }

        private void Reset()
        {
            _currentDamage = 0;
            ResetDummyParts();
        }

        private void ResetDummyParts()
        {
            for(int i = 0; i < _dummyParts.Length; i++) ResetDummyPart(i);
        }

        private void ResetDummyPart(int i)
        {
            _dummyParts[i].position = transform.position + _positions[i];
            _dummyParts[i].linearVelocity = Vector2.zero;
        }
        
        
        private void Knockback(Vector3 direction, float knockbackForce, float knockbackUpForce)
        {
            if (!IsServer) return;
            
            float healthRatio = _currentDamage / _baseHealth;
            float knockback = knockbackForce * healthRatio * 10f;
            float knockbackUp = knockbackUpForce * healthRatio * 10f;
            Vector3 force = direction * knockback + Vector3.up * knockbackUp;

            foreach (var part in _dummyParts)
            {
                part.AddForce(force, ForceMode2D.Impulse);
            }
            
            _stunTimer.Start();
        } 
        
        private void ExplosionFX()
        {
            var explosionGo = ObjectPoolManager.Instance.Get(_explosionFXPrefab, transform.position, Quaternion.identity);
            var explosion = explosionGo.GetComponent<VFXExplosion>();
            explosion.Init(_explosionRadius);
        } 
    }
}
