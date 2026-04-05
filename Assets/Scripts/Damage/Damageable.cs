using UnityEngine;
using System.Collections;
using System;

public class Damageable : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int xpReward = 25;

    [Header("Авто-респаун (для тестов)")]
    [SerializeField] private bool autoRespawn = false;
    [SerializeField] private float respawnDelay = 3f;

    private int currentHP;

    public bool IsDead { get; private set; } = false;
    public bool IsCorpse { get; private set; } = false;
    public bool IsStunned { get; private set; } = false;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;

    public event Action<Damageable> Died;

    private Coroutine bleedCoroutine;
    private Coroutine stunCoroutine;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || IsCorpse) return;
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
        if (IsDead || IsCorpse) return;
        if (bleedCoroutine != null) StopCoroutine(bleedCoroutine);
        bleedCoroutine = StartCoroutine(BleedCoroutine(damagePerTick, duration));
    }

    /// <summary>
    /// Стан: цель не может двигаться/атаковать на duration секунд.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (IsDead || IsCorpse) return;
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    /// <summary>
    /// Позволяет спавнеру задать параметры врага в рантайме (до начала боя).
    /// </summary>
    public void SetRuntimeStats(int newMaxHP, int newXpReward)
    {
        maxHP = Mathf.Max(1, newMaxHP);
        xpReward = Mathf.Max(0, newXpReward);

        // При настройке нового врага считаем что он живой и не труп.
        IsDead = false;
        IsCorpse = false;
        IsStunned = false;
        currentHP = maxHP;
    }

    /// <summary>
    /// Переводит цель в состояние трупа: не принимает урон, не станится, не истекает кровью.
    /// </summary>
    public void MarkAsCorpse()
    {
        IsCorpse = true;
        IsStunned = false;

        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
            bleedCoroutine = null;
        }

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
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

        Died?.Invoke(this);
        EventBroker.Publish(new EnemyKilledEvent { XPReward = xpReward });
        Debug.Log(gameObject.name + " убит! XP: " + xpReward);

        if (autoRespawn)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHP = maxHP;
        IsDead = false;
        IsCorpse = false;
        IsStunned = false;
        bleedCoroutine = null;
        stunCoroutine = null;

        Debug.Log(gameObject.name + " воскрес!");
    }
}
