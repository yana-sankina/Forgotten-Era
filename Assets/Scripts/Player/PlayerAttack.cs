using UnityEngine;
using System.Collections;
public class PlayerAttack : MonoBehaviour
{
    
    private int attackDamage;
    
    [Header("Налаштування Атаки")]
    [SerializeField] private float attackDuration = 0.3f;
    [Tooltip("Кулдаун после атаки (подбери под длину анимации)")]
    [SerializeField] private float attackCooldown = 0.8f;

    [Header("Посилання")]
    [SerializeField] private Hitbox attackHitbox; 

    private bool isAttacking = false;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        if(attackHitbox)
            attackHitbox.gameObject.SetActive(false);
    }
    
    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
        EventBroker.Subscribe<PlayerAttackEvent>(OnAttack);
    }
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
        EventBroker.Unsubscribe<PlayerAttackEvent>(OnAttack);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }
    
    private void OnStatsUpdated(PlayerStatsUpdatedEvent e)
    {
        attackDamage = e.NewAttackDamage;
    }
    private void OnAttack(PlayerAttackEvent e)
    {
        if (!isAttacking && cooldownTimer <= 0f)
        {
            PerformAttack();
        }
    }
    private void PerformAttack()
    {
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true; 
        
        attackHitbox.Activate(attackDamage);
        
        attackHitbox.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(attackDuration);
        
        attackHitbox.gameObject.SetActive(false);
        
        isAttacking = false;
        cooldownTimer = attackCooldown;
    }
}