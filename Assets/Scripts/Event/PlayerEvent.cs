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
