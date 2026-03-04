using UnityEngine;
using System.Collections;

public class PlayerGrowth : MonoBehaviour
{
    [Header("Данные вида")]
    [Tooltip("Задаётся автоматически через DinosaurInitializer")]
    [SerializeField] private DinosaurSpeciesData speciesData;

    private float currentGrowth = 0f;
    private bool isInitialized = false;

    /// <summary>
    /// Вызывается DinosaurInitializer при старте сцены.
    /// Задаёт SO вида и запускает рост.
    /// </summary>
    public void Initialize(DinosaurSpeciesData data)
    {
        speciesData = data;
        currentGrowth = 0f;
        isInitialized = true;

        UpdateScaleAndStats();
        PublishGrowthEvent();

        StartCoroutine(GrowthLoop());
    }

    void Start()
    {
        // Если Initialize() уже вызван — ничего не делаем
        if (isInitialized) return;

        // Фоллбэк: если SO задан в инспекторе (тестирование без меню)
        if (speciesData != null)
        {
            Initialize(speciesData);
        }
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

            if (currentGrowth >= maxGrowth)
            {
                Debug.LogWarning("Игрок ДОСТИГ МАКСИМАЛЬНОГО РОСТА!");
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

        // Боевые характеристики
        int newHealth = speciesData.GetMaxHP(growthPercent);
        int newDamage = speciesData.GetAttackDamage(growthPercent);
        float newSpeed = speciesData.GetMoveSpeed(growthPercent);

        EventBroker.Publish(new PlayerStatsUpdatedEvent
        {
            NewMaxHP = newHealth,
            NewAttackDamage = newDamage,
            NewMoveSpeed = newSpeed
        });

        // Потребности — желудок и жажда масштабируются с ростом
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