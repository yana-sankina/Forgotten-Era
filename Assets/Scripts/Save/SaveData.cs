/// <summary>
/// Данные одного слота сохранения.
/// Сериализуется в JSON через JsonUtility.
/// </summary>
[System.Serializable]
public class SaveData
{
    // Версия формата сейва
    public int saveVersion;

    // Вид динозавра
    public string speciesName;

    // Позиция и поворот игрока
    public float posX;
    public float posY;
    public float posZ;
    public float rotationY;

    // Рост
    public float currentGrowth;
    public int growthStageIndex;

    // Здоровье
    public int currentHP;

    // Опыт
    public int currentXP;
    public int currentLevel;

    // Прокачка
    public int availablePoints;
    public int bonusHP;
    public int bonusATK;
    public float bonusSPD;

    // Потребности
    public float currentHunger;
    public float currentThirst;

    // Мета
    public string saveTimestamp;
    public int slotIndex;
}
