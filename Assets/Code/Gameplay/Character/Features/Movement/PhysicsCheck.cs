using Code.Gameplay.Character.Framework;
using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class PhysicsCheck : Feature
    {
        private const float ExtraDistanceGround = .01f;
        private const float ExtraDistanceHead = .1f;
        private const float ExtraDistanceSlope = .25f;

        private Vector3 _playerPositionCache;
        private Vector2 _playerSizeCache;

        private Crouch crouch;
        
        [Header("Settings")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _maxSlopeAngle;
        private RaycastHit2D _slopeHit;

        [Header("Runtime")] 
        [SerializeField] private bool _isGrounded;
        public bool IsGrounded => _isGrounded;
        [SerializeField] private float _lastTimeOnGround;
        public float LastTimeOnGround => _lastTimeOnGround;
        [SerializeField] private bool _onSlope;
        public bool OnSlope => _onSlope;
        [SerializeField] private bool _isHeadBlocked;
        public bool HeadBlocked => _isHeadBlocked;

        public override void ResetFeature() { }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out crouch);
        }

        public override void UpdateFeature()
        {
            CachePlayerVariables();
            GroundCheck();
            SlopeCheck();
            HeadBlockCheck();
        }

        public override void FixedUpdateFeature() { }

        private void GroundCheck()
        {
            var position = _playerPositionCache;
            var size = _playerSizeCache;

            var footSize = new Vector2(size.x / 2, ExtraDistanceGround);
            var distance = size.y / 2 + ExtraDistanceGround;
            
            RaycastHit2D hit2D = Physics2D.BoxCast(position, footSize, 0f, Vector2.down, distance, _groundLayer);
            
            _isGrounded = hit2D.collider != null;
            if(_isGrounded) _lastTimeOnGround = Time.time;
        }

        private void SlopeCheck()
        {
            var position = _playerPositionCache;
            var size = _playerSizeCache;
            
            var distance = size.y / 2 + ExtraDistanceSlope;
            
            _slopeHit = Physics2D.Raycast(position, Vector2.down, distance, _groundLayer);
            
            if (_slopeHit.collider != null)
            {
                var slopeAngle = Vector2.Angle(Vector2.up, _slopeHit.normal);
                _onSlope = slopeAngle < _maxSlopeAngle && slopeAngle != 0;
            }
            else _onSlope = false;
        }

        private void HeadBlockCheck()
        {
            if (!crouch.IsCrouching)
            {
                _isHeadBlocked = false;
                return; 
            }
            
            var position = _playerPositionCache;
            var size = _playerSizeCache;

            var headSize = new Vector2(size.x / 2, ExtraDistanceHead);
            var distance = size.y / 2 + ExtraDistanceHead;

            RaycastHit2D hit2D = Physics2D.BoxCast(position, headSize, 0f, Vector2.up, distance, _groundLayer);

            _isHeadBlocked = hit2D.collider != null;
        }
        
        public Vector2 ProjectOnSlopeDirection(Vector2 inputDirection)
        {
            if (!_onSlope) return inputDirection;
            Vector2 tangent = Vector2.Perpendicular(_slopeHit.normal).normalized;
            return tangent * Vector2.Dot(tangent, inputDirection);
        }

        public void CachePlayerVariables()
        {
            _invoker.CenterPosition.Request(out _playerPositionCache);
            _invoker.Size.Request(out _playerSizeCache);
        }

        public override void Apply(ref InputPayload @event) { }
    }
}