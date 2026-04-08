using UnityEngine;

/// <summary>
/// Движение игрока на CharacterController.
/// Не скользит, не застревает, стабильно ходит по склонам.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed = 7f;

    [Header("Налаштування")]
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float turnSpeed = 7f;
    [SerializeField] private float minTurnSpeedDegrees = 420f;
    [SerializeField] private float maxTurnSpeedDegrees = 900f;

    [Header("Гравітація та стрибок")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Маленькая сила вниз чтобы прижимать к земле")]
    [SerializeField] private float groundStickForce = -2f;

    [Header("Склоны")]
    [Tooltip("Максимальный угол склона (90 = любой)")]
    [SerializeField] private float slopeLimit = 89f;
    [Tooltip("Высота автоподъёма на ступеньки")]
    [SerializeField] private float stepOffset = 0.5f;

    [Header("Посилання")]
    [SerializeField] private Transform playerCameraTransform;

    private CharacterController controller;
    private PlayerInput input;
    private PlayerStamina playerStamina;
    private Vector3 verticalVelocity;

    /// <summary>Текущая скорость (для аниматора)</summary>
    public Vector3 Velocity => CanUseController() ? controller.velocity : Vector3.zero;

    /// <summary>На земле? (для аниматора и способностей)</summary>
    public bool IsGrounded => CanUseController() && controller.isGrounded;



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
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        playerStamina = GetComponent<PlayerStamina>();

        controller.slopeLimit = slopeLimit;
        controller.stepOffset = stepOffset;
    }

    void Update()
    {
        if (input == null || playerStamina == null || !CanUseController()) return;

        // --- Земля ---
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            // Прижимаем к земле (не отскакиваем)
            verticalVelocity.y = groundStickForce;
        }

        // --- Направление ---
        Vector3 camForward = playerCameraTransform.forward;
        Vector3 camRight = playerCameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * input.MovementInput.y + camRight * input.MovementInput.x);
        moveDirection.Normalize();

        // --- Скорость ---
        bool isSprinting = input.IsSprinting && playerStamina.CanSprint;
        float currentSpeed = moveSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        // --- Горизонтальное движение ---
        Vector3 horizontalMove = moveDirection * currentSpeed * Time.deltaTime;

        // --- Гравитация ---
        verticalVelocity.y += gravity * Time.deltaTime;

        // --- Финальное движение ---
        if (!CanUseController()) return;
        controller.Move(horizontalMove + verticalVelocity * Time.deltaTime);

        // --- Поворот ---
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            float turnSpeedDegrees = CalculateTurnSpeedDegrees(angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeedDegrees * Time.deltaTime);
        }
    }

    private float CalculateTurnSpeedDegrees(float angle)
    {
        // Поддержка старых значений turnSpeed (раньше это был коэффициент Slerp).
        float baseTurnSpeedDegrees = turnSpeed <= 30f ? turnSpeed * 90f : turnSpeed;
        float boostedTurnSpeed = Mathf.Lerp(baseTurnSpeedDegrees, maxTurnSpeedDegrees, Mathf.InverseLerp(30f, 150f, angle));
        return Mathf.Max(minTurnSpeedDegrees, boostedTurnSpeed);
    }

    /// <summary>
    /// Прыжок. Вызывается из способностей (DakotaraptorAbility/PectinodonAbility).
    /// </summary>
    public void Jump(float jumpSpeed)
    {
        if (CanUseController() && controller.isGrounded)
        {
            verticalVelocity.y = jumpSpeed;
        }
    }

    private bool CanUseController()
    {
        return controller != null
            && controller.enabled
            && controller.gameObject.activeInHierarchy;
    }
}
