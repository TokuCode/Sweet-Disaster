using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Helpers;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace Code.Gameplay.Objects
{
    public class ObjectBullet : NetworkBehaviour
    {
        private const float maxLatencyMiliseconds = 300;
        [SerializeField] private bool started;
        
        [Header("References")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private CircleCollider2D _circleCollider2D;
        [SerializeField] private SerializableGuid prefabId;
        
        [Header("Static Settings")] 
        [SerializeField] private float _lifeTime;
        private float _lifeTimeTimer;
        [SerializeField] private float _damage;
        [SerializeField] private float _knockbackLevel;
        [SerializeField] private float _knockbackUpLevel;
        [SerializeField] private float _speed;
        
        [Header("Dynamic Settings")]
        [SerializeField] private string _ownerTag;
        [SerializeField] private Vector2 _direction;
        
        [Header("Collision Settings")]
        [SerializeField] LayerMask _characterLayer;
        [SerializeField] LayerMask _solidLayer;

        public void Initialize(Vector2 direction, string ownerTag, float latency)
        {
            _ownerTag = ownerTag;
            _direction = direction;
            transform.right = direction;
            
            latency = Mathf.Min(latency, maxLatencyMiliseconds/1000);
            _rigidbody.position += _direction.normalized * (_speed * latency);
            
            _lifeTimeTimer = _lifeTime;

            started = true;
        }

        private void Update()
        {
            if(!started) return;
            
            if(_lifeTimeTimer > 0) _lifeTimeTimer -= Time.deltaTime;
            else 
            {
                Reset();
            }
        }

        private void FixedUpdate()
        {
            if (_direction != Vector2.zero)
            {
                _rigidbody.linearVelocity = _direction.normalized * _speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!started && String.IsNullOrEmpty(_ownerTag)) return;
            
            if (!other.gameObject.CompareTag(_ownerTag) && LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _characterLayer))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (player.Dependencies.TryGetFeature(out Health health))
                    {
                        health.Attack(new ()
                        {
                            DamagePercentage = _damage,
                            KnockbackForce = _knockbackLevel,
                            KnockbackUpForce = _knockbackUpLevel,
                            SourcePosition = transform.position,
                            Success = true
                        });
                    }
                }
                
                Dummy dummy = other.gameObject.GetComponent<Dummy>();
                if (dummy != null)
                {
                    dummy.Attack(new ()
                    {
                        DamagePercentage = _damage,
                        KnockbackForce = _knockbackLevel,
                        KnockbackUpForce = _knockbackUpLevel,
                        SourcePosition = transform.position,
                        Success = true
                    });
                    
                } 
            }
            
            if (LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _solidLayer) || (LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _characterLayer) && !other.gameObject.CompareTag(_ownerTag)))
            {
                Reset();
            }
        }

        private void Reset()
        {
            gameObject.SetActive(false);
            
            _ownerTag = string.Empty;
            _direction = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero; 
            
            started = false;
        }
    }
}