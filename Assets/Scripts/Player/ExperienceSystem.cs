using UnityEngine;

/// <summary>
/// Система опыта. Висит на объекте игрока.
/// Получает XP за убийства (EnemyKilledEvent), считает уровни, раздаёт очки.
/// </summary>
public class ExperienceSystem : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Базовое количество XP для первого уровня")]
    [SerializeField] private int baseXPPerLevel = 100;
    [Tooltip("На сколько XP растёт каждый следующий уровень")]
    [SerializeField] private int xpScaling = 150;
    [Tooltip("Очков характеристик за каждый уровень")]
    [SerializeField] private int statPointsPerLevel = 1;

    private int currentXP = 0;
    private int currentLevel = 1;

    public int CurrentXP => currentXP;
    public int CurrentLevel => currentLevel;
    public int XPToNextLevel => baseXPPerLevel + (currentLevel - 1) * xpScaling;

    private void OnEnable()
    {
        EventBroker.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void Start()
    {
        // Публикуем начальное состояние для UI
        PublishXPEvent();
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        AddXP(e.XPReward);
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        Debug.Log("+" + amount + " XP! Всего: " + currentXP + "/" + XPToNextLevel);

        // Проверяем левел-апы (может быть несколько за раз)
        while (currentXP >= XPToNextLevel)
        {
            currentXP -= XPToNextLevel;
            currentLevel++;

            Debug.Log("LEVEL UP! Уровень " + currentLevel + "! +" + statPointsPerLevel + " очков");

            EventBroker.Publish(new LevelUpEvent
            {
                NewLevel = currentLevel,
                StatPoints = statPointsPerLevel
            });
        }

        PublishXPEvent();
    }

    private void PublishXPEvent()
    {
        EventBroker.Publish(new ExperienceChangedEvent
        {
            CurrentXP = currentXP,
            XPToNextLevel = XPToNextLevel,
            Level = currentLevel
        });
    }
}
