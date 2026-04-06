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

    private HashSet<Damageable> hitTargets;
    
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
            hitTargets = new HashSet<Damageable>();
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
    
    private void OnEnable()
    {
        if (hitTargets == null)
            hitTargets = new HashSet<Damageable>();
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Фоллбек: если хитбокс включился уже внутри цели, Enter может не сработать в этот кадр.
        TryApplyHit(other);
    }

    private void TryApplyHit(Collider other)
    {
        if (attackDamage <= 0 || other == null)
            return;

        Damageable target = null;
        if (!other.TryGetComponent<Damageable>(out target))
            target = other.GetComponentInParent<Damageable>();

        if (target == null)
            return;

        // Защита от авто-урона по себе, если на игроке когда-нибудь появится Damageable.
        if (target.transform.root == transform.root)
            return;

        if (hitTargets.Contains(target))
            return;

        target.TakeDamage(attackDamage);

        if (applyBleed)
            target.ApplyBleed(bleedDamagePerTick, bleedDuration);

        if (applyStun)
            target.ApplyStun(stunDuration);

        hitTargets.Add(target);
    }
}
