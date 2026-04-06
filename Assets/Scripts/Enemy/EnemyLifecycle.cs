using UnityEngine;
using System.Collections;

/// <summary>
/// Жизненный цикл врага: смерть -> труп (еда) -> удаление по таймеру.
/// Работает на том же объекте врага, без отдельного префаба трупа.
/// </summary>
[RequireComponent(typeof(Damageable))]
public class EnemyLifecycle : MonoBehaviour
{
    [Header("Труп по умолчанию")]
    [SerializeField] private float defaultCorpseLifetimeSeconds = 600f; // ~10 минут
    [SerializeField] private int defaultCorpseUses = 5;
    [SerializeField] private float defaultFoodPerBite = 25f;

    [Header("Поза трупа")]
    [SerializeField] private Vector3 corpseRotationEuler = new Vector3(0f, 0f, 90f);

    [Header("Дополнительно отключить при смерти")]
    [SerializeField] private MonoBehaviour[] additionalBehavioursToDisable;

    private Damageable damageable;
    private Rigidbody rb;

    private bool isCorpse;
    private bool corpseSpawnedEventPublished;
    private bool corpseRemovedEventPublished;

    private float configuredCorpseLifetime = -1f;
    private int configuredCorpseUses = -1;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (damageable != null)
            damageable.Died += OnDamageableDied;
    }

    private void OnDisable()
    {
        if (damageable != null)
            damageable.Died -= OnDamageableDied;
    }

    private void OnDestroy()
    {
        PublishCorpseRemovedIfNeeded();
    }

    /// <summary>
    /// Вызывается спавнером/инициализатором врага для настройки трупа по типу/размеру.
    /// </summary>
    public void ConfigureCorpse(float lifetimeSeconds, int corpseUses)
    {
        configuredCorpseLifetime = Mathf.Max(1f, lifetimeSeconds);
        configuredCorpseUses = Mathf.Max(1, corpseUses);
    }

    private void OnDamageableDied(Damageable dead)
    {
        if (dead != damageable || isCorpse)
            return;

        ConvertToCorpse();
    }

    private void ConvertToCorpse()
    {
        isCorpse = true;
        damageable.MarkAsCorpse();

        DisableCombatBehaviours();
        ApplyCorpsePose();
        SetupConsumable();

        EventBroker.Publish(new EnemyCorpseSpawnedEvent
        {
            InstanceId = gameObject.GetInstanceID(),
            CorpseObject = gameObject
        });
        corpseSpawnedEventPublished = true;

        float lifetime = (configuredCorpseLifetime > 0f) ? configuredCorpseLifetime : defaultCorpseLifetimeSeconds;
        StartCoroutine(RemoveCorpseAfterDelay(lifetime));
    }

    private void DisableCombatBehaviours()
    {

        EnemyHitbox[] hitboxes = GetComponentsInChildren<EnemyHitbox>(true);
        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i].enabled = false;
        }

        if (additionalBehavioursToDisable != null)
        {
            for (int i = 0; i < additionalBehavioursToDisable.Length; i++)
            {
                if (additionalBehavioursToDisable[i] != null)
                    additionalBehavioursToDisable[i].enabled = false;
            }
        }

        // Если есть физика — останавливаем тело чтобы труп не продолжал двигаться.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void ApplyCorpsePose()
    {
        transform.rotation = transform.rotation * Quaternion.Euler(corpseRotationEuler);
    }

    private void SetupConsumable()
    {
        Consumable consumable = GetComponent<Consumable>();
        if (consumable == null)
            consumable = gameObject.AddComponent<Consumable>();

        consumable.type = Consumable.ConsumableType.Food;
        consumable.restoreAmountPerBite = defaultFoodPerBite;

        int uses = (configuredCorpseUses > 0) ? configuredCorpseUses : defaultCorpseUses;
        consumable.usesLeft = Mathf.Max(1, uses);
    }

    private IEnumerator RemoveCorpseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (this == null || !isCorpse)
            yield break;

        PublishCorpseRemovedIfNeeded();
        Destroy(gameObject);
    }

    private void PublishCorpseRemovedIfNeeded()
    {
        if (!isCorpse || !corpseSpawnedEventPublished || corpseRemovedEventPublished)
            return;

        EventBroker.Publish(new EnemyCorpseRemovedEvent
        {
            InstanceId = gameObject.GetInstanceID()
        });
        corpseRemovedEventPublished = true;
    }
}
