using Code.Helpers.Singleton;
using UnityEngine;

namespace Code.Gameplay.Objects.SceneBox
{
    public class SceneBox : Singleton<SceneBox>
    {
        [SerializeField] private BoxCollider2D _box;

        [Header("Scene Box Padding")] 
        [SerializeField] private float _left;
        [SerializeField] private float _right;
        [SerializeField] private float _top;
        [SerializeField] private float _bottom;
        
        public float Left => _box.bounds.min.x - _left;
        public float Right => _box.bounds.max.x + _right;
        public float Top => _box.bounds.max.y + _top;
        public float Bottom => _box.bounds.min.y - _bottom;

        private void OnDrawGizmos()
        {
            if(_box == null) return;
            
            Gizmos.color = Color.green;
            
            Vector2 topLeft = new Vector2(Left, Top);
            Vector2 topRight = new Vector2(Right, Top);
            Vector2 bottomLeft = new Vector2(Left, Bottom);
            Vector2 bottomRight = new Vector2(Right, Bottom);
            
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
    }
}