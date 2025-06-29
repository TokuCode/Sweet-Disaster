using UnityEngine;

namespace Code.Gameplay.Character.Command
{
    public class PlayerCommandInvoker
    {
        private readonly PlayerController player;

        private Transform _transform;
        private Rigidbody2D _rigidbody;
        private CapsuleCollider2D _collider;
        private Transform _gunTip;

        public RequestCenterPosition CenterPosition { get; }
        public LocalScaleHandler LocalScale { get; }
        public SizeHanlder Size { get; }
        public VelocityHandlder Velocity { get; }
        public GravityScaleHanlder GravityScale { get; }
        public AddForceCommand AddForce { get; }
        public KnockbackRawCommand Knockback { get; }
        public RequestPosition GunTipPosition { get; }

        public PlayerCommandInvoker(PlayerController player)
        {
            this.player = player;
            
            _transform = player.transform;
            _rigidbody = player.rigidbody;
            _collider = player.collider;
            _gunTip = player.GunTip;

            CenterPosition = new(_transform, _collider);
            LocalScale = new(_transform);
            Size = new(_collider);
            Velocity = new(_rigidbody);
            GravityScale = new(_rigidbody);
            AddForce = new(_rigidbody);
            Knockback = new(_rigidbody);
            GunTipPosition = new(_gunTip);
        }
    }
}