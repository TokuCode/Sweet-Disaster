using UnityEngine;

namespace Code.Helpers.Utils
{
    public static class CameraUtils
    {
        public static Vector3 ScreenToWorldPoint(Vector2 screenPoint, Camera camera)
        {
            var worldPoint = camera.ScreenToWorldPoint(screenPoint);
            worldPoint.z = 0;
            return worldPoint;
        }
        
        public static Vector3 ScreenToWorldPoint(Vector2 screenPoint)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
                return ScreenToWorldPoint(screenPoint, mainCamera);
            
            Debug.LogWarning("Main camera not found.");
            return Vector3.zero;
        }

        public static Vector2 WorldToScreenPosition(Vector3 worldPoint, Camera camera)
        {
            var screenPoint = camera.WorldToScreenPoint(worldPoint);
            screenPoint.z = 0;
            return screenPoint;
        }
    }
}