using UnityEngine;

/// <summary>
/// ScriptableObject — «паспорт» вида динозавра.
/// Создайте 3 ассета: ПКМ → Create → Dinosaur → Species Data
/// Заполните поля для T-Rex, Velociraptor и Troodon.
/// </summary>
[CreateAssetMenu(fileName = "NewDinosaurSpecies", menuName = "Dinosaur/Species Data")]
public class DinosaurSpeciesData : ScriptableObject
{
    [Header("Общая информация")]
    public string speciesName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Базовые характеристики (при 0% роста)")]
    public int baseMaxHP = 80;
    public int baseAttackDamage = 10;
    public float baseMoveSpeed = 4f;

    [Header("Характеристики при максимальном росте (100%)")]
    public int grownMaxHP = 250;
    public int grownAttackDamage = 50;
    public float grownMoveSpeed = 6f;

    [Header("Потребности (голод и жажда)")]
    public float baseMaxHunger = 60f;
    public float grownMaxHunger = 150f;
    public float baseMaxThirst = 60f;
    public float grownMaxThirst = 150f;
    [Tooltip("Скорость падения голода за интервал. Тирекс голодает быстрее.")]
    public float hungerDecayRate = 1f;
    [Tooltip("Скорость падения жажды за интервал.")]
    public float thirstDecayRate = 1.5f;

    [Header("Масштаб модели")]
    public float minScale = 0.5f;
    public float maxScale = 1.0f;

    [Header("Рост")]
    [Tooltip("Максимальное значение шкалы роста")]
    public float maxGrowth = 100f;
    [Tooltip("Сколько роста прибавляется за тик")]
    public float growthPerTick = 0.5f;
    [Tooltip("Интервал между тиками роста (секунды)")]
    public float growthInterval = 20f;

    [Header("Стадии роста (пороги в %)")]
    [Tooltip("На этих процентах роста игрок получает очки прокачки")]
    public float[] growthStageThresholds = { 20f, 40f, 60f, 80f, 100f };
    [Tooltip("Гарантированная прибавка к выбранной характеристике на каждой стадии")]
    public int[] stageStatBonus = { 50, 80, 120, 180, 250 };

    [Header("Модель")]
    [Tooltip("Префаб 3D-модели динозавра (пока можно оставить пустым)")]
    public GameObject modelPrefab;

    /// <summary>
    /// Интерполирует характеристики по проценту роста (0..1).
    /// </summary>
    public int GetMaxHP(float growthPercent)
    {
        return (int)Mathf.Lerp(baseMaxHP, grownMaxHP, growthPercent);
    }

    public int GetAttackDamage(float growthPercent)
    {
        return (int)Mathf.Lerp(baseAttackDamage, grownAttackDamage, growthPercent);
    }

    public float GetMoveSpeed(float growthPercent)
    {
        return Mathf.Lerp(baseMoveSpeed, grownMoveSpeed, growthPercent);
    }

    public float GetScale(float growthPercent)
    {
        return Mathf.Lerp(minScale, maxScale, growthPercent);
    }

    public float GetMaxHunger(float growthPercent)
    {
        return Mathf.Lerp(baseMaxHunger, grownMaxHunger, growthPercent);
    }

    public float GetMaxThirst(float growthPercent)
    {
        return Mathf.Lerp(baseMaxThirst, grownMaxThirst, growthPercent);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Валидация в редакторе: предупреждает о некорректных значениях.
    /// </summary>
    private void OnValidate()
    {
        if (baseMaxHP > grownMaxHP)
            Debug.LogWarning($"[{speciesName}] baseMaxHP ({baseMaxHP}) > grownMaxHP ({grownMaxHP})!", this);
        if (baseAttackDamage > grownAttackDamage)
            Debug.LogWarning($"[{speciesName}] baseAttackDamage ({baseAttackDamage}) > grownAttackDamage ({grownAttackDamage})!", this);
        if (baseMoveSpeed > grownMoveSpeed)
            Debug.LogWarning($"[{speciesName}] baseMoveSpeed ({baseMoveSpeed}) > grownMoveSpeed ({grownMoveSpeed})!", this);
        if (minScale > maxScale)
            Debug.LogWarning($"[{speciesName}] minScale ({minScale}) > maxScale ({maxScale})!", this);
        if (baseMaxHunger > grownMaxHunger)
            Debug.LogWarning($"[{speciesName}] baseMaxHunger ({baseMaxHunger}) > grownMaxHunger ({grownMaxHunger})!", this);
        if (baseMaxThirst > grownMaxThirst)
            Debug.LogWarning($"[{speciesName}] baseMaxThirst ({baseMaxThirst}) > grownMaxThirst ({grownMaxThirst})!", this);
        if (growthStageThresholds.Length != stageStatBonus.Length)
            Debug.LogWarning($"[{speciesName}] growthStageThresholds и stageStatBonus должны быть одинаковой длины!", this);
    }
#endif
}
