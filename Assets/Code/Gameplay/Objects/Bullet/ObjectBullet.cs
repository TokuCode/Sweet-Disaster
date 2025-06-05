using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Helpers.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class ObjectBullet : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private CircleCollider2D _circleCollider2D;
        
        [Header("Settings")] 
        [SerializeField] private string _ownerTag;
        [SerializeField] private float _lifeTime;
        [SerializeField] private float _lifeTimeTimer;
        [SerializeField] private float _damage;
        [SerializeField] private int _knockbackLevel;
        [SerializeField] private int _knockbackUpLevel;
        [SerializeField] private float _speed;
        [SerializeField] private Vector2 _direction;
        
        [Header("Collision Settings")]
        [SerializeField] LayerMask _characterLayer;
        [SerializeField] LayerMask _solidLayer;

        public override void OnNetworkDespawn()
        {
            _lifeTimeTimer = _lifeTime;
            _direction = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
        }
        
        public void Set(string ownerTag, Vector2 direction)
        {
            _ownerTag = ownerTag;
            _direction = direction;
            transform.right = direction;
        }

        private void Update()
        {
            if (!IsServer) return;
            
            if(_lifeTimeTimer > 0) _lifeTimeTimer -= Time.deltaTime;
            else 
            {
                SelfDestroy();
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
            if(!IsServer) return;
            
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
                            KnockbackLevel = _knockbackLevel,
                            KnockbackUpLevel = _knockbackUpLevel,
                            SourcePosition = transform.position,
                            Success = true
                        });
                    }
                }
                SelfDestroy();
            }
            
            else if (LayerMaskUtils.CompareGameObjectLayerMask(other.gameObject, _solidLayer))
            {
                SelfDestroy();
            }
        }

        private void SelfDestroy()
        {
            if(!IsServer) return;
            NetworkObject.Despawn();
        }
    }
}