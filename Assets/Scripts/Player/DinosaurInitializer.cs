using UnityEngine;
using System.Collections;

/// <summary>
/// Висит на объекте игрока. При старте сцены читает выбранный вид
/// из GameSession и инициализирует все компоненты: SO + способность.
/// Вид фиксируется до конца игры (до возврата в главное меню).
/// </summary>
public class DinosaurInitializer : MonoBehaviour
{
    [Header("Фоллбэк (для тестирования без меню)")]
    [Tooltip("Если GameSession.SelectedSpecies пуст — используется этот SO")]
    [SerializeField] private DinosaurSpeciesData fallbackSpecies;

    [Header("Ссылки")]
    [SerializeField] private Hitbox attackHitbox;

    private void Awake()
    {
        // Читаем выбор из GameSession (задан в меню)
        DinosaurSpeciesData species = GameSession.SelectedSpecies;

        // Если меню не было (тестируем напрямую Game сцену) — используем фоллбэк
        if (species == null)
        {
            species = fallbackSpecies;
            if (species == null)
            {
                Debug.LogError("Вид не выбран и фоллбэк не назначен! Назначь Fallback Species в инспекторе.", this);
                return;
            }
            Debug.LogWarning("GameSession пуст — используется фоллбэк: " + species.speciesName);
            GameSession.SelectedSpecies = species;
        }

        // 1. Сначала добавляем способность (чтобы она подписалась на события)
        AttachAbility(species);

        // 2. Потом запускаем рост (он публикует PlayerStatsUpdatedEvent,
        //    которое способность уже может поймать)
        PlayerGrowth growth = GetComponent<PlayerGrowth>();
        if (growth != null)
        {
            growth.Initialize(species);
        }

        // 3. Модель и анимации
        SetupModel(species);

    }

    private IEnumerator Start()
    {
        if (!GameSession.IsLoadingFromSave)
            yield break;

        // Ждём один кадр, чтобы PlayerHealth/PlayerNeeds/UI успели пройти Start/OnEnable.
        yield return null;

        ApplySaveData(GetComponent<PlayerGrowth>());
        GameSession.IsLoadingFromSave = false;
    }

    /// <summary>
    /// Восстановить все данные игрока из сохранения.
    /// </summary>
    private void ApplySaveData(PlayerGrowth growth)
    {
        SaveData data = SaveSystem.Load(GameSession.ActiveSaveSlot);
        if (data == null)
        {
            Debug.LogWarning("Не удалось загрузить сейв из слота " + GameSession.ActiveSaveSlot);
            return;
        }

        // Позиция (отключаем CC чтобы teleport сработал)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = new Vector3(data.posX, data.posY, data.posZ);
        transform.rotation = Quaternion.Euler(0, data.rotationY, 0);
        if (cc != null) cc.enabled = true;

        // Рост
        if (growth != null)
            growth.LoadState(data.currentGrowth, data.growthStageIndex);

        // Прокачка (ПЕРЕД здоровьем, чтобы maxHP был пересчитан)
        StatUpgradeSystem stats = GetComponent<StatUpgradeSystem>();
        if (stats != null)
            stats.LoadState(data.availablePoints, data.bonusHP, data.bonusATK, data.bonusSPD);

        if (growth != null)
            growth.RefreshStats();

        // Здоровье (ПОСЛЕ прокачки)
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
            health.LoadHP(data.currentHP);

        // Опыт
        ExperienceSystem xp = GetComponent<ExperienceSystem>();
        if (xp != null)
            xp.LoadState(data.currentXP, data.currentLevel);

        // Потребности
        PlayerNeeds needs = GetComponent<PlayerNeeds>();
        if (needs != null)
            needs.LoadNeeds(data.currentHunger, data.currentThirst);
    }

    private void SetupModel(DinosaurSpeciesData species)
    {
        if (species.modelPrefab == null)
        {
            Debug.LogWarning("modelPrefab не задан для " + species.speciesName + " — игрок остаётся кубом.");
            return;
        }

        // Добавляем ModelSwitcher если нет, и спавним модель
        ModelSwitcher switcher = GetComponent<ModelSwitcher>();
        if (switcher == null)
            switcher = gameObject.AddComponent<ModelSwitcher>();

        switcher.SwitchModel(species.modelPrefab, species.modelYOffset);

        // Подгоняем CharacterController под размер модели
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = species.controllerHeight;
            cc.radius = species.controllerRadius;
            cc.center = new Vector3(0, species.controllerCenterY, 0);
        }

        // Добавляем DinosaurAnimator и инициализируем его
        Animator modelAnimator = switcher.GetModelAnimator();
        if (modelAnimator != null)
        {
            DinosaurAnimator dAnimator = GetComponent<DinosaurAnimator>();
            if (dAnimator == null)
                dAnimator = gameObject.AddComponent<DinosaurAnimator>();

            dAnimator.Init(modelAnimator);
        }
    }

    private void AttachAbility(DinosaurSpeciesData species)
    {
        // Убираем все старые способности (на случай если были)
        RemoveExistingAbilities();

        // Определяем тип по имени SO
        string speciesName = species.speciesName.ToLower().Trim();

        IDinosaurAbility ability = null;

        if (speciesName.Contains("дакотараптор") || speciesName.Contains("dakotaraptor"))
        {
            var comp = gameObject.AddComponent<DakotaraptorAbility>();
            ability = comp;
        }
        else if (speciesName.Contains("тиранозавр") || speciesName.Contains("тирекс") || speciesName.Contains("рекс") || speciesName.Contains("tyrannosaurus") || speciesName.Contains("t-rex") || speciesName.Contains("rex"))
        {
            var comp = gameObject.AddComponent<TyrannosaurusAbility>();
            ability = comp;
        }
        else if (speciesName.Contains("пектинодон") || speciesName.Contains("pectinodon"))
        {
            var comp = gameObject.AddComponent<PectinodonAbility>();
            ability = comp;
        }
        else
        {
            Debug.LogWarning("Неизвестный вид: " + species.speciesName + ". Способность не назначена.");
            return;
        }
    }

    private void RemoveExistingAbilities()
    {
        var existing = GetComponents<MonoBehaviour>();
        foreach (var comp in existing)
        {
            if (comp is IDinosaurAbility && comp != this)
            {
                Destroy(comp);
            }
        }
    }
}
