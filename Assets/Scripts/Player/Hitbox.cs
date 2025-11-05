using UnityEngine;
using System.Collections.Generic;

public class Hitbox : MonoBehaviour
{
    private int attackDamage = 0; 
    
    private List<Collider> hitTargets; 
    
    public void Activate(int damage)
    {
        attackDamage = damage;
        if (hitTargets == null)
        {
            hitTargets = new List<Collider>();
        }
        hitTargets.Clear();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (attackDamage > 0 && !hitTargets.Contains(other))
        {
            if (other.TryGetComponent<Damageable>(out Damageable target))
            {
                target.TakeDamage(attackDamage);
                
                hitTargets.Add(other);
            }
        }
    }
}
