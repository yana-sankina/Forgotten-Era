using UnityEngine;
using System.Collections;

public class Damageable : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int xpReward = 25;
    private int currentHP;

    public bool IsDead { get; private set; } = false;
    public bool IsStunned { get; private set; } = false;

    private Coroutine bleedCoroutine;
    private Coroutine stunCoroutine;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHP -= amount;
        Debug.Log(gameObject.name + " получил " + amount + " урона. Осталось ХП: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Кровотечение: урон каждую секунду на протяжении duration секунд.
    /// Повторный вызов сбрасывает таймер.
    /// </summary>
    public void ApplyBleed(int damagePerTick, float duration)
    {
        if (IsDead) return;
        if (bleedCoroutine != null) StopCoroutine(bleedCoroutine);
        bleedCoroutine = StartCoroutine(BleedCoroutine(damagePerTick, duration));
    }

    /// <summary>
    /// Стан: цель не может двигаться/атаковать на duration секунд.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (IsDead) return;
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator BleedCoroutine(int damagePerTick, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !IsDead)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            TakeDamage(damagePerTick);
            Debug.Log(gameObject.name + " истекает кровью: " + damagePerTick + " урона");
        }
        bleedCoroutine = null;
    }

    private IEnumerator StunCoroutine(float duration)
    {
        IsStunned = true;
        Debug.Log(gameObject.name + " оглушён на " + duration + " сек!");
        yield return new WaitForSeconds(duration);
        IsStunned = false;
        stunCoroutine = null;
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Публикуем событие смерти с наградой XP (для системы опыта)
        EventBroker.Publish(new EnemyKilledEvent { XPReward = xpReward });
        Debug.Log(gameObject.name + " убит! XP: " + xpReward);
    }
}