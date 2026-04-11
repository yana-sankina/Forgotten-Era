using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Центральный менеджер простых визуальных эффектов.
/// На первом этапе обрабатывает только экранную вспышку при получении урона игроком.
/// Остальные поля подготовлены под следующие шаги плана.
/// </summary>
public class VisualFeedbackManager : MonoBehaviour
{
    [Header("Player Damage")]
    [SerializeField] private Image playerDamageOverlay;
    [SerializeField] private float playerDamageFlashAlpha = 0.4f;
    [SerializeField] private float playerDamageFadeInDuration = 0.05f;
    [SerializeField] private float playerDamageFadeOutDuration = 0.25f;

    [Header("Enemy Damage")]
    [SerializeField] private GameObject enemyHitEffectPrefab;
    [SerializeField] private float enemyHitEffectOffset = 0.2f;
    [SerializeField] private float enemyHitEffectLiftY = 0.2f;
    [SerializeField] private GameObject stunEffectPrefab;
    [SerializeField] private float stunEffectHeightOffset = 1.5f;
    [SerializeField] private GameObject bleedEffectPrefab;
    [SerializeField] private float bleedEffectHeightOffset = 1.2f;

    [Header("Needs")]
    [SerializeField] private GameObject eatEffectPrefab;
    [SerializeField] private GameObject drinkEffectPrefab;
    [SerializeField] private float needsEffectOffsetY = 0.1f;

    private Coroutine playerDamageFlashCoroutine;
    private Color playerDamageOverlayBaseColor = Color.clear;
    private readonly Dictionary<Damageable, GameObject> activeStunEffects = new();
    private readonly Dictionary<Damageable, Coroutine> activeStunCoroutines = new();
    private readonly Dictionary<Damageable, GameObject> activeBleedEffects = new();
    private readonly Dictionary<Damageable, Coroutine> activeBleedCoroutines = new();

    private void Awake()
    {
        if (playerDamageOverlay != null)
        {
            playerDamageOverlayBaseColor = playerDamageOverlay.color;
            SetOverlayAlpha(0f);
        }
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBroker.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
        EventBroker.Subscribe<EnemyStunnedEvent>(OnEnemyStunned);
        EventBroker.Subscribe<EnemyBleedingEvent>(OnEnemyBleeding);
        EventBroker.Subscribe<PlayerAteEvent>(OnPlayerAte);
        EventBroker.Subscribe<PlayerDrankEvent>(OnPlayerDrank);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBroker.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
        EventBroker.Unsubscribe<EnemyStunnedEvent>(OnEnemyStunned);
        EventBroker.Unsubscribe<EnemyBleedingEvent>(OnEnemyBleeding);
        EventBroker.Unsubscribe<PlayerAteEvent>(OnPlayerAte);
        EventBroker.Unsubscribe<PlayerDrankEvent>(OnPlayerDrank);
        ClearStunEffects();
        ClearBleedEffects();
    }

    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        if (playerDamageOverlay == null)
            return;

        if (playerDamageFlashCoroutine != null)
            StopCoroutine(playerDamageFlashCoroutine);

