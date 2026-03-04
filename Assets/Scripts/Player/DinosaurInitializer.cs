using UnityEngine;

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
        }

        Debug.Log("Инициализация вида: " + species.speciesName);

        // 1. Сначала добавляем способность (чтобы она подписалась на события)
        AttachAbility(species);

        // 2. Потом запускаем рост (он публикует PlayerStatsUpdatedEvent,
        //    которое способность уже может поймать)
        PlayerGrowth growth = GetComponent<PlayerGrowth>();
        if (growth != null)
        {
            growth.Initialize(species);
        }
    }

    private void AttachAbility(DinosaurSpeciesData species)
    {
        // Убираем все старые способности (на случай если были)
        RemoveExistingAbilities();

        // Определяем тип по имени SO. Можно заменить на enum если имена будут меняться.
        string speciesName = species.speciesName.ToLower().Trim();

        IDinosaurAbility ability = null;

        if (speciesName.Contains("велоцираптор") || speciesName.Contains("velociraptor"))
        {
            var comp = gameObject.AddComponent<VelociraptorAbility>();
            ability = comp;
        }
        else if (speciesName.Contains("тиранозавр") || speciesName.Contains("tyrannosaurus") || speciesName.Contains("t-rex") || speciesName.Contains("rex"))
        {
            var comp = gameObject.AddComponent<TyrannosaurusAbility>();
            ability = comp;
        }
        else if (speciesName.Contains("троодон") || speciesName.Contains("troodon"))
        {
            var comp = gameObject.AddComponent<TroodonAbility>();
            ability = comp;
        }
        else
        {
            Debug.LogWarning("Неизвестный вид: " + species.speciesName + ". Способность не назначена.");
            return;
        }

        Debug.Log("Способность назначена: " + ability.AbilityName);
    }

    private void RemoveExistingAbilities()
    {
        // Удаляем все существующие способности перед добавлением новой
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
