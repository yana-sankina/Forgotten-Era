using UnityEngine;
using System.Collections;

/// <summary>
/// Дакотараптор:
/// - Пассивка: прыжок (Space)
/// - Активная: Серповидный удар (Q) — через хитбокс наносит урон + кровотечение (DOT)
/// </summary>
public class DakotaraptorAbility : MonoBehaviour, IDinosaurAbility
{
    [Header("Прыжок")]
    [SerializeField] private float jumpSpeed = 8f;

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
    private PlayerMovement playerMovement;
    private int currentAttackDamage;

    public string AbilityName => "Серповидный удар";
    public float Cooldown => abilityCooldown;
    public bool IsReady => cooldownTimer <= 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();

        if (attackHitbox == null)
            attackHitbox = GetComponentInChildren<Hitbox>(true);
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
        if (playerInput.JumpInput && playerMovement != null)
        {
            playerMovement.Jump(jumpSpeed);
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


}
