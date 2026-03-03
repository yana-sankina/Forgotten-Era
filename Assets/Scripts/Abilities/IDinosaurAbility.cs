/// <summary>
/// Интерфейс для уникальных способностей видов динозавров.
/// Каждый вид реализует свою способность, вызываемую на кнопку Q.
/// </summary>
public interface IDinosaurAbility
{
    /// <summary>Название способности для UI.</summary>
    string AbilityName { get; }

    /// <summary>Время перезарядки в секундах.</summary>
    float Cooldown { get; }

    /// <summary>Готова ли способность к использованию.</summary>
    bool IsReady { get; }

    /// <summary>Активировать способность.</summary>
    void Activate();
}
