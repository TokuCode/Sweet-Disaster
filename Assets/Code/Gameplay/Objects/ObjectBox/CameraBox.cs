using Code.Helpers.Singleton;
using UnityEngine;

namespace Code.Gameplay.Objects.ObjectBox
{
    public class CameraBox : Singleton<CameraBox>
    {
        [SerializeField] private ObjectBox _box;
        [SerializeField] private ObjectBox _outBox;
        
        public bool Inside(Vector3 position) => _box.InsideBox(position);
        public bool Outside(Vector3 position) => _box.OutsideBox(position); 
        
        public Vector3 ConstrainToView(Vector3 position) => _outBox.ConstrainToBox(position);
        public float Distance(Vector3 position) => _box.Distance(position);
    }
}