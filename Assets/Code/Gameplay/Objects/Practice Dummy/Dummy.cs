using System;
using Code.Helpers;
using Code.Networking.Session;
using Code.Systems.Attack;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;
using Code.Gameplay.Tutorial;
using Code.Helpers.Utils;

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

        [Header("Dummy shoot settings")] 
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private float timer;
        [SerializeField] private float _timeBetweenShots;
        [SerializeField] private Transform shootPos;
        private bool startShooting;
        [SerializeField] private float direction;
        
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
            timer = _timeBetweenShots;
        }
        
        private void Update()
        {
            _stunTimer.Tick(Time.deltaTime);

            if (TutorialActions.Instance.currentIndex == 12 && TutorialActions.Instance.waitForTrigger)
                startShooting = true;

            if (!startShooting) return;
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = _timeBetweenShots;
                var rotation = DirectionToRotation.GetRotation(Vector3.right);
                
                var bulletNetworkObject = NonNetworkObjectPool.Singleton.GetNetworkObject(
                    _bulletPrefab,
                    new Vector3(shootPos.position.x * direction, shootPos.position.y, shootPos.position.z),
                    rotation,
                    out int bulletId
                );

                var bullet = bulletNetworkObject.gameObject.GetComponent<ObjectBullet>();

                bullet.Initialize(Vector2.right * direction, gameObject.tag, 0, 1, 5);
            }
        }

        public void Attack(AttackEvent attackEvent)
        {
            if (attackEvent.Success)
            {
                var direction = transform.position - attackEvent.SourcePosition;
                Damage(attackEvent.DamagePercentage);
                Knockback(direction.normalized, attackEvent.KnockbackForce, attackEvent.KnockbackUpForce);

                if (TutorialActions.Instance.currentIndex == 7 && TutorialActions.Instance.waitForTrigger)
                    TutorialActions.Instance.PlayerHasShotABot = true;
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
