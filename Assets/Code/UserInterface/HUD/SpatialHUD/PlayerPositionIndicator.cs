using System.Collections.Generic;
using Code.Gameplay.Character;
using Code.Helpers.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class PlayerPositionIndicator : MonoBehaviour
    {
        private Camera _main;
        private PlayerPublicInfo _playerInfo;
        private bool _started;

        [Header("UI Elements")]
        [SerializeField] private Image _arrow;
        [SerializeField] private RectTransform _rect;

        private void Start()
        {
            _main = Camera.main;
        }

        public void CachePlayerInfo(PlayerPublicInfo playerInfo)
        { 
            _playerInfo = playerInfo;  
            _started = true;
            _arrow.color = playerInfo.playerColor;
        }

        private void Update()
        {
            if (!_started)
            {
                _arrow.gameObject.SetActive(false);
                return;
            }

            if (_playerInfo.player.outOfBattle.Value || _playerInfo.player.defeated.Value)
            {
                _arrow.gameObject.SetActive(false);
                return;
            }
            
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            var position= _playerInfo.player.Invoker.CenterPosition.Request(out var playerPosition).success ? playerPosition : Vector3.zero;
            Vector3 size = _playerInfo.player.Invoker.Size.Request(out var playerSize).success ? playerSize : Vector3.zero;

            var corners = new List<Vector3>
            {
                position + size/2,
                position - size/2,
                position + new Vector3(-size.x, size.y) /2,
                position + new Vector3(size.x, -size.y) /2
            };

            bool inView = false;
            foreach (var corner in corners)
            {
                bool cornerInView = CameraUtils.CheckIfInsideCamera(_main, corner);
                if(cornerInView) inView = true;
            }

            if (inView)
            {
                _arrow.gameObject.SetActive(false);
                return;
            }
            
            _arrow.gameObject.SetActive(true);
            var viewport = CameraUtils.WorldToViewportPosition(position, _main);
            var min = .05f;
            var max = .95f;
            var topLeft = new Vector3(min, max);
            var bottomRight = new Vector3(max, min);
            var topRight = new Vector3(max, max);
            var bottomLeft = new Vector3(min, min);
            var center = new Vector3(.5f, .5f);
            bool topIntersect = GeometryUtils.Intersect(center, viewport, topLeft, topRight);
            bool bottomIntersect = GeometryUtils.Intersect(center, viewport, bottomLeft, bottomRight);
            bool leftIntersect = GeometryUtils.Intersect(center, viewport, bottomLeft, topLeft);
            bool rightIntersect = GeometryUtils.Intersect(center, viewport, bottomRight, topRight);
            float slope = (viewport.y - center.y)/(viewport.x - center.x);
            var finalPosition = Vector3.zero;

            if (topIntersect)
            {
                float x = (topLeft.y - center.y) / slope + center.x;
                finalPosition = new Vector3(x, topLeft.y, 0);
            }
            else if (bottomIntersect)
            {
                float x = (bottomLeft.y - center.y) / slope + center.x;
                finalPosition = new Vector3(x, bottomLeft.y, 0);
            }
            else if (leftIntersect)
            {
                float y = (bottomLeft.x - center.x) * slope + center.y;
                finalPosition = new Vector3(bottomLeft.x, y, 0);
            }
            else if (rightIntersect)
            {
                float y = (bottomRight.x - center.x) * slope + center.y;
                finalPosition = new Vector3(bottomRight.x, y, 0);
            }

            _rect.transform.position = CameraUtils.ViewportToScreenPosition(finalPosition, _main);
            
            var centerWorld = CameraUtils.ViewportToWorldPosition(new (.5f, .5f), _main);
            var diff = position - centerWorld;
            
            _arrow.transform.up = diff.normalized;
        }
    }
}