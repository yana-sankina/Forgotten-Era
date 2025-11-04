using UnityEngine;

public class ThirdPersonCameraCollision : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float zoomSpeed = 2f;
    public float xSpeed = 120f;
    public float ySpeed = 120f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public LayerMask collisionLayers;

    private float currentDistance;
    private float rotationYAxis = 0f;
    private float rotationXAxis = 0f;
    private float velocityX = 0f;
    private float velocityY = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationYAxis = angles.y;
        rotationXAxis = angles.x;
        currentDistance = distance;

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    void LateUpdate()
    {
        if (target)
        {
            velocityX += xSpeed * Input.GetAxis("Mouse X") * 0.02f;
            velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.02f;

            rotationYAxis += velocityX;
            rotationXAxis -= velocityY;
            rotationXAxis = ClampAngle(rotationXAxis, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);

            RaycastHit hit;
            Vector3 desiredPosition = target.position - (rotation * Vector3.forward * currentDistance);
            if (Physics.Linecast(target.position, desiredPosition, out hit, collisionLayers))
            {
                currentDistance = Mathf.Clamp(hit.distance, minDistance, currentDistance);
            }

            transform.position = target.position - (rotation * Vector3.forward * currentDistance);
            transform.rotation = rotation;


            velocityX = Mathf.Lerp(velocityX, 0, 0.2f);
            velocityY = Mathf.Lerp(velocityY, 0, 0.2f);
        }
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
