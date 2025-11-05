using UnityEngine;
using System.Collections;
public class PlayerAttack : MonoBehaviour
{
    [Header("Настройки Атаки")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackDuration = 0.3f;

    [Header("Ссылки")]
    [SerializeField] private Hitbox attackHitbox; 

    private bool isAttacking = false;

    private void Awake()
    {
        if(attackHitbox)
            attackHitbox.gameObject.SetActive(false);
    }
    
    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerAttackEvent>(OnAttack);
    }
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerAttackEvent>(OnAttack);
    }
    
    private void OnAttack(PlayerAttackEvent e)
    {
        if (!isAttacking)
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
    }
}