        playerDamageFlashCoroutine = StartCoroutine(PlayerDamageFlashRoutine());
    }

    private void OnEnemyDamaged(EnemyDamagedEvent e)
    {
        if (enemyHitEffectPrefab == null)
            return;

        Vector3 spawnPosition = e.HitPoint;
        if (e.Target != null)
        {
            Vector3 direction = e.HitPoint - e.Target.transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.up;

            spawnPosition += direction.normalized * enemyHitEffectOffset;
        }

        spawnPosition += Vector3.up * enemyHitEffectLiftY;

        GameObject effectInstance = Instantiate(enemyHitEffectPrefab, spawnPosition, Quaternion.identity);
        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play(true);
        }
    }

    private void OnPlayerAte(PlayerAteEvent e)
    {
        SpawnSimpleEffect(eatEffectPrefab, e.Position + Vector3.up * needsEffectOffsetY);
    }

    private void OnPlayerDrank(PlayerDrankEvent e)
    {
        SpawnSimpleEffect(drinkEffectPrefab, e.Position + Vector3.up * needsEffectOffsetY);
    }

    private void OnEnemyStunned(EnemyStunnedEvent e)
    {
        if (stunEffectPrefab == null || e.Target == null)
            return;

        if (activeStunCoroutines.TryGetValue(e.Target, out Coroutine existingCoroutine) && existingCoroutine != null)
            StopCoroutine(existingCoroutine);

        if (!activeStunEffects.TryGetValue(e.Target, out GameObject stunEffectInstance) || stunEffectInstance == null)
        {
            Vector3 spawnPosition = e.Target.transform.position + Vector3.up * stunEffectHeightOffset;
            stunEffectInstance = Instantiate(stunEffectPrefab, spawnPosition, Quaternion.identity, e.Target.transform);
            activeStunEffects[e.Target] = stunEffectInstance;

            ParticleSystem[] particleSystems = stunEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play(true);
            }
        }

        activeStunCoroutines[e.Target] = StartCoroutine(RemoveStunEffectAfterDelay(e.Target, e.Duration));
    }

    private void OnEnemyBleeding(EnemyBleedingEvent e)
    {
        if (bleedEffectPrefab == null || e.Target == null)
            return;

        if (activeBleedCoroutines.TryGetValue(e.Target, out Coroutine existingCoroutine) && existingCoroutine != null)
            StopCoroutine(existingCoroutine);

        if (!activeBleedEffects.TryGetValue(e.Target, out GameObject bleedEffectInstance) || bleedEffectInstance == null)
        {
            Vector3 spawnPosition = e.Target.transform.position + Vector3.up * bleedEffectHeightOffset;
            bleedEffectInstance = Instantiate(bleedEffectPrefab, spawnPosition, Quaternion.identity, e.Target.transform);
            activeBleedEffects[e.Target] = bleedEffectInstance;

            ParticleSystem[] particleSystems = bleedEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Play(true);
            }
        }

        activeBleedCoroutines[e.Target] = StartCoroutine(RemoveBleedEffectAfterDelay(e.Target, e.Duration));
    }

    private IEnumerator PlayerDamageFlashRoutine()
    {
        yield return FadeOverlay(0f, playerDamageFlashAlpha, playerDamageFadeInDuration);
        yield return FadeOverlay(playerDamageFlashAlpha, 0f, playerDamageFadeOutDuration);
        playerDamageFlashCoroutine = null;
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (playerDamageOverlay == null)
            yield break;

        if (duration <= 0f)
        {
            SetOverlayAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetOverlayAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetOverlayAlpha(to);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (playerDamageOverlay == null)
            return;

        Color color = playerDamageOverlayBaseColor;
        color.a = Mathf.Clamp01(alpha);
        playerDamageOverlay.color = color;
    }

    private void SpawnSimpleEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null)
            return;

        GameObject effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);
        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play(true);
        }
    }

    private IEnumerator RemoveStunEffectAfterDelay(Damageable target, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveStunEffect(target);
    }

    private void RemoveStunEffect(Damageable target)
    {
        if (target == null)
            return;

        if (activeStunEffects.TryGetValue(target, out GameObject effectInstance) && effectInstance != null)
            Destroy(effectInstance);

        if (activeStunCoroutines.TryGetValue(target, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        activeStunEffects.Remove(target);
        activeStunCoroutines.Remove(target);
    }

    private void ClearStunEffects()
    {
        foreach (KeyValuePair<Damageable, GameObject> pair in activeStunEffects)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        foreach (KeyValuePair<Damageable, Coroutine> pair in activeStunCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        activeStunEffects.Clear();
        activeStunCoroutines.Clear();
    }

    private IEnumerator RemoveBleedEffectAfterDelay(Damageable target, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveBleedEffect(target);
    }

    private void RemoveBleedEffect(Damageable target)
    {
        if (target == null)
            return;

        if (activeBleedEffects.TryGetValue(target, out GameObject effectInstance) && effectInstance != null)
            Destroy(effectInstance);

        if (activeBleedCoroutines.TryGetValue(target, out Coroutine coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        activeBleedEffects.Remove(target);
        activeBleedCoroutines.Remove(target);
    }

    private void ClearBleedEffects()
    {
        foreach (KeyValuePair<Damageable, GameObject> pair in activeBleedEffects)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        foreach (KeyValuePair<Damageable, Coroutine> pair in activeBleedCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        activeBleedEffects.Clear();
        activeBleedCoroutines.Clear();
    }
}
