using UnityEngine;

namespace Code.Gameplay.Anim
{
    public class AnimTest : MonoBehaviour
    {
        public Transform arm;              // The arm transform (pivoted at shoulder)
        public Transform playerSprite;     // The player’s sprite (body)
        private Camera cam;

        void Start()
        {
            cam = Camera.main;
        }

        void Update()
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mousePos - arm.position;
            direction.z = 0;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arm.rotation = Quaternion.Euler(0, 0, angle);

            // Flip the player sprite if aiming to the left
            if (mousePos.x < transform.position.x)
            {
                playerSprite.GetComponent<SpriteRenderer>().flipX = true;
                arm.GetComponent<SpriteRenderer>().flipY = true;
            }
            else
            {
                playerSprite.GetComponent<SpriteRenderer>().flipX = false;
                arm.GetComponent<SpriteRenderer>().flipY = false;
            }
        }
    }
}