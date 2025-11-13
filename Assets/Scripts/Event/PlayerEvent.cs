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