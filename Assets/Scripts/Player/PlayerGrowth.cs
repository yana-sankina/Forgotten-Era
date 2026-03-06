using UnityEngine;
using System.Collections;

public class PlayerGrowth : MonoBehaviour
{
    [Header("Данные вида")]
    [Tooltip("Задаётся автоматически через DinosaurInitializer")]
    [SerializeField] private DinosaurSpeciesData speciesData;

    private float currentGrowth = 0f;
    private bool isInitialized = false;
    private int currentStageIndex = -1;

    // Бонусы от очков прокачки (поверх роста)
    private int bonusHP = 0;
    private int bonusATK = 0;
    private float bonusSPD = 0f;

    /// <summary>
    /// Вызывается DinosaurInitializer при старте сцены.
    /// </summary>
    public void Initialize(DinosaurSpeciesData data)
    {
        speciesData = data;
        currentGrowth = 0f;
        currentStageIndex = -1;
        isInitialized = true;

        UpdateScaleAndStats();
        PublishGrowthEvent();

        StartCoroutine(GrowthLoop());
    }

    void Start()
    {
        if (isInitialized) return;

        if (speciesData != null)
        {
            Initialize(speciesData);
        }
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerBonusStatsUpdatedEvent>(OnBonusStatsUpdated);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerBonusStatsUpdatedEvent>(OnBonusStatsUpdated);
    }

    private void OnBonusStatsUpdated(PlayerBonusStatsUpdatedEvent e)
    {
        bonusHP = e.BonusHP;
        bonusATK = e.BonusATK;
        bonusSPD = e.BonusSPD;

        // Пересчитываем статы с новыми бонусами
        if (speciesData != null)
            UpdateScaleAndStats();
    }

    private IEnumerator GrowthLoop()
    {
        float maxGrowth = speciesData.maxGrowth;

        while (currentGrowth < maxGrowth)
        {
            yield return new WaitForSeconds(speciesData.growthInterval);

            currentGrowth += speciesData.growthPerTick;
            currentGrowth = Mathf.Clamp(currentGrowth, 0f, maxGrowth);

            UpdateScaleAndStats();
            PublishGrowthEvent();
            CheckStageThresholds();

            if (currentGrowth >= maxGrowth)
            {
                Debug.LogWarning("Игрок ДОСТИГ МАКСИМАЛЬНОГО РОСТА!");
                break;
            }
        }
    }

    private void CheckStageThresholds()
    {
        if (speciesData.growthStageThresholds == null) return;

        float growthPercent = (currentGrowth / speciesData.maxGrowth) * 100f;

        for (int i = currentStageIndex + 1; i < speciesData.growthStageThresholds.Length; i++)
        {
            if (growthPercent >= speciesData.growthStageThresholds[i])
            {
                currentStageIndex = i;

                int bonus = (i < speciesData.stageStatBonus.Length) ? speciesData.stageStatBonus[i] : 1;

                Debug.Log("Стадия роста " + (i + 1) + "! Рост: " +
                    growthPercent.ToString("F0") + "% → +" + bonus + " очков");

                EventBroker.Publish(new GrowthStageReachedEvent
                {
                    StageIndex = i + 1,
                    BonusPoints = bonus
                });
            }
            else
            {
                break;
            }
        }
    }

    private void UpdateScaleAndStats()
    {
        float growthPercent = currentGrowth / speciesData.maxGrowth;

        // Масштаб модели
        float newScale = speciesData.GetScale(growthPercent);
        transform.localScale = new Vector3(newScale, newScale, newScale);

        // Базовые характеристики от роста + бонусы от прокачки
        int newHealth = speciesData.GetMaxHP(growthPercent) + bonusHP;
        int newDamage = speciesData.GetAttackDamage(growthPercent) + bonusATK;
        float newSpeed = speciesData.GetMoveSpeed(growthPercent) + bonusSPD;

        EventBroker.Publish(new PlayerStatsUpdatedEvent
        {
            NewMaxHP = newHealth,
            NewAttackDamage = newDamage,
            NewMoveSpeed = newSpeed
        });

        // Потребности
        float newMaxHunger = speciesData.GetMaxHunger(growthPercent);
        float newMaxThirst = speciesData.GetMaxThirst(growthPercent);

        EventBroker.Publish(new PlayerNeedsCapacityUpdatedEvent
        {
            NewMaxHunger = newMaxHunger,
            NewMaxThirst = newMaxThirst,
            HungerDecayRate = speciesData.hungerDecayRate,
            ThirstDecayRate = speciesData.thirstDecayRate
        });
    }

    private void PublishGrowthEvent()
    {
        EventBroker.Publish(new PlayerGrowthChangedEvent
        {
            CurrentGrowth = this.currentGrowth,
            MaxGrowth = speciesData.maxGrowth
        });
    }
}