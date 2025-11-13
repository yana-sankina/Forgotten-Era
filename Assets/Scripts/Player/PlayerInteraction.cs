using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerNeeds playerNeeds;
    private List<Consumable> nearbyItems = new List<Consumable>();

    private Coroutine drinkingCoroutine = null;
    private Consumable currentWaterSource = null;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerNeeds = GetComponent<PlayerNeeds>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Consumable>(out Consumable item))
        {
            if (!nearbyItems.Contains(item))
            {
                nearbyItems.Add(item);
                Debug.Log("Вижу предмет: " + item.name);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Consumable>(out Consumable item))
        {
            if (nearbyItems.Contains(item))
            {
                nearbyItems.Remove(item);
                Debug.Log("Предмет ушел из зоны: " + item.name);
            }
        }
    }


    private void Update()
    {
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
            Debug.Log("Перестал пить.");
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