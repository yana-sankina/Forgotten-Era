using UnityEngine;

/// <summary>
/// Зона воды. Вешается на объект с триггер-коллайдером.
/// Когда игрок заходит внутрь — может пить (E).
/// Вода бесконечная, не исчезает.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterZone : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Сколько жажды восполняется за тик")]
    [SerializeField] private float restorePerTick = 5f;
    [Tooltip("Интервал между тиками восполнения (сек)")]
    [SerializeField] private float restoreInterval = 1f;

    private void Awake()
    {
        // Автоматически создаём Consumable типа Water
        Consumable consumable = GetComponent<Consumable>();
        if (consumable == null)
            consumable = gameObject.AddComponent<Consumable>();

        consumable.type = Consumable.ConsumableType.Water;
        consumable.restoreAmountPerTick = restorePerTick;
        consumable.restoreInterval = restoreInterval;

        // Нужен триггер-коллайдер. MeshCollider не может быть триггером — добавляем BoxCollider
        Collider existingCol = GetComponent<Collider>();
        if (existingCol is MeshCollider)
        {
            // MeshCollider оставляем как есть, добавляем BoxCollider-триггер поверх
            BoxCollider triggerCol = gameObject.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            // Размер подгоняем под MeshCollider
            triggerCol.center = existingCol.bounds.center - transform.position;
            triggerCol.size = existingCol.bounds.size;
        }
        else if (existingCol != null)
        {
            existingCol.isTrigger = true;
        }
    }
}
