public struct PlayerHealthChangedEvent
{
    public int CurrentHP;
    public int MaxHP;
}

public struct PlayerDiedEvent { }

public struct PlayerRespawnedEvent
{
    public UnityEngine.Vector3 Position;
}
public struct PlayerAttackEvent { }
public struct PlayerGrowthChangedEvent
{
    public float CurrentGrowth;
    public float MaxGrowth;
}

public struct PlayerStatsUpdatedEvent
{
    public int NewMaxHP;
    public int NewAttackDamage;
    public float NewMoveSpeed;
}

public struct PlayerHungerChangedEvent
{
    public float Current;
    public float Max;
}

public struct PlayerThirstChangedEvent
{
    public float Current;
    public float Max;
}

public struct EnvironmentDamageEvent
{
    public int DamageAmount;
}

public struct PlayerStaminaChangedEvent
{
    public float Current;
    public float Max;
}

public struct PlayerNeedsCapacityUpdatedEvent
{
    public float NewMaxHunger;
    public float NewMaxThirst;
    public float HungerDecayRate;
    public float ThirstDecayRate;
}

public struct EnemyKilledEvent
{
    public int XPReward;
}

public struct AbilityUsedEvent
{
    public string AbilityName;
    public float Cooldown;
}

public struct EntityDetectedEvent
{
    public UnityEngine.Transform EntityTransform;
    public float Duration;
}

// === Этап 2: Прокачка ===

public struct GrowthStageReachedEvent
{
    public int StageIndex;
    public int BonusPoints;
}

public struct ExperienceChangedEvent
{
    public int CurrentXP;
    public int XPToNextLevel;
    public int Level;
}

public struct LevelUpEvent
{
    public int NewLevel;
    public int StatPoints;
}

public struct StatPointsChangedEvent
{
    public int AvailablePoints;
}

public struct PlayerBonusStatsUpdatedEvent
{
    public int BonusHP;
    public int BonusATK;
    public float BonusSPD;
}
