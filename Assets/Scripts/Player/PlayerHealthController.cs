using UnityEngine;
using System.Collections;

public class PlayerHealthController : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int healAmount = 3;
    [SerializeField] private float healDelay = 20f;

    [Header("Controls / Keys")]
    public KeyCode damageKey = KeyCode.I;
    public KeyCode healKey = KeyCode.K;
    public KeyCode respawnKey = KeyCode.R;
    public Vector3 respawnPosition = Vector3.zero;

    private Coroutine healCoroutine;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("`playerHealth` не назначен в инспекторе и не найден на объекте. Назначьте компонент PlayerHealth или перетащите ссылку в инспектор.", this);
            }
        }
    }

    void Update()
    {
        if (playerHealth == null) return;

        if (Input.GetKeyDown(damageKey))
            playerHealth.TakeDamage(20);

        if (Input.GetKeyDown(healKey))
            playerHealth.Heal(20);

        if (Input.GetKeyDown(respawnKey))
            playerHealth.Respawn(respawnPosition);
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBroker.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBroker.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
            healCoroutine = null;
        }
    }

    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (healCoroutine != null) StopCoroutine(healCoroutine);
        healCoroutine = StartCoroutine(HealAfterDelay());
    }

    private IEnumerator HealAfterDelay()
    {
        yield return new WaitForSeconds(healDelay);
        while (playerHealth != null && playerHealth.currentHP < playerHealth.maxHP)
        {
            playerHealth.Heal(healAmount);
            yield return new WaitForSeconds(healDelay);
        }
        healCoroutine = null;
    }
}
