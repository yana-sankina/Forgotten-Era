using UnityEngine;
using System.Collections;

public class PlayerGrowth : MonoBehaviour
{
    [Header("Налаштування Роста")]
    [SerializeField] private float maxGrowth = 100f;
    [SerializeField] private float growthPerTick = 0.5f;
    [SerializeField] private float growthInterval = 20f;
    
    [Header("Налаштування Масштаба")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.0f;
    
    [Header("Налаштування Характеристик")]
    [SerializeField] private int minHealth = 80;
    [SerializeField] private int maxHealth = 250;
    [SerializeField] private int minDamage = 10;
    [SerializeField] private int maxDamage = 50;
    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 6f;

    private float currentGrowth = 0f;

    void Start()
    {
        currentGrowth = 0f;
        
        UpdateScaleAndStats();
        PublishGrowthEvent();
        
        StartCoroutine(GrowthLoop());
    }

    private IEnumerator GrowthLoop()
    {
        while (currentGrowth < maxGrowth)
        {
            yield return new WaitForSeconds(growthInterval);
            
            currentGrowth += growthPerTick;
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
        float growthPercent = currentGrowth / maxGrowth;
        
        float newScale = Mathf.Lerp(minScale, maxScale, growthPercent);
        
        transform.localScale = new Vector3(newScale, newScale, newScale);
        
        int newHealth = (int)Mathf.Lerp(minHealth, maxHealth, growthPercent);
        int newDamage = (int)Mathf.Lerp(minDamage, maxDamage, growthPercent);
        float newSpeed = Mathf.Lerp(minSpeed, maxSpeed, growthPercent);
        
        EventBroker.Publish(new PlayerStatsUpdatedEvent
        {
            NewMaxHP = newHealth,
            NewAttackDamage = newDamage,
            NewMoveSpeed = newSpeed
        });
    }
    
    private void PublishGrowthEvent()
    {
        EventBroker.Publish(new PlayerGrowthChangedEvent
        {
            CurrentGrowth = this.currentGrowth,
            MaxGrowth = this.maxGrowth
        });
    }
}