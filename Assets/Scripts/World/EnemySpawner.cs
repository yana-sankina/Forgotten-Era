using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Спавнер врагов. Ставится на пустой объект на сцене.
/// Спавнит врагов (с Damageable) в радиусе с респавном.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private class EnemyProfileEntry
    {
        public EnemyRuntimeProfile profile;
        [Min(1)] public int weight = 1;
    }

    [Header("Что спавнить")]
    [Tooltip("Префаб врага (должен иметь Damageable + Collider)")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Профили типов врага (опционально)")]
    [SerializeField] private EnemyProfileEntry[] enemyProfiles;

    [Header("Настройки")]
    [Tooltip("Локальный максимум живых врагов для этого спавнера")]
    [SerializeField] private int maxEnemies = 2;
    [Tooltip("Радиус разброса от точки спавна")]
    [SerializeField] private float spawnRadius = 10f;
    [Tooltip("Время респавна после смерти (сек)")]
    [SerializeField] private float respawnTime = 45f;
    [Tooltip("Спавнить при старте сцены")]
    [SerializeField] private bool spawnOnStart = true;
    [Tooltip("Глобальный максимум живых врагов на всей карте")]
    [SerializeField] private int maxAliveEnemiesOnMap = 20;

    [Header("Качество спавна")]
    [Tooltip("Минимальная дистанция спавна от игрока")]
    [SerializeField] private float minDistanceFromPlayer = 20f;
    [Tooltip("Минимальная дистанция между живыми врагами этого спавнера")]
    [SerializeField] private float minDistanceBetweenEnemies = 5f;
    [Tooltip("Сколько попыток найти валидную точку спавна")]
    [SerializeField] private int spawnPointSearchAttempts = 16;
    [Tooltip("Высота старта луча вниз при поиске земли")]
    [SerializeField] private float groundProbeHeight = 60f;
    [Tooltip("Длина луча вниз при поиске земли")]
    [SerializeField] private float groundProbeDistance = 140f;
    [Tooltip("Поднять врага над землёй после Raycast")]
    [SerializeField] private float groundOffset = 0.05f;
    [Tooltip("Смещение модели врага по Y (если модельный pivot кривой)")]
    [SerializeField] private float modelYOffset = 0f;
    [Tooltip("Слои, которые считаем землёй при Raycast fallback")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [Tooltip("Сначала использовать высоту Terrain, если точка попала в Terrain bounds")]
    [SerializeField] private bool preferTerrainHeight = true;
    [Tooltip("Минимальная «вертикальность» нормали поверхности для спавна")]
    [Range(0f, 1f)]
    [SerializeField] private float minGroundNormalY = 0.55f;

    [Header("Performance (code toggles)")]
    [Tooltip("Отключать тени у заспавненной enemy-модели. Можно вернуть, сняв галочку.")]
    [SerializeField] private bool disableModelShadows = true;
    [Tooltip("Лёгкая оптимизация Animator/SkinnedMeshRenderer у enemy-модели.")]
    [SerializeField] private bool optimizeModelComponents = true;

    [Header("Collider Fit")]
    [Tooltip("Автоподгон root-коллайдера врага под размеры модели")]
    [SerializeField] private bool autoFitRootColliderToModel = true;
    [Tooltip("Запас по коллайдеру вокруг модели")]
    [SerializeField] private float colliderPadding = 0.1f;
    [Tooltip("Дополнительное смещение центра root-коллайдера по Y")]
    [SerializeField] private float colliderCenterYOffset = 0f;
    [Tooltip("Множитель высоты авто-коллайдера")]
    [SerializeField] private float colliderHeightScale = 0.8f;
    [Tooltip("Множитель радиуса авто-коллайдера")]
    [SerializeField] private float colliderRadiusScale = 0.4f;
    [Tooltip("Ограничение максимальной высоты авто-коллайдера")]
    [SerializeField] private float maxAutoColliderHeight = 6f;
    [Tooltip("Ограничение максимального радиуса авто-коллайдера")]
    [SerializeField] private float maxAutoColliderRadius = 2.2f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private static readonly HashSet<int> liveEnemyIds = new HashSet<int>();
    private Transform playerTransform;

    private void OnEnable()
    {
        EventBroker.Subscribe<EnemyCorpseSpawnedEvent>(OnCorpseSpawned);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<EnemyCorpseSpawnedEvent>(OnCorpseSpawned);
    }

    private void Start()
    {
        CachePlayerTransform();
        RebuildGlobalStateSnapshot();

        if (spawnOnStart && enemyPrefab != null)
        {
            while (CountLocalAliveEnemies() < maxEnemies && CanSpawnByGlobalLimits())
            {
                if (!SpawnEnemy())
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

            CleanupLocalList();

            while (CountLocalAliveEnemies() < maxEnemies && CanSpawnByGlobalLimits())
            {
                if (!SpawnEnemy())
                    break;
            }
        }
    }

    private bool SpawnEnemy()
    {
        if (enemyPrefab == null) return false;
        if (!CanSpawnByGlobalLimits()) return false;

        if (!TryFindSpawnPosition(out Vector3 spawnPos))
            return false;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
        if (enemy == null) return false;

        spawnedEnemies.Add(enemy);
        liveEnemyIds.Add(enemy.GetInstanceID());

        ConfigureSpawnedEnemy(enemy);
        return true;
    }

    private void ConfigureSpawnedEnemy(GameObject enemy)
    {
        Damageable damageable = enemy.GetComponent<Damageable>();
        if (damageable == null)
        {
            Debug.LogWarning("EnemySpawner: у префаба нет Damageable.", enemy);
            return;
        }

        EnemyProfileEntry entry = ChooseProfileEntry();
        EnemyRuntimeProfile profile = (entry != null) ? entry.profile : null;

        EnemyLifecycle lifecycle = enemy.GetComponent<EnemyLifecycle>();
        if (lifecycle == null)
            lifecycle = enemy.AddComponent<EnemyLifecycle>();

        // Если профиль не задан, оставляем базовые значения префаба.
        if (profile == null)
            return;

        float scale = profile.GetRandomScale();
        enemy.transform.localScale = enemy.transform.localScale * scale;

        float normalizedScale = profile.GetNormalizedScale(scale);
        int hp = profile.GetRandomHP(normalizedScale);
        int attack = profile.GetRandomAttack(normalizedScale);
        int xp = profile.GetRandomXPReward(normalizedScale);
        int corpseUses = profile.GetRandomCorpseUses(normalizedScale);

        damageable.SetRuntimeStats(hp, xp);
        ApplyAttackDamage(enemy, attack);
        ApplyVisualModel(enemy, profile.modelPrefab);
        lifecycle.ConfigureCorpse(profile.corpseLifetimeSeconds, corpseUses);
    }

    private void ApplyAttackDamage(GameObject enemy, int attackDamage)
    {
        EnemyHitbox[] hitboxes = enemy.GetComponentsInChildren<EnemyHitbox>(true);
        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i].attackDamage = attackDamage;
        }
    }

    private void ApplyVisualModel(GameObject enemy, GameObject modelPrefab)
    {
        if (modelPrefab == null) return;

        GameObject modelInstance = Instantiate(modelPrefab, enemy.transform);
        modelInstance.transform.localPosition = new Vector3(0f, modelYOffset, 0f);
        modelInstance.transform.localRotation = Quaternion.identity;

        Collider[] modelColliders = modelInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < modelColliders.Length; i++)
        {
            Destroy(modelColliders[i]);
        }

        ApplyModelPerformanceSettings(modelInstance);
        FitRootColliderToModel(enemy, modelInstance);

        Renderer[] allRenderers = enemy.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (!renderer.transform.IsChildOf(modelInstance.transform))
            {
                renderer.enabled = false;
            }
        }
    }

    private void ApplyModelPerformanceSettings(GameObject modelInstance)
    {
        if (modelInstance == null) return;

        if (disableModelShadows)
        {
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        if (optimizeModelComponents)
        {
            Animator[] animators = modelInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            SkinnedMeshRenderer[] skinned = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                skinned[i].updateWhenOffscreen = false;
            }
        }
    }

    private void FitRootColliderToModel(GameObject enemy, GameObject modelInstance)
    {
        if (!autoFitRootColliderToModel || enemy == null || modelInstance == null)
            return;

        if (!TryGetModelBounds(modelInstance, out Bounds worldBounds))
            return;

        Collider rootCollider = enemy.GetComponent<Collider>();
        if (rootCollider == null)
            return;

        Vector3 localCenter = enemy.transform.InverseTransformPoint(worldBounds.center);
        localCenter.y += colliderCenterYOffset;

        float pad = Mathf.Max(0f, colliderPadding);
        Vector3 size = worldBounds.size;
        size.x = Mathf.Max(0.01f, size.x + pad * 2f);
        size.y = Mathf.Max(0.01f, size.y + pad * 2f);
        size.z = Mathf.Max(0.01f, size.z + pad * 2f);

        if (rootCollider is CapsuleCollider capsule)
        {
            capsule.direction = 1; // Y-axis
            capsule.center = localCenter;
            float scaledRadius = Mathf.Max(size.x, size.z) * 0.5f * Mathf.Max(0.1f, colliderRadiusScale);
            float scaledHeight = size.y * Mathf.Max(0.1f, colliderHeightScale);
            capsule.radius = Mathf.Clamp(scaledRadius, 0.05f, Mathf.Max(0.05f, maxAutoColliderRadius));
            capsule.height = Mathf.Clamp(scaledHeight, capsule.radius * 2f, Mathf.Max(capsule.radius * 2f, maxAutoColliderHeight));
            return;
        }

        if (rootCollider is BoxCollider box)
        {
            box.center = localCenter;
            box.size = size;
            return;
        }

        if (rootCollider is SphereCollider sphere)
        {
            sphere.center = localCenter;
            float scaledRadius = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * 0.5f * Mathf.Max(0.1f, colliderRadiusScale);
            sphere.radius = Mathf.Clamp(scaledRadius, 0.05f, Mathf.Max(0.05f, maxAutoColliderRadius));
        }
    }

    private bool TryGetModelBounds(GameObject modelInstance, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;

        // MeshFilter bounds обычно стабильнее, чем Renderer.bounds у некоторых анимированных моделей.
        MeshFilter[] meshFilters = modelInstance.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds meshBounds = TransformLocalBounds(mf.transform.localToWorldMatrix, mf.sharedMesh.bounds);
            if (!hasBounds)
            {
                bounds = meshBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(meshBounds);
            }
        }

        SkinnedMeshRenderer[] skinned = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            SkinnedMeshRenderer smr = skinned[i];
            Bounds local = smr.localBounds;
            Bounds skinBounds = TransformLocalBounds(smr.transform.localToWorldMatrix, local);

            if (!hasBounds)
            {
                bounds = skinBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(skinBounds);
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        return true;
    }

    private Bounds TransformLocalBounds(Matrix4x4 localToWorld, Bounds localBounds)
    {
        Vector3 c = localBounds.center;
        Vector3 e = localBounds.extents;

        Vector3[] corners = new Vector3[8];
        corners[0] = localToWorld.MultiplyPoint3x4(c + new Vector3( e.x,  e.y,  e.z));
        corners[1] = localToWorld.MultiplyPoint3x4(c + new Vector3( e.x,  e.y, -e.z));
        corners[2] = localToWorld.MultiplyPoint3x4(c + new Vector3( e.x, -e.y,  e.z));
        corners[3] = localToWorld.MultiplyPoint3x4(c + new Vector3( e.x, -e.y, -e.z));
        corners[4] = localToWorld.MultiplyPoint3x4(c + new Vector3(-e.x,  e.y,  e.z));
        corners[5] = localToWorld.MultiplyPoint3x4(c + new Vector3(-e.x,  e.y, -e.z));
        corners[6] = localToWorld.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y,  e.z));
        corners[7] = localToWorld.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z));

        Bounds b = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            b.Encapsulate(corners[i]);
        return b;
    }

    private EnemyProfileEntry ChooseProfileEntry()
    {
        if (enemyProfiles == null || enemyProfiles.Length == 0)
            return null;

        int totalWeight = 0;
        for (int i = 0; i < enemyProfiles.Length; i++)
        {
            EnemyProfileEntry item = enemyProfiles[i];
            if (item == null || item.profile == null) continue;
            totalWeight += Mathf.Max(1, item.weight);
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < enemyProfiles.Length; i++)
        {
            EnemyProfileEntry item = enemyProfiles[i];
            if (item == null || item.profile == null) continue;

            cumulative += Mathf.Max(1, item.weight);
            if (roll < cumulative)
                return item;
        }

        return null;
    }

    private void CleanupLocalList()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = spawnedEnemies[i];
            if (enemy != null) continue;
            spawnedEnemies.RemoveAt(i);
        }
    }

    private int CountLocalAliveEnemies()
    {
        int count = 0;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            GameObject enemy = spawnedEnemies[i];
            if (enemy == null) continue;

            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable != null && !damageable.IsDead && !damageable.IsCorpse)
                count++;
        }
        return count;
    }

    private bool CanSpawnByGlobalLimits()
    {
        return liveEnemyIds.Count < Mathf.Max(1, maxAliveEnemiesOnMap);
    }

    private void RebuildGlobalStateSnapshot()
    {
        liveEnemyIds.Clear();

        Damageable[] allDamageables = FindObjectsOfType<Damageable>(true);
        for (int i = 0; i < allDamageables.Length; i++)
        {
            Damageable d = allDamageables[i];
            if (d == null) continue;

            int id = d.gameObject.GetInstanceID();
            if (!d.IsDead && !d.IsCorpse)
                liveEnemyIds.Add(id);
        }
    }

    private void OnCorpseSpawned(EnemyCorpseSpawnedEvent e)
    {
        liveEnemyIds.Remove(e.InstanceId);
    }

    private void CachePlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        if (playerTransform == null)
            CachePlayerTransform();

        int attempts = Mathf.Max(1, spawnPointSearchAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0f;
            Vector3 candidate = transform.position + randomOffset;

            if (!TryProjectOnGround(candidate, out Vector3 grounded))
                continue;
            if (!IsFarEnoughFromPlayer(grounded))
                continue;
            if (!IsFarEnoughFromLocalEnemies(grounded))
                continue;

            spawnPos = grounded;
            return true;
        }

        spawnPos = transform.position + Vector3.up * groundOffset;
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

    private bool IsFarEnoughFromPlayer(Vector3 point)
    {
        if (playerTransform == null) return true;
        return Vector3.Distance(playerTransform.position, point) >= Mathf.Max(0f, minDistanceFromPlayer);
    }

    private bool IsFarEnoughFromLocalEnemies(Vector3 point)
    {
        float minDist = Mathf.Max(0f, minDistanceBetweenEnemies);
        if (minDist <= 0.01f) return true;

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            GameObject enemy = spawnedEnemies[i];
            if (enemy == null) continue;

            Damageable damageable = enemy.GetComponent<Damageable>();
            if (damageable == null || damageable.IsDead || damageable.IsCorpse)
                continue;

            if (Vector3.Distance(enemy.transform.position, point) < minDist)
                return false;
        }

        return true;
    }

    // Показываем радиус в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
