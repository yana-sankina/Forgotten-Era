using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed;

    [Header("Налаштування")] [SerializeField]
    private float sprintMultiplier = 2f;

    [SerializeField] private float turnSpeed = 15f;

    [Header("Контроль в воздухе")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [Tooltip("0 = нет управления в воздухе, 1 = полное управление")]
    [SerializeField] private float airControlFactor = 0.05f;

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

        bool grounded = IsGrounded();

        if (grounded)
        {
            // На земле — полный контроль
            rb.linearVelocity = new Vector3(
                moveDirection.x * currentSpeed,
                rb.linearVelocity.y,
                moveDirection.z * currentSpeed
            );
        }
        else
        {
            // В воздухе — почти нет контроля, сохраняем инерцию
            // Спринт ЧАСТИЧНО влияет на прыжок (70% от скорости спринта)
            float airMaxSpeed = isSprinting ? moveSpeed * sprintMultiplier * 0.7f : moveSpeed;

            Vector3 airVelocity = rb.linearVelocity;
            airVelocity.x += moveDirection.x * moveSpeed * airControlFactor;
            airVelocity.z += moveDirection.z * moveSpeed * airControlFactor;

            // Ограничиваем горизонтальную скорость
            Vector2 horizontalVel = new Vector2(airVelocity.x, airVelocity.z);
            if (horizontalVel.magnitude > airMaxSpeed)
            {
                horizontalVel = horizontalVel.normalized * airMaxSpeed;
            }

            rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.y);
        }

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

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
}