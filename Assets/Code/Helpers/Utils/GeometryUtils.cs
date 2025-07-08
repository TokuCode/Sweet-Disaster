using UnityEngine;

namespace Code.Helpers.Utils
{
    public static class GeometryUtils
    {
        public static bool Intersect(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            return orientation(A, C, D) != orientation(B, C , D) &&  orientation(A, B, C) != orientation(A, B, D);
            
            bool orientation(Vector3 A, Vector3 B, Vector3 C)
                => (C.y - A.y) * (B.x - A.x) > (B.y - A.y) * (C.x - A.x);
        }
    }
}