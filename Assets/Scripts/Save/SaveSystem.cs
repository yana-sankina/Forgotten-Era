using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Статический менеджер сохранений.
/// Собирает данные из всех систем игрока, сериализует в JSON, пишет в файл.
/// </summary>
public static class SaveSystem
{
    public const int MAX_SLOTS = 3;
    private const int CURRENT_SAVE_VERSION = 1;
    private const string FILE_PREFIX = "save_slot_";
    private const string FILE_EXT = ".json";

    /// <summary>
    /// Сохранить текущее состояние игрока в указанный слот.
    /// </summary>
    public static bool Save(int slotIndex)
    {
        slotIndex = Mathf.Clamp(slotIndex, 0, MAX_SLOTS - 1);

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("SaveSystem: Игрок не найден!");
            return false;
        }

        SaveData data = CollectPlayerData(player, slotIndex);
        if (data == null) return false;

        try
        {
            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slotIndex);

            // Создаём директорию если не существует
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json);
            Debug.Log("Сохранено в слот " + (slotIndex + 1) + ": " + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SaveSystem: Ошибка сохранения: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Загрузить данные из указанного слота.
    /// Возвращает null если файл не найден или повреждён.
    /// </summary>
    public static SaveData Load(int slotIndex)
    {
        slotIndex = Mathf.Clamp(slotIndex, 0, MAX_SLOTS - 1);
        string path = GetSavePath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.LogWarning("SaveSystem: Файл сохранения не найден: " + path);
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null && data.saveVersion <= 0)
                data.saveVersion = CURRENT_SAVE_VERSION; // Старые сейвы до версиирования.

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("SaveSystem: Ошибка загрузки: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Есть ли сохранение в указанном слоте?
    /// </summary>
    public static bool HasSave(int slotIndex)
    {
        return File.Exists(GetSavePath(Mathf.Clamp(slotIndex, 0, MAX_SLOTS - 1)));
    }

    /// <summary>
    /// Есть ли хотя бы одно сохранение?
    /// </summary>
    public static bool HasAnySave()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (HasSave(i)) return true;
        }
        return false;
    }

    /// <summary>
    /// Удалить сохранение из указанного слота.
    /// </summary>
    public static void DeleteSave(int slotIndex)
    {
        string path = GetSavePath(Mathf.Clamp(slotIndex, 0, MAX_SLOTS - 1));
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Сохранение удалено из слота " + (slotIndex + 1) + ": " + path);
        }
    }

    /// <summary>
    /// Получить краткую инфу о слоте для UI (без полной загрузки).
    /// </summary>
    public static SaveData GetSaveInfo(int slotIndex)
    {
        return Load(slotIndex);
    }

    /// <summary>
    /// Путь к файлу сохранения.
    /// </summary>
    public static string GetSavePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, FILE_PREFIX + slotIndex + FILE_EXT);
    }

    private static GameObject FindPlayer()
    {
        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                return taggedPlayer;
        }
        catch (UnityException)
        {
            // Проект может тестироваться без настроенного Player tag.
        }

        PlayerHealth health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null)
            return health.gameObject;

        DinosaurInitializer initializer = UnityEngine.Object.FindAnyObjectByType<DinosaurInitializer>();
        if (initializer != null)
            return initializer.gameObject;

        PlayerInput input = UnityEngine.Object.FindAnyObjectByType<PlayerInput>();
        if (input != null)
            return input.gameObject;

        PlayerNeeds needs = UnityEngine.Object.FindAnyObjectByType<PlayerNeeds>();
        if (needs != null)
            return needs.gameObject;

        return null;
    }

    /// <summary>
    /// Собрать все данные игрока в SaveData.
    /// </summary>
    private static SaveData CollectPlayerData(GameObject player, int slotIndex)
    {
        SaveData data = new SaveData();
        data.saveVersion = CURRENT_SAVE_VERSION;
        data.slotIndex = slotIndex;
        data.saveTimestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        // Позиция
        data.posX = player.transform.position.x;
        data.posY = player.transform.position.y;
        data.posZ = player.transform.position.z;
        data.rotationY = player.transform.eulerAngles.y;

        // Вид
        if (GameSession.SelectedSpecies != null)
            data.speciesName = GameSession.SelectedSpecies.speciesName;

        // Рост
        PlayerGrowth growth = player.GetComponent<PlayerGrowth>();
        if (growth != null)
        {
            data.currentGrowth = growth.CurrentGrowth;
            data.growthStageIndex = growth.CurrentStageIndex;
        }

        // Здоровье
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            data.currentHP = health.currentHP;
        }

        // Опыт
        ExperienceSystem xp = player.GetComponent<ExperienceSystem>();
        if (xp != null)
        {
            data.currentXP = xp.CurrentXP;
            data.currentLevel = xp.CurrentLevel;
        }

        // Прокачка
        StatUpgradeSystem stats = player.GetComponent<StatUpgradeSystem>();
        if (stats != null)
        {
            data.availablePoints = stats.AvailablePoints;
            data.bonusHP = stats.BonusHP;
            data.bonusATK = stats.BonusATK;
            data.bonusSPD = stats.BonusSPD;
        }

        // Потребности
        PlayerNeeds needs = player.GetComponent<PlayerNeeds>();
        if (needs != null)
        {
            data.currentHunger = needs.CurrentHunger;
            data.currentThirst = needs.CurrentThirst;
        }

        return data;
    }
}
