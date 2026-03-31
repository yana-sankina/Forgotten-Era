using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Спавнер ресурсов (еда). Ставится на пустой объект на сцене.
/// Спавнит Consumable-префабы в радиусе с респавном.
/// </summary>
public class ResourceSpawner : MonoBehaviour
{
    [Header("Что спавнить")]
    [Tooltip("Префаб еды (должен иметь Consumable + Collider)")]
    [SerializeField] private GameObject consumablePrefab;

    [Header("Настройки")]
    [Tooltip("Максимум предметов одновременно")]
    [SerializeField] private int maxItems = 3;
    [Tooltip("Радиус разброса от точки спавна")]
    [SerializeField] private float spawnRadius = 5f;
    [Tooltip("Время респавна после съедения (сек)")]
    [SerializeField] private float respawnTime = 30f;
    [Tooltip("Спавнить при старте сцены")]
    [SerializeField] private bool spawnOnStart = true;

    private List<GameObject> spawnedItems = new List<GameObject>();

    private void Start()
    {
        if (spawnOnStart && consumablePrefab != null)
        {
            for (int i = 0; i < maxItems; i++)
            {
                SpawnItem();
            }
        }

        StartCoroutine(RespawnLoop());
    }

    private IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnTime);

            // Убираем null (съеденные предметы)
            spawnedItems.RemoveAll(item => item == null);

            // Доспавниваем до максимума
            while (spawnedItems.Count < maxItems)
            {
                SpawnItem();
            }
        }
    }

    private void SpawnItem()
    {
        if (consumablePrefab == null) return;

        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0; // На уровне земли

        Vector3 spawnPos = transform.position + randomOffset;

        // Пытаемся поставить на поверхность (Raycast вниз)
        if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
        {
            spawnPos = hit.point + Vector3.up * 0.1f; // Чуть выше земли
        }

        GameObject item = Instantiate(consumablePrefab, spawnPos, Quaternion.identity, transform);
        spawnedItems.Add(item);
    }

    // Показываем радиус в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
