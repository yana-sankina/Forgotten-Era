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
    [Tooltip("Если включено, этот спавнер не будет спавнить, когда трупов на карте >= лимита")]
    [SerializeField] private bool limitByGlobalCorpseCount = false;
    [Tooltip("Глобальный лимит трупов на карте для этого спавнера")]
    [SerializeField] private int maxCorpsesOnMap = 10;
    [Tooltip("Минимальная дистанция между заспавненными предметами этого спавнера")]
    [SerializeField] private float minDistanceBetweenItems = 2.5f;
    [Tooltip("Сколько попыток поиска валидной точки спавна")]
    [SerializeField] private int spawnPointSearchAttempts = 16;
    [Tooltip("Высота старта луча вниз при поиске земли")]
    [SerializeField] private float groundProbeHeight = 50f;
    [Tooltip("Длина луча вниз при поиске земли")]
    [SerializeField] private float groundProbeDistance = 120f;
    [Tooltip("Смещение по Y после попадания в землю")]
    [SerializeField] private float groundOffset = 0.02f;
    [Tooltip("Слои, которые считаем землёй при Raycast fallback")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [Tooltip("Сначала использовать высоту Terrain, если точка внутри Terrain bounds")]
    [SerializeField] private bool preferTerrainHeight = true;
    [Tooltip("Минимальная «вертикальность» нормали поверхности для спавна")]
    [Range(0f, 1f)]
    [SerializeField] private float minGroundNormalY = 0.55f;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private int currentCorpseCount = 0;

    private void OnEnable()
    {
        EventBroker.Subscribe<EnemyCorpseSpawnedEvent>(OnEnemyCorpseSpawned);
        EventBroker.Subscribe<EnemyCorpseRemovedEvent>(OnEnemyCorpseRemoved);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<EnemyCorpseSpawnedEvent>(OnEnemyCorpseSpawned);
        EventBroker.Unsubscribe<EnemyCorpseRemovedEvent>(OnEnemyCorpseRemoved);
    }

    private void Start()
    {
        // Однократный снимок при старте, чтобы корректно работать если трупы уже есть на сцене.
        currentCorpseCount = CountInitialCorpses();

        if (spawnOnStart && consumablePrefab != null)
        {
            while (spawnedItems.Count < maxItems)
            {
                if (!SpawnItem())
                    break;
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
                if (!SpawnItem())
                    break;
            }
        }
    }

    private bool SpawnItem()
    {
        if (consumablePrefab == null) return false;
        if (limitByGlobalCorpseCount && currentCorpseCount >= Mathf.Max(1, maxCorpsesOnMap))
            return false;

        if (!TryFindSpawnPosition(out Vector3 spawnPos))
            return false;

        GameObject item = Instantiate(consumablePrefab, spawnPos, Quaternion.identity, transform);
        spawnedItems.Add(item);
        return true;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        int attempts = Mathf.Max(1, spawnPointSearchAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0f;
            Vector3 candidate = transform.position + randomOffset;

            if (!TryProjectOnGround(candidate, out Vector3 grounded))
                continue;
            if (!IsFarEnoughFromExistingItems(grounded))
                continue;

            spawnPos = grounded;
            return true;
        }

        spawnPos = default;
        return false;
    }

    private bool TryProjectOnGround(Vector3 worldPos, out Vector3 grounded)
    {
        if (preferTerrainHeight && TryProjectOnTerrain(worldPos, out grounded))
            return true;

        Vector3 rayStart = worldPos + Vector3.up * Mathf.Max(1f, groundProbeHeight);
        float rayDistance = Mathf.Max(2f, groundProbeDistance);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < minGroundNormalY)
            {
                grounded = default;
                return false;
            }
            grounded = hit.point + Vector3.up * groundOffset;
            return true;
        }

        grounded = default;
        return false;
    }

    private bool TryProjectOnTerrain(Vector3 worldPos, out Vector3 grounded)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            grounded = default;
            return false;
        }

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float minX = terrainPos.x;
        float maxX = terrainPos.x + terrainSize.x;
        float minZ = terrainPos.z;
        float maxZ = terrainPos.z + terrainSize.z;

        if (worldPos.x < minX || worldPos.x > maxX || worldPos.z < minZ || worldPos.z > maxZ)
        {
            grounded = default;
            return false;
        }

        float y = terrain.SampleHeight(worldPos) + terrainPos.y;
        grounded = new Vector3(worldPos.x, y + groundOffset, worldPos.z);
        return true;
    }

    private bool IsFarEnoughFromExistingItems(Vector3 point)
    {
        float minDist = Mathf.Max(0f, minDistanceBetweenItems);
        if (minDist <= 0.01f) return true;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            GameObject item = spawnedItems[i];
            if (item == null) continue;
            if (Vector3.Distance(item.transform.position, point) < minDist)
                return false;
        }

        return true;
    }

    private int CountInitialCorpses()
    {
        Damageable[] allDamageables = FindObjectsByType<Damageable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int corpseCount = 0;
        for (int i = 0; i < allDamageables.Length; i++)
        {
            Damageable d = allDamageables[i];
            if (d != null && d.IsCorpse)
                corpseCount++;
        }

        return corpseCount;
    }

    private void OnEnemyCorpseSpawned(EnemyCorpseSpawnedEvent e)
    {
        currentCorpseCount++;
    }

    private void OnEnemyCorpseRemoved(EnemyCorpseRemovedEvent e)
    {
        currentCorpseCount = Mathf.Max(0, currentCorpseCount - 1);
    }

    // Показываем радиус в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
