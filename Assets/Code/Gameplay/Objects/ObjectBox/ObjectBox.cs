using UnityEngine;

namespace Code.Gameplay.Objects.ObjectBox
{
    public class ObjectBox : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D _box;

        [Header("Scene Box Padding")] 
        [SerializeField] private float _left;
        [SerializeField] private float _right;
        [SerializeField] private float _top;
        [SerializeField] private float _bottom;
        
        [Header("Gizmos")]
        [SerializeField] private Color _boxColor;
        
        public float Left => _box.bounds.min.x - _left;
        public float Right => _box.bounds.max.x + _right;
        public float Top => _box.bounds.max.y + _top;
        public float Bottom => _box.bounds.min.y - _bottom;

        public bool InsideBox(Vector3 position)
        {
            bool inX = position.x > Left && position.x < Right;
            bool inY = position.y > Bottom && position.y < Top; 
            return inX && inY;
        }
        
        public bool OutsideBox(Vector3 position) => !InsideBox(position);

        public Vector3 ConstrainToBox(Vector3 position)
        {
            float x = Mathf.Clamp(position.x, Left, Right);
            float y = Mathf.Clamp(position.y, Bottom, Top);
            return new Vector3(x, y);
        }

        public float Distance(Vector3 position)
        {
            return Mathf.Min(Mathf.Abs(position.x - Left), Mathf.Abs(position.x - Right), Mathf.Abs(position.y - Top), Mathf.Abs(position.y - Bottom));
        }
       
        private void OnDrawGizmos()
        {
            if(_box == null) return;
            
            Gizmos.color = _boxColor;
            
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