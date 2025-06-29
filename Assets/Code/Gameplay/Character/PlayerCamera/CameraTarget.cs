using Code.Helpers.Singleton;
using Code.Networking.Session;
using Unity.Cinemachine;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class CameraTarget : Singleton<CameraTarget>
    {
        [Header("Settings")] 
        private CinemachineTargetGroup _cameraTargetGroup;
        [SerializeField] private float _playerRadius;
        [SerializeField] private float _playerRadiusPractice;
        [SerializeField] private float _mainPlayerWeight;
        [SerializeField] private float _otherPlayerWeight;

        protected override void Awake()
        {
            base.Awake();
            _cameraTargetGroup = GetComponent<CinemachineTargetGroup>();
        }

        public void AddTarget(Transform target, bool isMainPlayer)
        {
            _cameraTargetGroup.AddMember(target, isMainPlayer ? _mainPlayerWeight : _otherPlayerWeight, SessionManager.Instance.IsPracticeMode ? _playerRadiusPractice : _playerRadius);
        }
    }
}