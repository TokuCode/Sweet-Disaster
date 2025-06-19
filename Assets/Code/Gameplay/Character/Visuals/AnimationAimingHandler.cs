using Code.Helpers.Utils;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Visuals
{
    public class AnimationAimingHandler : NetworkBehaviour
    {
        [SerializeField] private Transform arm;
        private Vector3 _mouseWorldPos;

        void Update()
        {
            if (!IsOwner) return;

            Vector3 direction = InputReader.Instance.HandleDirection;
            _mouseWorldPos = CameraUtils.ScreenToWorldPoint(Input.mousePosition);
            
            bool isFacingLeft = _mouseWorldPos.x < transform.position.x;
            transform.localScale = new Vector3(isFacingLeft ? -1 : 1, transform.localScale.y, transform.localScale.z);
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            if (isFacingLeft)
                angle += 180f;

            arm.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}