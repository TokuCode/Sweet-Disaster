using Code.Gameplay.Character;
using Code.Helpers.Utils;
using Code.Systems.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class PlayerAim : PlayerHUDBase
    {
        private Camera _main;
        [SerializeField] private float _aimDistance;
        
        [Header("UI Elements")]
        [SerializeField] private Image _aimImage;
        [SerializeField] private RectTransform _aimRect;

        private void Awake()
        {
            _main = Camera.main;
        }

        protected override void Update()
        {
            base.Update();
            if (!Assigned) return;
            
            UpdateAimPosition();
        }

        protected override void TryCachePlayer()
        {
            base.TryCachePlayer();
            if (!Assigned) return;
            SetMouseColor();
        }

        private void SetMouseColor()
        {
            Color aimColor = PlayerVisibility.Instance.GetPlayerColor(Player);
            _aimImage.color = aimColor;
        }

        private void UpdateAimPosition()
        {
            if (Player == null) return;
            
            Vector3 positionToTrack = Player.GunTip.position + InputReader.Instance.HandleDirection * _aimDistance;
            _aimRect.transform.position = CameraUtils.WorldToScreenPosition(positionToTrack, _main);
        }
    }
}