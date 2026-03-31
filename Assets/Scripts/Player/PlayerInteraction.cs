using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Радиус обнаружения")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float scanInterval = 0.25f; // сканировать 4 раза в секунду

    private PlayerInput playerInput;
    private PlayerNeeds playerNeeds;
    private List<Consumable> nearbyItems = new List<Consumable>();

    private Coroutine drinkingCoroutine = null;
    private Consumable currentWaterSource = null;
    private float scanTimer = 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerNeeds = GetComponent<PlayerNeeds>();
    }

    private void Update()
    {
        // Периодически сканируем область вокруг на предметы
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanNearby();
        }

        if (playerInput.InteractInput)
        {
            TryEat();
        }

        if (playerInput.InteractHeldInput)
        {
            TryStartDrinking();
        }
        else
        {
            TryStopDrinking();
        }
    }

    /// <summary>
    /// Ищем все Consumable в радиусе без триггеров — через OverlapSphere.
    /// </summary>
    private void ScanNearby()
    {
        nearbyItems.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var col in hits)
        {
            // Проверяем на самом объекте
            Consumable item = col.GetComponent<Consumable>();
            // Если нет — на родителе
            if (item == null)
                item = col.GetComponentInParent<Consumable>();

            if (item != null && !nearbyItems.Contains(item))
            {
                nearbyItems.Add(item);
            }
        }
    }

    private void TryEat()
    {
        Consumable closestItem = FindClosestItem();
        if (closestItem == null || closestItem.type != Consumable.ConsumableType.Food)
        {
            return;
        }

        if (playerNeeds.CurrentHunger >= playerNeeds.maxHunger)
        {
            Debug.Log("Я не голоден!");
            return;
        }

        playerNeeds.Eat(closestItem.restoreAmountPerBite);
        closestItem.usesLeft--;
        Debug.Log("Укусил. Осталось укусов: " + closestItem.usesLeft);

        if (closestItem.usesLeft <= 0)
        {
            nearbyItems.Remove(closestItem);
            Destroy(closestItem.gameObject);
        }
    }

    private void TryStartDrinking()
    {
        if (drinkingCoroutine != null) return;

        Consumable waterSource = FindClosestItem();
        if (waterSource == null || waterSource.type != Consumable.ConsumableType.Water)
        {
            return;
        }

        if (playerNeeds.CurrentThirst >= playerNeeds.maxThirst)
        {
            Debug.Log("Я не хочу пить!");
            return;
        }

        currentWaterSource = waterSource;
        drinkingCoroutine = StartCoroutine(DrinkingLoop(waterSource));
    }

    private void TryStopDrinking()
    {
        if (drinkingCoroutine != null)
        {
            StopCoroutine(drinkingCoroutine);
            drinkingCoroutine = null;
            currentWaterSource = null;
        }
    }

    private IEnumerator DrinkingLoop(Consumable source)
    {
        while (true)
        {
            playerNeeds.Drink(source.restoreAmountPerTick);

            if (playerNeeds.CurrentThirst >= playerNeeds.maxThirst)
            {
                Debug.Log("Напился!");
                TryStopDrinking();
                yield break;
            }

            yield return new WaitForSeconds(source.restoreInterval);
        }
    }

    private Consumable FindClosestItem()
    {
        if (nearbyItems.Count == 0) return null;

        return nearbyItems
            .OrderBy(item => Vector3.Distance(transform.position, item.transform.position))
            .FirstOrDefault();
    }
}