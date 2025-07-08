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

        public static bool CheckIfInsideCamera(Camera camera, Vector3 position)
        {
            var viewport = camera.WorldToViewportPoint(position);
            return viewport.x is > 0 and < 1 && viewport.y is > 0 and < 1;
        }

        public static Vector3 WorldToViewportPosition(Vector3 worldPoint, Camera camera)
        {
            var viewport = camera.WorldToViewportPoint(worldPoint);
            viewport.z = 0;
            return viewport;
        }

        public static Vector3 ViewportToScreenPosition(Vector3 viewportPoint, Camera camera)
        {
            var screen = camera.ViewportToScreenPoint(viewportPoint);
            screen.z = 0;
            return screen;
        }

        public static Vector3 ViewportToWorldPosition(Vector3 viewportPoint, Camera camera)
        {
            var world = camera.ViewportToWorldPoint(viewportPoint);
            world.z = 0;
            return world;
        }
    }
}