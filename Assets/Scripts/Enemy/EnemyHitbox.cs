using UnityEngine;
using System.Collections.Generic;

public class EnemyHitbox : MonoBehaviour
{
    [Header("Налаштування")]
    public int attackDamage = 5;
    
    private List<Collider> hitTargets; 
    
    void OnEnable()
    {
        if (hitTargets == null)
        {
            hitTargets = new List<Collider>();
        }
        hitTargets.Clear();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (hitTargets.Contains(other))
        {
            return;
        }
        
        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth player))
        {
            Debug.LogWarning("Игрок получил " + attackDamage + " урона!");
            player.TakeDamage(attackDamage);
            
            hitTargets.Add(other);
        }
    }
}
