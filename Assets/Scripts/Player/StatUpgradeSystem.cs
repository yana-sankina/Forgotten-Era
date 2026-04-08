using UnityEngine;

/// <summary>
/// Система распределения очков характеристик.
/// Накапливает очки от стадий роста и уровней.
/// Игрок тратит очки на +HP, +ATK или +SPD.
/// </summary>
public class StatUpgradeSystem : MonoBehaviour
{
    [Header("Бонус за одно очко")]
    [SerializeField] private int hpPerPoint = 15;
    [SerializeField] private int atkPerPoint = 3;
    [SerializeField] private float spdPerPoint = 0.3f;

    private int availablePoints = 0;
    private int bonusHP = 0;
    private int bonusATK = 0;
    private float bonusSPD = 0f;

    public int AvailablePoints => availablePoints;
    public int BonusHP => bonusHP;
    public int BonusATK => bonusATK;
    public float BonusSPD => bonusSPD;

    /// <summary>
    /// Восстановить очки и бонусы из сохранения.
    /// </summary>
    public void LoadState(int points, int hp, int atk, float spd)
    {
        availablePoints = Mathf.Max(0, points);
        bonusHP = hp;
        bonusATK = atk;
        bonusSPD = spd;
        PublishAll();
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<GrowthStageReachedEvent>(OnStageReached);
        EventBroker.Subscribe<LevelUpEvent>(OnLevelUp);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<GrowthStageReachedEvent>(OnStageReached);
        EventBroker.Unsubscribe<LevelUpEvent>(OnLevelUp);
    }

    private void OnStageReached(GrowthStageReachedEvent e)
    {
        availablePoints += e.BonusPoints;

        PublishPointsEvent();
    }

    private void OnLevelUp(LevelUpEvent e)
    {
        availablePoints += e.StatPoints;

        PublishPointsEvent();
    }

    public bool UpgradeHP()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusHP += hpPerPoint;

        PublishAll();
        return true;
    }

    public bool UpgradeATK()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusATK += atkPerPoint;

        PublishAll();
        return true;
    }

    public bool UpgradeSPD()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusSPD += spdPerPoint;

        PublishAll();
        return true;
    }

    private void PublishAll()
    {
        PublishPointsEvent();
        EventBroker.Publish(new PlayerBonusStatsUpdatedEvent
        {
            BonusHP = bonusHP,
            BonusATK = bonusATK,
            BonusSPD = bonusSPD
        });
    }

    private void PublishPointsEvent()
    {
        EventBroker.Publish(new StatPointsChangedEvent
        {
            AvailablePoints = availablePoints
        });
    }
}
