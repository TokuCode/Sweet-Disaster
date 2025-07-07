using UnityEngine;

namespace Code.Helpers.Utils
{
    public static class DirectionToRotation
    {
        public static Quaternion GetRotation(Vector3 direction)
        { 
            direction.Normalize();
            float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(rotation, Vector3.forward);
        }
    }
}