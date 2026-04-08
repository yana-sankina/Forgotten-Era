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
        TryDealDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Фоллбек: если коллайдер уже пересекался в момент включения хитбокса,
        // OnTriggerEnter может не прийти в этот же кадр.
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider other)
    {
        if (hitTargets.Contains(other))
        {
            return;
        }

        PlayerHealth player = null;
        if (!other.TryGetComponent<PlayerHealth>(out player))
        {
            // Часто коллайдер висит на дочернем объекте игрока.
            player = other.GetComponentInParent<PlayerHealth>();
        }

        if (player != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Debug.LogWarning("Игрок получил " + attackDamage + " урона!");
            player.TakeDamage(attackDamage);

            EventBroker.Publish(new PlayerDamagedEvent
            {
                Player = player,
                DamageAmount = attackDamage,
                HitPoint = hitPoint
            });

            hitTargets.Add(other);
        }
    }
}
