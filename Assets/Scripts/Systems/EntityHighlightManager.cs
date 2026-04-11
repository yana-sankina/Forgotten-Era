using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Используется для подсветки врагов способностью Пектинодона.
/// </summary>
public class EntityHighlightManager : MonoBehaviour
{
    [Header("Marker")]
    [SerializeField] private GameObject highlightMarkerPrefab;
    [SerializeField] private float markerHeightOffset = 1.5f;

    private readonly Dictionary<Transform, HighlightEntry> activeHighlights = new();

    private void OnEnable()
    {
        EventBroker.Subscribe<EntityDetectedEvent>(OnEntityDetected);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<EntityDetectedEvent>(OnEntityDetected);
        ClearAllHighlights();
    }

    private void LateUpdate()
    {
        if (activeHighlights.Count == 0)
            return;

        List<Transform> toRemove = null;
        foreach (KeyValuePair<Transform, HighlightEntry> pair in activeHighlights)
        {
            Transform target = pair.Key;
            HighlightEntry entry = pair.Value;

            if (target == null || entry.MarkerInstance == null)
            {
                if (toRemove == null) toRemove = new List<Transform>();
                toRemove.Add(target);
                continue;
            }

            entry.MarkerInstance.transform.position = target.position + Vector3.up * markerHeightOffset;
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
        {
            RemoveHighlight(toRemove[i]);
        }
    }

    private void OnEntityDetected(EntityDetectedEvent e)
    {
        if (highlightMarkerPrefab == null || e.EntityTransform == null)
            return;

        if (activeHighlights.TryGetValue(e.EntityTransform, out HighlightEntry existingEntry))
        {
            if (existingEntry.LifetimeCoroutine != null)
                StopCoroutine(existingEntry.LifetimeCoroutine);

            existingEntry.LifetimeCoroutine = StartCoroutine(RemoveAfterDelay(e.EntityTransform, e.Duration));
            activeHighlights[e.EntityTransform] = existingEntry;
            return;
        }

        Vector3 spawnPosition = e.EntityTransform.position + Vector3.up * markerHeightOffset;
        GameObject markerInstance = Instantiate(highlightMarkerPrefab, spawnPosition, Quaternion.identity);

        HighlightEntry newEntry = new HighlightEntry
        {
            MarkerInstance = markerInstance,
            LifetimeCoroutine = StartCoroutine(RemoveAfterDelay(e.EntityTransform, e.Duration))
        };

        activeHighlights[e.EntityTransform] = newEntry;
    }

    private IEnumerator RemoveAfterDelay(Transform target, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveHighlight(target);
    }

    private void RemoveHighlight(Transform target)
    {
        if (target == null)
            return;

        if (!activeHighlights.TryGetValue(target, out HighlightEntry entry))
            return;

        if (entry.MarkerInstance != null)
            Destroy(entry.MarkerInstance);

        if (entry.LifetimeCoroutine != null)
            StopCoroutine(entry.LifetimeCoroutine);

        activeHighlights.Remove(target);
    }

    private void ClearAllHighlights()
    {
        foreach (KeyValuePair<Transform, HighlightEntry> pair in activeHighlights)
        {
            if (pair.Value.MarkerInstance != null)
                Destroy(pair.Value.MarkerInstance);

            if (pair.Value.LifetimeCoroutine != null)
                StopCoroutine(pair.Value.LifetimeCoroutine);
        }

        activeHighlights.Clear();
    }

    private struct HighlightEntry
    {
        public GameObject MarkerInstance;
        public Coroutine LifetimeCoroutine;
    }
}
