using UnityEngine;

/// <summary>
/// Конфиг одного типа врага.
/// Ассет создаётся пользователем в Unity, скрипт только описывает структуру данных.
/// </summary>
[CreateAssetMenu(fileName = "EnemyRuntimeProfile", menuName = "Game/Enemy Runtime Profile")]
public class EnemyRuntimeProfile : ScriptableObject
{
    [Header("Идентификация")]
    public string enemyTypeName = "Enemy";

    [Header("Визуал")]
    [Tooltip("Модель, которая будет натянута на врага в рантайме")]
    public GameObject modelPrefab;
    [Tooltip("Быстрый разворот модели на 180° по Y")]
    public bool flipForward = false;
    [Tooltip("Локальный поворот модели (если импортирована не в ту сторону)")]
    public Vector3 modelLocalEulerOffset = Vector3.zero;

    [Header("Масштаб модели")]
    [Min(0.1f)] public float minScale = 0.8f;
    [Min(0.1f)] public float maxScale = 1.3f;

    [Header("Статы (подбираются в рантайме с учётом масштаба)")]
    [Min(1)] public int minHP = 60;
    [Min(1)] public int maxHP = 120;
    [Min(1)] public int minAttack = 8;
    [Min(1)] public int maxAttack = 16;
    [Min(0)] public int minXPReward = 15;
    [Min(0)] public int maxXPReward = 35;

    [Header("Труп как еда")]
    [Min(1)] public int minCorpseUses = 3;
    [Min(1)] public int maxCorpseUses = 6;
    [Min(1f)] public float corpseLifetimeSeconds = 600f; // ~10 минут

    [Header("AI (NavMesh + FSM)")]
    [Min(0f)] public float patrolRadius = 12f;
    [Min(0.1f)] public float patrolPointTimeout = 6f;

    [Min(0f)] public float detectionRadius = 20f;
    [Min(0f)] public float loseTargetRadius = 28f;
    [Min(0f)] public float loseTargetDelaySeconds = 2f;

    [Min(0f)] public float patrolSpeed = 3.5f;
    [Min(0f)] public float chaseSpeed = 5.5f;
    [Min(0f)] public float fleeSpeed = 6.5f;

    [Min(0f)] public float attackRange = 2.2f;
    [Tooltip("Чтобы бот не дёргался на границе attackRange (гистерезис)")]
    [Min(0f)] public float attackRangeHysteresis = 0.6f;
    [Min(0.01f)] public float attackDuration = 0.25f;
    [Min(0.01f)] public float attackCooldown = 1.0f;
    [Min(0f)] public float attackTurnSpeed = 8f;
    [Tooltip("Пауза после удара, в течение которой враг доворачивается медленнее.")]
    [Min(0f)] public float postAttackTurnDelay = 0.35f;
    [Tooltip("Ускорение набора скорости поворота в атаке.")]
    [Min(0f)] public float attackTurnAcceleration = 18f;
    [Tooltip("Базовый множитель скорости поворота в атаке.")]
    [Min(0f)] public float attackTurnSpeedMultiplier = 0.35f;
    [Tooltip("Множитель поворота во время recovery-паузы после удара.")]
    [Range(0f, 1f)] public float recoveryTurnSpeedMultiplier = 0.25f;
    [Tooltip("Минимальная длительность активного окна hitbox (сек).")]
    [Min(0.01f)] public float minAttackWindowDuration = 0.4f;
    [Tooltip("Минимальное время в каждом цикле атаки, когда враг может довернуться.")]
    [Min(0f)] public float minFreeTurnTimeBeforeNextSwing = 0.25f;

    [Range(0f, 1f)] public float fleeHpThreshold = 0.5f;
    [Min(0.1f)] public float fleeDuration = 4f;
    [Range(0f, 1f)] public float reengageHpThreshold = 0.65f;
    [Min(0f)] public float fleeDistance = 12f;
    [Min(0.1f)] public float fleePickRadius = 6f;

    [Header("AI Debug")]
    [Tooltip("Включить диагностические логи FSM/ударов для этого типа врага.")]
    public bool enableDebugLogs = false;

    public float GetRandomScale()
    {
        return Random.Range(Mathf.Min(minScale, maxScale), Mathf.Max(minScale, maxScale));
    }

    public int GetRandomHP(float normalizedScale)
    {
        return GetScaledInt(minHP, maxHP, normalizedScale);
    }

    public int GetRandomAttack(float normalizedScale)
    {
        return GetScaledInt(minAttack, maxAttack, normalizedScale);
    }

    public int GetRandomXPReward(float normalizedScale)
    {
        return GetScaledInt(minXPReward, maxXPReward, normalizedScale);
    }

    public int GetRandomCorpseUses(float normalizedScale)
    {
        return GetScaledInt(minCorpseUses, maxCorpseUses, normalizedScale);
    }

    public float GetNormalizedScale(float scale)
    {
        float lo = Mathf.Min(minScale, maxScale);
        float hi = Mathf.Max(minScale, maxScale);
        if (Mathf.Approximately(lo, hi))
            return 1f;

        return Mathf.InverseLerp(lo, hi, scale);
    }

    private int GetScaledInt(int minValue, int maxValue, float normalizedScale)
    {
        int lo = Mathf.Min(minValue, maxValue);
        int hi = Mathf.Max(minValue, maxValue);
        if (lo == hi) return lo;

        float t = Mathf.Clamp01(normalizedScale);
        int center = Mathf.RoundToInt(Mathf.Lerp(lo, hi, t));

        // Небольшой разброс вокруг базового значения для вариативности.
        int spread = Mathf.Max(1, Mathf.RoundToInt((hi - lo) * 0.15f));
        int lowRoll = Mathf.Clamp(center - spread, lo, hi);
        int highRoll = Mathf.Clamp(center + spread, lo, hi);
        return Random.Range(lowRoll, highRoll + 1);
    }
}
