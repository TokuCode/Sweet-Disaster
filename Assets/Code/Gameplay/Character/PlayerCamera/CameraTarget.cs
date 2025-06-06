using Code.Helpers.Singleton;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class CameraTarget : Singleton<CameraTarget>
    {
        [Header("Settings")] 
        [SerializeField] private float _height;
        [SerializeField] private GameObject _cameraTarget;

        private void Update()
        {
            if (_cameraTarget != null)
            {
                transform.position = _cameraTarget.transform.position + Vector3.up * _height;
            }
        }

        public void SetTarget(GameObject target)
        {
            _cameraTarget = target;
        }
    }
}