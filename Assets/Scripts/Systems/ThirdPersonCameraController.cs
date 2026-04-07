using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
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
    
    [Header("Safety")]
    [Tooltip("Минимальная высота камеры относительно игрока (мировой Y).")]
    [SerializeField] private float minCameraRelativeY = -0.1f;
    
    [Header("Ground Clamp")]
    [Tooltip("Слои земли для нижнего зажима камеры. Если не задано, используется collisionLayers.")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundProbeStartHeight = 3f;
    [SerializeField] private float groundProbeDistance = 12f;
    [SerializeField] private float groundClearance = 0.2f;

    private float currentDistance;
    private float rotationYAxis = 0f;
    private float rotationXAxis = 0f;
    private float velocityX = 0f;
    private float velocityY = 0f;
    
    private PlayerInput playerInput;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationYAxis = angles.y;
        rotationXAxis = angles.x;
        currentDistance = distance;

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
        
        if (target != null)
        {
            playerInput = target.GetComponent<PlayerInput>();
        }
    }

    void LateUpdate()
    {
        if (target && playerInput != null)
        {
            velocityX += xSpeed * playerInput.MouseInput.x * 0.02f;
            velocityY += ySpeed * playerInput.MouseInput.y * 0.02f;

            rotationYAxis += velocityX;
            rotationXAxis -= velocityY;
            rotationXAxis = ClampAngle(rotationXAxis, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);
            
            float scroll = playerInput.ScrollInput;
            currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
            
            RaycastHit hit;
            Vector3 desiredPosition = target.position - (rotation * Vector3.forward * currentDistance);
            if (Physics.Linecast(target.position, desiredPosition, out hit, collisionLayers))
            {
                currentDistance = Mathf.Clamp(hit.distance, minDistance, currentDistance);
            }

            Vector3 cameraPosition = target.position - (rotation * Vector3.forward * currentDistance);
            float minAllowedY = target.position.y + minCameraRelativeY;
            if (cameraPosition.y < minAllowedY)
                cameraPosition.y = minAllowedY;

            int groundMask = groundLayers.value != 0 ? groundLayers.value : collisionLayers.value;
            Vector3 probeOrigin = new Vector3(
                cameraPosition.x,
                target.position.y + Mathf.Max(0.1f, groundProbeStartHeight),
                cameraPosition.z);

            if (Physics.Raycast(
                probeOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                Mathf.Max(0.5f, groundProbeStartHeight + groundProbeDistance),
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                Transform hitTransform = groundHit.transform;
                bool hitPlayer = hitTransform == target || hitTransform.IsChildOf(target);
                if (!hitPlayer)
                {
                    float minGroundY = groundHit.point.y + groundClearance;
                    if (cameraPosition.y < minGroundY)
                        cameraPosition.y = minGroundY;
                }
            }

            transform.position = cameraPosition;
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
