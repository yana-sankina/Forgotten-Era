using UnityEngine;
using System.Collections;

public class DummyAttackSpam : MonoBehaviour
{
    [Header("Посилання")]
    [SerializeField] private GameObject attackHitboxObject; 

    [Header("Таймінги")]
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float attackCooldown = 1.0f;

    private Damageable damageable;
    
    void Awake()
    {
        damageable = GetComponent<Damageable>(); 
    }
    void Start()
    {
        if (attackHitboxObject == null)
        {
            Debug.LogError("Хитбокс не назначен!", this);
            return;
        }
        
        attackHitboxObject.SetActive(false);
        StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        while (damageable != null && !damageable.IsDead) 
        {
            attackHitboxObject.SetActive(true);
            yield return new WaitForSeconds(attackDuration);
            
            attackHitboxObject.SetActive(false);
            yield return new WaitForSeconds(attackCooldown);
        }
        
        Debug.Log("Атака дамми остановлена (цель мертва).");
        
        attackHitboxObject.SetActive(false);
    }
}
