using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Gameplay.Objects;
using Code.Helpers;
using Code.Helpers.Utils;
using Code.Systems.Attack;
using Code.Systems.NetworkObjectPool;
using Unity.Netcode;
using UnityEngine;

public class ObjectBomb : NetworkBehaviour 
{
    private const float maxLatencyMiliseconds = 250;
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
    
    [Header("Static Settings")]
    [SerializeField] private float _explosionRadius;
    [SerializeField] private float _explosionDamageInCenter;
    [SerializeField] private float _explosionDamageInBorder;
    [SerializeField] private int _knockbackLevelInCenter;
    [SerializeField] private int _knockbackLevelInBorder;
    [SerializeField] private int _knockbackUpLevelInCenter;
    [SerializeField] private int _knockbackUpLevelInBorder;

    [Header("Dynamic Settings")]
    [SerializeField] private string _ownerTag;
    
    [Header("Synchronization")]
    [SerializeField] private bool _syncNextBounce;
    [SerializeField] private float _maxSyncTimeMiliseconds;
    private DateTime _syncTime;
    [SerializeField] private Vector3 _syncPosition;
    [SerializeField] private Vector2 _syncVelocity;

    private void Awake()
    {
        _initTimer = new(_initTime);
        _initTimer.OnTimerStop += () => { _collider2D.isTrigger = false; };
    }

    private void Update()
    {
        if (!started) return;
        _initTimer.Tick(Time.deltaTime);
    }

    public void Init(string ownerTag, Vector2 impulse, int id, float latency)
    {
        _ownerTag = ownerTag;
        this.id = id;
        _initTimer.Start();
        if (latency != 0)
        {
            Anticipate(impulse, latency);
            _initTimer.Tick(latency);
        }
        else AddImpulse(impulse);
        started = true;
    }

    public void Anticipate(Vector2 impulse, float latency)
    {
        float v0Y, vX, vY, g, t, dX, dY;
        vX = impulse.x;
        v0Y = impulse.y;
        g = Physics2D.gravity.y;
        t = Mathf.Min(latency, maxLatencyMiliseconds/1000);
        dX = vX * t;
        dY = v0Y * t - g / 2 * t * t;
        vY = v0Y - g * t;
        
        Vector2 dVec = new (dX, dY);
        Vector2 VF = new (vX, vY);
        
        _rigidbody2D.position += dVec;
        AddImpulse(VF);

        if (IsServer)
        {
            NonPooledSync.Singleton.RequestHardSync(_rigidbody2D.position, _rigidbody2D.linearVelocity, id);
        }
    }


    public void HardSync(Vector3 position, Vector3 velocity)
    {
        _rigidbody2D.position = position;
        _rigidbody2D.linearVelocity = velocity;
    }
    
    public void Reset()
    {
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
        
        if(LayerMaskUtils.CompareGameObjectLayerMask(collision.gameObject, _attackLayer) || horizontal) Explode(); 
        else Bounce(surfaceNormal);
    }

    private void Explode()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;

        ExplosionFX();
        
        var colliders = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _attackLayer);
        foreach (var collider in colliders)
        {
            PlayerController player = collider.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                float ratio = 1 - Mathf.Clamp01(distance / _explosionRadius);
                float damagePercentage = Mathf.Lerp(_explosionDamageInBorder, _explosionDamageInCenter, ratio);
                int knockbackLevel = Mathf.RoundToInt(Mathf.Lerp(_knockbackLevelInBorder, _knockbackLevelInCenter, ratio));
                int knockbackUpLevel = Mathf.RoundToInt(Mathf.Lerp(_knockbackUpLevelInBorder, _knockbackUpLevelInCenter, ratio));

                if (player.Dependencies.TryGetFeature(out Health health))
                {
                    health.Attack(new AttackEvent
                    {
                        DamagePercentage = damagePercentage,
                        KnockbackLevel = knockbackLevel,
                        KnockbackUpLevel = knockbackUpLevel,
                        SourcePosition = transform.position,
                        Success = true
                    });
                }
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
        
        if (_syncNextBounce && IsClient && !IsHost)
        {
            _syncNextBounce = false;
            float latency = MilisecondsUtils.CalculateLatency(_syncTime);
            if (latency <= _maxSyncTimeMiliseconds / 1000)
            {
                _rigidbody2D.position = _syncPosition;
                _rigidbody2D.linearVelocity = _syncVelocity;
                _syncPosition = Vector3.zero;
                _syncVelocity = Vector2.zero;
                return;
            }
        }
        
        var velocity = _rigidbody2D.linearVelocity;
        var reflection = Vector2.Reflect(velocity, normal);
        _rigidbody2D.linearVelocity = reflection * _collisionSimetryCoefficient;

        if (IsServer)
        {
            RequestBounceSync(_rigidbody2D.position, velocity, id, _bounceCount);
        }
    }

    public void AddImpulse(Vector2 force) => _rigidbody2D.AddForce(force, ForceMode2D.Impulse);

    public void RequestBounceSync(Vector2 position, Vector2 velocity, int id, int bounceCount)
    {
        NonPooledSync.Singleton.RequestBounceSync(position, velocity, id, bounceCount);
    }

    public void SynchronizeBounce(Vector3 position, Vector2 velocity, int bounceCount)
    {
        if (_bounceCount > bounceCount) return;

        if (_bounceCount == bounceCount)
        {
            _rigidbody2D.linearVelocity = velocity;
            return;
        }

        if (_bounceCount == bounceCount - 1)
        {
            _syncNextBounce = true;
            _syncPosition = position;
            _syncVelocity = velocity;
            _syncTime = DateTime.Now;
        }

        else
        {
            _rigidbody2D.position = position;
            _rigidbody2D.linearVelocity = velocity;
        }
    } 
}
