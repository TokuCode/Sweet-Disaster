using Code.Helpers.Singleton;
using UnityEngine;

namespace Code.Gameplay.Objects.ObjectBox
{
    public class SceneBox : Singleton<SceneBox>
    {
        [SerializeField] private ObjectBox _box;
        
        public bool Inside(Vector3 position) => _box.InsideBox(position);
        public bool Outside(Vector3 position) => _box.OutsideBox(position);

    }
}