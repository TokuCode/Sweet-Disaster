using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Gameplay.Objects;
using Code.Helpers;
using Code.Helpers.Utils;
using Code.Networking.ClientPrediction;
using Code.Systems.Attack;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

public class ObjectBomb : NetworkBehaviour 
{
    private const float maxLatencyMiliseconds = 300;
    
    [SerializeField] private int _bounceCount; 
    [SerializeField] private bool started;
    [SerializeField] private float _initTime;
    private CountdownTimer _initTimer;
    
    [Header("References")]
    [SerializeField] private CircleCollider2D _collider2D;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private SerializableGuid prefabId;
    [SerializeField] private int id;
 
    [Header("Collision Settings")]
    [SerializeField] private float _maxSlopeAngle;
    [SerializeField] private float _collisionSimetryCoefficient;
    [SerializeField] private LayerMask _attackLayer;
    [SerializeField] private LayerMask _bounceLayer;
    [SerializeField] private LayerMask _specialAttackLayer;
    
    [Header("Static Settings")]
    [SerializeField] private float _explosionRadius;
    [SerializeField] private float _explosionDamageInCenter;
    [SerializeField] private float _explosionDamageInBorder;
    [SerializeField] private float _knockbackLevelInCenter;
    [SerializeField] private float _knockbackLevelInBorder;
    [SerializeField] private float _knockbackUpLevelInCenter;
    [SerializeField] private float _knockbackUpLevelInBorder;

    [Header("Dynamic Settings")]
    [SerializeField] private string _ownerTag;
    
    [Header("Sync Settings")]
    [SerializeField] private float _maxPositionError;
    [SerializeField] private float _maxVelocityError;
    
    private void Awake()
    {
        _initTimer = new(_initTime);
        _initTimer.OnTimerStop += OnDelayedInit;
    }

    private void OnDelayedInit()
    {
        _collider2D.isTrigger = false;

        var collision = Physics2D.OverlapCircleAll(transform.position, _collider2D.radius);

        foreach (var collider2D in collision)
        {
            if(collider2D != null && collider2D.gameObject != gameObject) Explode();
        }
    }

    private void Update()
    {
        if (!started) return;
        
        _initTimer.Tick(Time.deltaTime);
    }

    public BombStatePayload GetState()
    {
        return new()
        {
            objectId = id,
            velocity = _rigidbody2D.linearVelocity,
            position = _rigidbody2D.position,
            timestamp = DateTime.Now
        };
    }

    public void Init(string ownerTag, Vector2 impulse, int id, float latency)
    {
        _ownerTag = ownerTag;
        this.id = id;
        _initTimer.Start();
        //if (latency != 0)
        //{
        //    Anticipate(impulse, latency);
        //    _initTimer.Tick(latency);
        //}
        //else _rigidbody2D.linearVelocity = impulse;
        _rigidbody2D.linearVelocity = impulse;
        started = true;
    }

    public void Anticipate(Vector2 initialVelocity, float latency)
    {
        float v0Y, vX, dVY, g, t, dX, dY;
        vX = initialVelocity.x;
        v0Y = initialVelocity.y;
        g = Physics2D.gravity.y;
        t = Mathf.Min(latency, maxLatencyMiliseconds/1000);
        dX = vX * t;
        dY = v0Y * t - g / 2 * t * t;
        dVY = - g * t;
        
        Vector2 dPos = new (dX, dY);
        Vector2 dV = new (0, dVY);
        
        _rigidbody2D.position += dPos;
        _rigidbody2D.linearVelocity = initialVelocity + dV;
    }

    public void HardSync(Vector3 position, Vector2 velocity, float latency)
    {
        if (IsHost) return;
        
        _rigidbody2D.position = position;
        _rigidbody2D.linearVelocity = velocity;
        
        float v0Y, vX, dVY, g, t, dX, dY;
        t = Mathf.Min(latency, maxLatencyMiliseconds/1000);
        if(t <= 0) return;
        
        g = Physics2D.gravity.y;
        vX = velocity.x;
        v0Y = velocity.y;
        dX = vX * t;
        dY = v0Y * t - g / 2 * t * t;
        dVY = - g * t;
        
        Vector2 dPos = new (dX, dY);
        Vector2 dV = new (0, dVY); 
        _rigidbody2D.position += dPos;
        _rigidbody2D.linearVelocity += dV;
    }
    
