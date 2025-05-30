using UnityEngine;

public class DefaultPlayer : MonoBehaviour
{
    [SerializeField] private Transform arm;
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

        if (mousePos.x < transform.position.x)
        {
            gameObject.GetComponent<SpriteRenderer>().flipX = true;
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().flipX = true;
            transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().flipY = true;
        }
        else
        {
            gameObject.GetComponent<SpriteRenderer>().flipX = false;
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().flipX = false;
            transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().flipY = false;
        }
    }
}