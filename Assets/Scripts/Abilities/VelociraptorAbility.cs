using UnityEngine;
using System.Collections;

/// <summary>
/// Велоцираптор:
/// - Пассивка: прыжок (Space)
/// - Активная: Серповидный удар (Q) — через хитбокс наносит урон + кровотечение (DOT)
/// </summary>
public class VelociraptorAbility : MonoBehaviour, IDinosaurAbility
{
    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Серповидный удар")]
    [SerializeField] private float abilityCooldown = 8f;
    [SerializeField] private float abilityDuration = 0.3f;
    [SerializeField] private int bleedDamagePerTick = 3;
    [SerializeField] private float bleedDuration = 5f;

    [Header("Ссылки")]
    [SerializeField] private Hitbox attackHitbox;

    private float cooldownTimer = 0f;
    private PlayerInput playerInput;
    private PlayerAttack playerAttack;
    private Rigidbody rb;
    private int currentAttackDamage;

    public string AbilityName => "Серповидный удар";
    public float Cooldown => abilityCooldown;
    public bool IsReady => cooldownTimer <= 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerAttack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody>();

        // Автопоиск хитбокса если не задан (при добавлении через AddComponent)
        if (attackHitbox == null)
            attackHitbox = GetComponentInChildren<Hitbox>(true);

        // Подтягиваем groundLayer из PlayerMovement (там он уже настроен в инспекторе)
        if (groundLayer == 0)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
            {
                groundLayer = movement.GroundLayer;
                groundCheckDistance = movement.GroundCheckDistance;
            }
        }
    }

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
        currentAttackDamage = e.NewAttackDamage;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (playerInput == null) return;

        // Прыжок — пассивная механика
        if (playerInput.JumpInput && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Серповидный удар — активная способность
        if (playerInput.AbilityInput && IsReady)
        {
            Activate();
        }
    }

    public void Activate()
    {
        cooldownTimer = abilityCooldown;
        StartCoroutine(AbilityCoroutine());
        EventBroker.Publish(new AbilityUsedEvent { AbilityName = AbilityName, Cooldown = abilityCooldown });
    }

    private IEnumerator AbilityCoroutine()
    {
        // Используем тот же хитбокс что и для обычной атаки,
        // но с эффектом кровотечения
        attackHitbox.ActivateWithBleed(currentAttackDamage, bleedDamagePerTick, bleedDuration);
        attackHitbox.gameObject.SetActive(true);

        yield return new WaitForSeconds(abilityDuration);

        attackHitbox.gameObject.SetActive(false);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
}
