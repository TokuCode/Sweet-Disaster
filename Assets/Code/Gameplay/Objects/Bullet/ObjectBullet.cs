using System;
using Code.Gameplay.Character;
using Code.Helpers.Utils;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class ObjectBullet : PoolCustomBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private CircleCollider2D _circleCollider2D;
        
        [Header("Settings")] 
        public string OwnerTag;
        public float LifeTime;
        private float _lifeTimeTimer;
        public float Damage;
        public int KnockbackLevel;
        public float Speed;
        public Vector2 Direction;
        
        [Header("Collision Settings")]
        public LayerMask characterLayer;
        public LayerMask solidLayer;

        public override void Reset()
        {
            _lifeTimeTimer = LifeTime;
            Direction = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
        }
        
        public override void SetArgs(CustomBehaviourArgs args)
        {
            if (args is BulletArgs bulletArgs)
            {
                OwnerTag = bulletArgs.OwnerTag;
                Damage = bulletArgs.Damage;
                LifeTime = bulletArgs.LifeTime;
                KnockbackLevel = bulletArgs.KnockbackLevel;
                Speed = bulletArgs.Speed;
                Direction = bulletArgs.Direction;

                if (Direction != Vector2.zero)
                {
                    transform.up = Direction.normalized;
                }
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            
            if(_lifeTimeTimer > 0) _lifeTimeTimer -= Time.deltaTime;
            else 
            {
                PoolReturn();
            }
        }

        private void FixedUpdate()
        {
            if(!IsServer) return;
            
            if (Direction != Vector2.zero)
            {
                _rigidbody.linearVelocity = Direction.normalized * Speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!IsServer) return;
            
            if (!other.gameObject.CompareTag(OwnerTag) && LayerMaskUtils.CompareLayerMask(other.gameObject, characterLayer))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    // Handle player hit logic
                }
                PoolReturn();
            }
            
            else if (LayerMaskUtils.CompareLayerMask(other.gameObject, solidLayer))
            {
                PoolReturn();
            }
        }

        private void PoolReturn()
        {
            if(!IsServer) return;
            
            networkObject.Despawn();
        }
    }
}