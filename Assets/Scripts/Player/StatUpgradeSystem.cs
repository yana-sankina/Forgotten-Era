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
        Debug.Log("Стадия " + e.StageIndex + "! +" + e.BonusPoints + " очков (всего: " + availablePoints + ")");
        PublishPointsEvent();
    }

    private void OnLevelUp(LevelUpEvent e)
    {
        availablePoints += e.StatPoints;
        Debug.Log("Level Up → +" + e.StatPoints + " очков (всего: " + availablePoints + ")");
        PublishPointsEvent();
    }

    public bool UpgradeHP()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusHP += hpPerPoint;
        Debug.Log("Прокачка HP! +" + hpPerPoint + " (бонус: " + bonusHP + ")");
        PublishAll();
        return true;
    }

    public bool UpgradeATK()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusATK += atkPerPoint;
        Debug.Log("Прокачка ATK! +" + atkPerPoint + " (бонус: " + bonusATK + ")");
        PublishAll();
        return true;
    }

    public bool UpgradeSPD()
    {
        if (availablePoints <= 0) return false;
        availablePoints--;
        bonusSPD += spdPerPoint;
        Debug.Log("Прокачка SPD! +" + spdPerPoint + " (бонус: " + bonusSPD + ")");
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
