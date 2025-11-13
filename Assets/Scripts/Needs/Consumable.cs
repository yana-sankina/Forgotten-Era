using UnityEngine;

public class Consumable : MonoBehaviour
{
    public enum ConsumableType { Food, Water }
    public ConsumableType type;

    [Header("Food")]
    public float restoreAmountPerBite = 25f;
    public int usesLeft = 5;

    [Header("Water")]
    public float restoreAmountPerTick = 5f;
    public float restoreInterval = 2f;
}
