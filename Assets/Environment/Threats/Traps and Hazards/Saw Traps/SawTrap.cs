using UnityEngine;
using UnityEngine.UIElements;

public class SawbladeTrap : MonoBehaviour
{
    [SerializeField] private float targetDistance = 0f; //Must be 0, 4, 8, or 16
    [SerializeField] private Vector2 targetDirection = Vector2.right; //The direction that the sawblade will travel n units towards.  
    [SerializeField] private float speed = 0.5f; //Sawblade movement speed
    [SerializeField] private float rotationSpeed = 360f; //Rotation speed in degrees

    private Vector3 origin;
    private Vector3 targetPoint;
    private float lerpTimer = 0f;
    private bool reversing = false;

    void Start()
    {
        origin = transform.position;

        if (targetDistance > 0f)
        {
            // Normalize direction and calculate target point
            Vector2 normalizedDir = targetDirection.normalized;
            targetPoint = origin + (Vector3)(normalizedDir * targetDistance);
        }
    }

    void Update()
    {
        if (targetDistance != 0f)
        {
            //Travel towards and back from target direction.  
            lerpTimer += Time.deltaTime * speed * (reversing ? -1 : 1);
            float t = Mathf.PingPong(lerpTimer, 1f);

            transform.position = Vector3.Lerp(origin, targetPoint, t);
        }


        this.gameObject.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