    public void Reset()
    {
        ResetNonNotify();
        
        NonPooledSync.Singleton.RemoveBomb(this);
        NonPooledSync.Singleton.RequestBombRemoval(id);
    }

    public void ResetNonNotify()
    {
        ExplosionFX();
        gameObject.SetActive(false);
        
        _ownerTag = string.Empty;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _collider2D.isTrigger = true;
        
        _bounceCount = 0; 
        started = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!started) return;
        
        var surfaceNormal = collision.GetContact(0).normal;
        float angle = Vector3.Angle(Vector3.up, surfaceNormal);
        bool horizontal = angle < _maxSlopeAngle;

        if (collision.gameObject.CompareTag(_ownerTag) && _bounceCount == 0) return;
        
        if(LayerMaskUtils.CompareGameObjectLayerMask(collision.gameObject, _attackLayer) || (horizontal && !LayerMaskUtils.CompareGameObjectLayerMask(collision.gameObject, _bounceLayer))) Explode(); 
        else Bounce(surfaceNormal);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!started) return; 
        
        if(LayerMaskUtils.CompareGameObjectLayerMask(collision.gameObject, _specialAttackLayer)) Explode(); 
    }

    private void Explode()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;

        var colliders = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _attackLayer);
        foreach (var collider in colliders)
        {
            PlayerController player = collider.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                float ratio = 1 - Mathf.Clamp01(distance / _explosionRadius);
                float damagePercentage = Mathf.Lerp(_explosionDamageInBorder, _explosionDamageInCenter, ratio);
                float knockbackLevel = Mathf.Lerp(_knockbackLevelInBorder, _knockbackLevelInCenter, ratio);
                float knockbackUpLevel = Mathf.Lerp(_knockbackUpLevelInBorder, _knockbackUpLevelInCenter, ratio);

                if (player.Dependencies.TryGetFeature(out Health health))
                {
                    health.Attack(new AttackEvent
                    {
                        DamagePercentage = damagePercentage,
                        KnockbackForce = knockbackLevel,
                        KnockbackUpForce = knockbackUpLevel,
                        SourcePosition = transform.position,
                        Success = true
                    });
                }
            }
            
            Dummy dummy = collider.gameObject.GetComponent<Dummy>();
            if (dummy != null)
            {
                float distance = Vector2.Distance(transform.position, dummy.transform.position);
                float ratio = 1 - Mathf.Clamp01(distance / _explosionRadius);
                float damagePercentage = Mathf.Lerp(_explosionDamageInBorder, _explosionDamageInCenter, ratio);
                float knockbackLevel = Mathf.Lerp(_knockbackLevelInBorder, _knockbackLevelInCenter, ratio);
                float knockbackUpLevel = Mathf.Lerp(_knockbackUpLevelInBorder, _knockbackUpLevelInCenter, ratio); 
                
                dummy.Attack(new AttackEvent
                {
                    DamagePercentage = damagePercentage,
                    KnockbackForce = knockbackLevel,
                    KnockbackUpForce = knockbackUpLevel,
                    SourcePosition = transform.position,
                    Success = true
                }); 
            }
        }
        
        Reset();
    }

    private void ExplosionFX()
    {
        var explosionGo = ObjectPoolManager.Instance.Get(_explosionEffectPrefab, transform.position, Quaternion.identity);
        var explosion = explosionGo.GetComponent<VFXExplosion>();
        explosion.Init(_explosionRadius);
    }

    private void Bounce(Vector2 normal)
    {
        _bounceCount++;
        var velocity = _rigidbody2D.linearVelocity;
        var reflection = Vector2.Reflect(velocity, normal);
        var newVelocity = reflection * _collisionSimetryCoefficient;
        _rigidbody2D.linearVelocity = newVelocity;
        //NonPooledSync.Singleton.RequestHardSync(new BombStatePayload
        //{
        //    objectId = id,
        //    position = _rigidbody2D.position,
        //    velocity = newVelocity,
        //    timestamp = DateTime.Now
        //});
    }

    public void AddImpulse(Vector2 force) => _rigidbody2D.AddForce(force, ForceMode2D.Impulse);
}
