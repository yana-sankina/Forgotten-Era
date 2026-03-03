using UnityEngine;
using System.Collections;

/// <summary>
/// Троодон:
/// - Пассивка 1: Прыжок (Space)
/// - Пассивка 2: Ночное зрение (визуал — настраивается пользователем через постпроцессинг)
/// - Активная: Обострённый слух (Q) — подсвечивает всех существ в большом радиусе
/// </summary>
public class TroodonAbility : MonoBehaviour, IDinosaurAbility
{
    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Обострённый слух")]
    [SerializeField] private float abilityCooldown = 15f;
    [SerializeField] private float scanRadius = 30f;
    [SerializeField] private float highlightDuration = 5f;

    private float cooldownTimer = 0f;
    private PlayerInput playerInput;
    private Rigidbody rb;

    public string AbilityName => "Обострённый слух";
    public float Cooldown => abilityCooldown;
    public bool IsReady => cooldownTimer <= 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (playerInput == null) return;

        // Прыжок — пассивная механика (общая с велоцираптором)
        if (playerInput.JumpInput && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Обострённый слух — активная способность
        if (playerInput.AbilityInput && IsReady)
        {
            Activate();
        }
    }

    public void Activate()
    {
        cooldownTimer = abilityCooldown;
        StartCoroutine(ScanCoroutine());

        EventBroker.Publish(new AbilityUsedEvent { AbilityName = AbilityName, Cooldown = abilityCooldown });
    }

    private IEnumerator ScanCoroutine()
    {
        // Найти все объекты с Damageable в радиусе
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<Damageable>(out Damageable target) && !target.IsDead)
            {
                // Публикуем событие для каждой обнаруженной цели
                // UI может отрисовать маркер над ними
                EventBroker.Publish(new EntityDetectedEvent
                {
                    EntityTransform = hit.transform,
                    Duration = highlightDuration
                });
                Debug.Log("Слышу: " + hit.name + " на расстоянии " +
                    Vector3.Distance(transform.position, hit.transform.position).ToString("F1") + "м");
            }
        }

        yield return null;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
}
