using UnityEngine;

namespace Code.Gameplay.Objects
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        public float parallaxFactorX;
        public float parallaxFactorY;

        public void MoveX(float delta)
        {
            Vector3 newPos = transform.localPosition;
            newPos.x -= delta * parallaxFactorX;
            
            transform.localPosition = newPos;
        }

        public void MoveY(float delta)
        {
            Vector3 newPos = transform.localPosition;
            newPos.y -= delta * parallaxFactorY;
            
            transform.localPosition = newPos;
        }
    }
}