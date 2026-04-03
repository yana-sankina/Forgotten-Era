using UnityEngine;
using System.Collections.Generic;

public class Hitbox : MonoBehaviour
{
    private int attackDamage = 0; 
    
    // Эффекты способностей — если заданы, применяются при ударе
    private bool applyBleed = false;
    private int bleedDamagePerTick;
    private float bleedDuration;

    private bool applyStun = false;
    private float stunDuration;

    private List<Collider> hitTargets; 
    
    /// <summary>
    /// Обычная атака — только урон.
    /// </summary>
    public void Activate(int damage)
    {
        attackDamage = damage;
        applyBleed = false;
        applyStun = false;
        if (hitTargets == null)
        {
            hitTargets = new List<Collider>();
        }
        hitTargets.Clear();
    }

    /// <summary>
    /// Атака с кровотечением (Дакотараптор — серповидный удар).
    /// </summary>
    public void ActivateWithBleed(int damage, int bleedDmgPerTick, float bleedDur)
    {
        Activate(damage);
        applyBleed = true;
        bleedDamagePerTick = bleedDmgPerTick;
        bleedDuration = bleedDur;
    }

    /// <summary>
    /// Атака со станом (Тирекс — сокрушительный удар).
    /// </summary>
    public void ActivateWithStun(int damage, float stunDur)
    {
        Activate(damage);
        applyStun = true;
        stunDuration = stunDur;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (attackDamage > 0 && !hitTargets.Contains(other))
        {
            Damageable target = null;

            // Сначала пытаемся найти Damageable на самом collider-объекте.
            if (!other.TryGetComponent<Damageable>(out target))
            {
                // Если collider дочерний — ищем на родителе (частый случай у моделей врагов).
                target = other.GetComponentInParent<Damageable>();
            }

            if (target != null)
            {
                target.TakeDamage(attackDamage);

                if (applyBleed)
                    target.ApplyBleed(bleedDamagePerTick, bleedDuration);

                if (applyStun)
                    target.ApplyStun(stunDuration);
                
                hitTargets.Add(other);
            }
        }
    }
}
