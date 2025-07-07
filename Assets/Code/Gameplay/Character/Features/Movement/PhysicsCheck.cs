using Code.Gameplay.Character.Framework;
using Code.Helpers;
using Code.Networking.ClientPrediction;
using UnityEngine;

namespace Code.Gameplay.Character.Features
{
    public class PhysicsCheck : Feature
    {
        [Header("Physics Check")]
        [SerializeField] private float _extraDistanceGround = .01f;
        [SerializeField] private float _extraDistanceHead = .1f;
        [SerializeField] private float _extraDistanceSlope = .25f;

        private Vector3 _playerPositionCache;
        private Vector2 _playerSizeCache;

        private Crouch crouch;
        
        [Header("Settings")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _maxSlopeAngle;
        private RaycastHit2D _rightSlopeHit;
        private RaycastHit2D _leftSlopeHit;
        private RaycastHit2D _centerSlopeHit;
        private Vector3 _slopeNormal;
        public Vector3 SlopeNormal => _slopeNormal;
        private Vector3 _previousSlopeNormal;
        public Vector3 PreviousSlopeNormal => _previousSlopeNormal;

        [Header("Runtime")] 
        [SerializeField] private bool _isGrounded;
        public bool IsGrounded => _isGrounded;
        [SerializeField] private float _lastTimeOnGround;
        public float LastTimeOnGround => _lastTimeOnGround;
        [SerializeField] private bool _onSlopeRight;
        [SerializeField] private bool _onSlopeLeft;
        [SerializeField] private bool _onSlopeCenter;
        public bool OnSlope => _onSlopeLeft || _onSlopeRight || _onSlopeCenter;
        private bool _previousOnSlope;
        public bool PreviousOnSlope => _previousOnSlope;
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

            var footSize = new Vector2(size.x / 2, _extraDistanceGround);
            var distance = size.y / 2 + _extraDistanceGround;
            
            RaycastHit2D hit2D = Physics2D.BoxCast(position, footSize, 0f, Vector2.down, distance, _groundLayer);
            
            _isGrounded = hit2D.collider != null;
            if(_isGrounded) _lastTimeOnGround = Time.time;
        }

        private void SlopeCheck()
        {
            _previousOnSlope = OnSlope;
            _previousSlopeNormal = _slopeNormal;
            
            var position = _playerPositionCache;
            var size = _playerSizeCache;
            var positionLeft = position.With(x : position.x - size.x / 2);
            var positionRight = position.With(x : position.x + size.x / 2);
            
            var distance = size.y / 2 + _extraDistanceSlope;
            
            _rightSlopeHit = Physics2D.Raycast(positionRight, Vector2.down, distance, _groundLayer);
            _leftSlopeHit = Physics2D.Raycast(positionLeft, Vector2.down, distance, _groundLayer);
            _centerSlopeHit = Physics2D.Raycast(position, Vector2.down, distance, _groundLayer);

            Vector3 _slopeNormalRight;
            Vector3 _slopeNormalLeft;
            Vector3 _slopeNormalCenter;
            
            if (_rightSlopeHit.collider != null)
            {
                var slopeAngle = Vector2.Angle(Vector2.up, _rightSlopeHit.normal);
                _onSlopeRight = slopeAngle < _maxSlopeAngle && slopeAngle != 0;
                _slopeNormalRight = _rightSlopeHit.normal;
            }
            else
            {
                _onSlopeRight = false;
                _slopeNormalRight = Vector3.zero;
            }
            
            if (_leftSlopeHit.collider != null)
            {
                var slopeAngle = Vector2.Angle(Vector2.up, _leftSlopeHit.normal);
                _onSlopeLeft = slopeAngle < _maxSlopeAngle && slopeAngle != 0;
                _slopeNormalLeft = _leftSlopeHit.normal;
            }
            else
            {
                _onSlopeLeft = false;
                _slopeNormalLeft = Vector3.zero;
            }
            
            if (_centerSlopeHit.collider != null)
            {
                var slopeAngle = Vector2.Angle(Vector2.up, _centerSlopeHit.normal);
                _onSlopeCenter = slopeAngle < _maxSlopeAngle && slopeAngle != 0;
                _slopeNormalCenter = _centerSlopeHit.normal;
            }
            else
            {
                _onSlopeCenter = false;
                _slopeNormalCenter = Vector3.zero;
            }
            
            bool previousEqual = (_slopeNormalLeft == _previousSlopeNormal || _slopeNormalRight == _previousSlopeNormal || _slopeNormalCenter == _previousSlopeNormal) && _previousOnSlope;
            bool slopeGround = _slopeNormalLeft == Vector3.up || _slopeNormalRight == Vector3.up || _slopeNormalCenter == Vector3.up;
            if (!previousEqual)
            {
                if (!OnSlope)
                {
                    if(slopeGround) _slopeNormal = Vector3.up;
                    else _slopeNormal = Vector3.zero;
                }
                else
                {
                    if(_onSlopeRight) _slopeNormal = _rightSlopeHit.normal;
                    else if(_onSlopeLeft) _slopeNormal = _leftSlopeHit.normal;
                    else if(_centerSlopeHit) _slopeNormal = _centerSlopeHit.normal;
                } 
            }
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

            var headSize = new Vector2(size.x / 2, _extraDistanceHead);
            var distance = size.y / 2 + _extraDistanceHead;

            RaycastHit2D hit2D = Physics2D.BoxCast(position, headSize, 0f, Vector2.up, distance, _groundLayer);

            _isHeadBlocked = hit2D.collider != null;
        }
        
        public Vector2 ProjectOnSlopeDirection(Vector2 inputDirection)
        {
            if(_slopeNormal == Vector3.zero) return inputDirection;
            Vector2 tangent = Vector2.Perpendicular(_slopeNormal).normalized;
            return (tangent * Vector2.Dot(tangent, inputDirection)).normalized;
        }

        public void CachePlayerVariables()
        {
            _invoker.CenterPosition.Request(out _playerPositionCache);
            _invoker.Size.Request(out _playerSizeCache);
        }

        public override void Apply(ref InputPayload @event) { }
    }
}