using UnityEngine;

namespace Code.Gameplay.Character
{
    public class CameraTarget : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _height;

        private void Update()
        {
            var target = PlayerController.Singleton;
            if (target != null)
            {
                transform.position = target.transform.position + Vector3.up * _height;
            }
        }
    }
}