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
