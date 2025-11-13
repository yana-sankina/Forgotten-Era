using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed;

    [Header("Налаштування")] [SerializeField]
    private float sprintMultiplier = 2f;

    [SerializeField] private float turnSpeed = 15f;

    [Header("Посилання")] [SerializeField] private Transform playerCameraTransform;

    private Rigidbody rb;
    private PlayerInput input;
    private PlayerStamina playerStamina;

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }

    private void OnStatsUpdated(PlayerStatsUpdatedEvent e)
    {
        moveSpeed = e.NewMoveSpeed;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();

        playerStamina = GetComponent<PlayerStamina>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (input == null || playerStamina == null) return;

        Vector3 camForward = playerCameraTransform.forward;
        Vector3 camRight = playerCameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * input.MovementInput.y + camRight * input.MovementInput.x);
        moveDirection.Normalize();

        bool isSprinting = input.IsSprinting && playerStamina.CanSprint;

        float currentSpeed = moveSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        rb.linearVelocity = new Vector3(
            moveDirection.x * currentSpeed,
            rb.linearVelocity.y,
            moveDirection.z * currentSpeed
        );

        rb.angularVelocity = Vector3.zero;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            var eulerRotation = newRotation.eulerAngles;
            eulerRotation.x = 0;
            eulerRotation.z = 0;

            rb.MoveRotation(Quaternion.Euler(eulerRotation));
        }
    }
}