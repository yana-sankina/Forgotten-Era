using UnityEngine;
using System.Collections;

public class PlayerNeeds : MonoBehaviour
{
    [Header("Налаштування")]
    public float maxHunger = 100f;
    [SerializeField] private float hungerDecayRate = 1f;
    public float maxThirst = 100f;
    [SerializeField] private float thirstDecayRate = 1.5f;
    [SerializeField] private float decayInterval = 10f;
    [SerializeField] private int starvationDamage = 5;
    public float CurrentThirst { get; private set; }
    
    public float CurrentHunger { get; private set; }


    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerNeedsCapacityUpdatedEvent>(OnCapacityUpdated);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerNeedsCapacityUpdatedEvent>(OnCapacityUpdated);
    }

    /// <summary>
    /// Когда динозавр растёт, его желудок и потребность в воде увеличиваются.
    /// Текущие значения масштабируются пропорционально.
    /// </summary>
    private void OnCapacityUpdated(PlayerNeedsCapacityUpdatedEvent e)
    {
        // Пропорциональное масштабирование: если было 50/100 (50%), станет 75/150 (50%)
        if (maxHunger > 0)
            CurrentHunger = (CurrentHunger / maxHunger) * e.NewMaxHunger;
        if (maxThirst > 0)
            CurrentThirst = (CurrentThirst / maxThirst) * e.NewMaxThirst;

        maxHunger = e.NewMaxHunger;
        maxThirst = e.NewMaxThirst;
        hungerDecayRate = e.HungerDecayRate;
        thirstDecayRate = e.ThirstDecayRate;

        PublishHungerEvent();
        PublishThirstEvent();
    }
    
    void Start()
    {
        // Начальные значения будут выставлены через OnCapacityUpdated от PlayerGrowth
        // но на случай если событие придёт позже — ставим дефолт
        CurrentHunger = maxHunger;
        CurrentThirst = maxThirst;
        
        PublishHungerEvent();
        PublishThirstEvent();
        
        StartCoroutine(NeedsDecayLoop());
    }

    private IEnumerator NeedsDecayLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decayInterval);

            CurrentHunger = Mathf.Clamp(CurrentHunger - hungerDecayRate, 0, maxHunger);
            CurrentThirst = Mathf.Clamp(CurrentThirst - thirstDecayRate, 0, maxHunger);
            
            bool isStarving = CurrentHunger <= 0;
            bool isDehydrated = CurrentThirst <= 0;
            
            if (isStarving)
            {
                Debug.LogWarning("Игрок умирает от голода!");
                EventBroker.Publish(new EnvironmentDamageEvent { DamageAmount = starvationDamage });
            }
            if (isDehydrated)
            {
                Debug.LogWarning("Игрок умирает от жажды!");
                EventBroker.Publish(new EnvironmentDamageEvent { DamageAmount = starvationDamage });
            }

            PublishHungerEvent();
            PublishThirstEvent();
        }
    }

    public void Eat(float amount)
    {
        CurrentHunger += amount;
        CurrentHunger = Mathf.Clamp(CurrentHunger, 0, maxHunger);
        PublishHungerEvent();
        Debug.Log("Поел. Голод теперь: " + CurrentHunger);
    }

    public void Drink(float amount)
    {
        CurrentThirst += amount;
        CurrentThirst = Mathf.Clamp(CurrentThirst, 0, maxThirst);
        PublishThirstEvent();
        Debug.Log("Попил. Жажда теперь: " + CurrentThirst);
    }
    
    private void PublishHungerEvent()
    {
        EventBroker.Publish(new PlayerHungerChangedEvent 
        { 
            Current = CurrentHunger, 
            Max = maxHunger 
        });
    }

    private void PublishThirstEvent()
    {
        EventBroker.Publish(new PlayerThirstChangedEvent 
        { 
            Current = CurrentThirst, 
            Max = maxThirst 
        });
    }
}
