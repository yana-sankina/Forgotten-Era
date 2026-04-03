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
