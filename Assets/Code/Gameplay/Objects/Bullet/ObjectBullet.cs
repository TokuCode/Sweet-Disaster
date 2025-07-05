using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Helpers;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.NetworkObjectPool;
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
        [SerializeField] private float _baseDamage;
        [SerializeField] private float _knockbackLevel;
        [SerializeField] private float _knockbackUpLevel;
        [SerializeField] private float _speed;
        
        [Header("Dynamic Settings")]
        [SerializeField] private string _ownerTag;
        [SerializeField] private Vector2 _direction;
        [SerializeField] private int _senderId;
        private float _damage;
        
        [Header("Collision Settings")]
        [SerializeField] LayerMask _characterLayer;
        [SerializeField] LayerMask _solidLayer;
        
        [Header("Vfx Settings")]
        [SerializeField] private GameObject _vfx;
        [SerializeField] private float _radiusVfx;

        public void Initialize(Vector2 direction, string ownerTag, float latency, int senderId, float damageMultiplier)
        {
            _ownerTag = ownerTag;
            _direction = direction;
            _damage = _baseDamage * damageMultiplier;
            transform.right = direction;
            
            latency = Mathf.Min(latency, maxLatencyMiliseconds/1000);
            _rigidbody.position += _direction.normalized * (_speed * latency);
            
            _lifeTimeTimer = _lifeTime;
            
            _senderId = senderId;

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
                        player.Invoker.PlayerNumber.Request(out int otherClientId);
                        health.Attack(new ()
                        {
                            DamagePercentage = _damage,
                            KnockbackForce = _knockbackLevel,
                            KnockbackUpForce = _knockbackUpLevel,
                            SourcePosition = transform.position,
                            Success = true,
                            SenderId = _senderId,
                            ReceiverId = otherClientId,
                            Weapon = (int)GunBelt.Weapon.Gun,
                            Unblockeable = false
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
                        Success = true,
                        SenderId = _senderId,
                        Weapon = (int)GunBelt.Weapon.Gun,
                        Unblockeable = false
                    });
                } 
            }
            
            if (LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _solidLayer) || (LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _characterLayer) && !other.gameObject.CompareTag(_ownerTag)))
            {
                ObjectShield shield = other.gameObject.GetComponent<ObjectShield>(); 
                if(shield != null) shield.OnBlock(_senderId, _damage);
                Reset();
            }
        }

        private void Reset()
        {
            gameObject.SetActive(false);
            
            _ownerTag = string.Empty;
            _direction = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero; 
            _damage = _baseDamage;
            
            _senderId = -1;
            
            started = false;
            
            AttackVFX(transform.position, _radiusVfx);
        }
        
        private void AttackVFX(Vector3 position, float radius)
        {
            var go = ObjectPoolManager.Instance.Get(_vfx, position, Quaternion.identity);
            go.SetActive(true);
            go.GetComponent<AttackVFX>().Init(radius);
        }
    }
}