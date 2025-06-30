using System;
using Code.Helpers.Utils;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Visuals
{
    public class AnimationAimingHandler : NetworkBehaviour
    {
        [SerializeField] private Transform _gunTip;
        [SerializeField] private Transform arm;
        private Vector3 _mouseWorldPos;
        [SerializeField] private float offsetAngle;

        private void Start()
        {
            var direction = _gunTip.localPosition.normalized;
            offsetAngle = Vector3.Angle(direction, arm.right);
        }

        void Update()
        {
            if (!IsOwner) return;

            Vector3 direction = InputReader.Instance.HandleDirection;
            
            bool isFacingLeft = direction.x < 0;
            transform.localScale = new Vector3(isFacingLeft ? -1 : 1, transform.localScale.y, transform.localScale.z);
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            if (isFacingLeft)
                angle += 180f;

            arm.rotation = Quaternion.Euler(0, 0, angle + (isFacingLeft ? +1 : -1) * offsetAngle);
        }
    }
}