using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class SpatialHUDAligner : PlayerHUDBase
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private Vector3 _offset;

        protected override void Update()
        {
            base.Update();
            if(!Assigned) return;
            
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Player.Invoker.CenterPosition.Request(out var position);
            _transform.position = position + _offset;
        }
    }
}