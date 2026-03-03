using UnityEngine;
using System.Collections;

/// <summary>
/// Тиранозавр:
/// - Активная: Сокрушительный удар (Q) — через хитбокс ⅓ базового урона + стан 1.5с
/// </summary>
public class TyrannosaurusAbility : MonoBehaviour, IDinosaurAbility
{
    [Header("Сокрушительный удар")]
    [SerializeField] private float abilityCooldown = 10f;
    [SerializeField] private float abilityDuration = 0.4f;
    [SerializeField] private float stunDuration = 1.5f;
    [Tooltip("Множитель от базового урона (⅓ = 0.33)")]
    [SerializeField] private float damageMultiplier = 0.33f;

    [Header("Ссылки")]
    [SerializeField] private Hitbox attackHitbox;

    private float cooldownTimer = 0f;
    private PlayerInput playerInput;
    private int currentAttackDamage;

    public string AbilityName => "Сокрушительный удар";
    public float Cooldown => abilityCooldown;
    public bool IsReady => cooldownTimer <= 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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
        int abilityDamage = Mathf.Max(1, (int)(currentAttackDamage * damageMultiplier));

        attackHitbox.ActivateWithStun(abilityDamage, stunDuration);
        attackHitbox.gameObject.SetActive(true);

        yield return new WaitForSeconds(abilityDuration);

        attackHitbox.gameObject.SetActive(false);
    }
}
