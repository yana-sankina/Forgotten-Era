using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Спавнер врагов. Ставится на пустой объект на сцене.
/// Спавнит врагов (с Damageable) в радиусе с респавном.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Что спавнить")]
    [Tooltip("Префаб врага (должен иметь Damageable + Collider)")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Настройки")]
    [Tooltip("Максимум врагов одновременно")]
    [SerializeField] private int maxEnemies = 2;
    [Tooltip("Радиус разброса от точки спавна")]
    [SerializeField] private float spawnRadius = 10f;
    [Tooltip("Время респавна после смерти (сек)")]
    [SerializeField] private float respawnTime = 45f;
    [Tooltip("Спавнить при старте сцены")]
    [SerializeField] private bool spawnOnStart = true;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        if (spawnOnStart && enemyPrefab != null)
        {
            for (int i = 0; i < maxEnemies; i++)
            {
                SpawnEnemy();
            }
        }

        StartCoroutine(RespawnLoop());
    }

    private IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnTime);

            // Убираем мёртвых/удалённых
            spawnedEnemies.RemoveAll(e => e == null);

            // Также убираем мёртвых что ещё существуют как объекты
            spawnedEnemies.RemoveAll(e =>
            {
                var damageable = e.GetComponent<Damageable>();
                return damageable != null && damageable.IsDead;
            });

            // Доспавниваем
            while (spawnedEnemies.Count < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0;

        Vector3 spawnPos = transform.position + randomOffset;

        // На поверхность
        if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
        {
            spawnPos = hit.point + Vector3.up * 0.5f;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
        spawnedEnemies.Add(enemy);
    }

    // Показываем радиус в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
