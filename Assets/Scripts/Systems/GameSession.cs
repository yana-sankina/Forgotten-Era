/// <summary>
/// Статический класс для передачи данных между сценами.
/// Хранит выбранный вид, флаг загрузки из сейва и реестр всех видов.
/// </summary>
public static class GameSession
{
    /// <summary>
    /// Выбранный вид динозавра. Задаётся при выборе в меню или при загрузке сейва.
    /// </summary>
    public static DinosaurSpeciesData SelectedSpecies;

    /// <summary>
    /// Флаг: загружаемся из сохранения (а не начинаем новую игру).
    /// DinosaurInitializer проверяет его при старте сцены.
    /// </summary>
    public static bool IsLoadingFromSave;

    /// <summary>
    /// Активный слот сохранения (0-2).
    /// </summary>
    public static int ActiveSaveSlot;

    /// <summary>
    /// Реестр всех видов. Заполняется при старте MainMenu из SpeciesSelectionUI.
    /// Используется SaveSystem для поиска SO по имени при загрузке.
    /// </summary>
    public static DinosaurSpeciesData[] AllSpecies;

    /// <summary>
    /// Найти SO вида по имени (для загрузки сейва).
    /// </summary>
    public static DinosaurSpeciesData FindSpeciesByName(string speciesName)
    {
        if (AllSpecies == null || string.IsNullOrEmpty(speciesName))
            return null;

        string lower = speciesName.ToLowerInvariant();
        for (int i = 0; i < AllSpecies.Length; i++)
        {
            if (AllSpecies[i] != null &&
                AllSpecies[i].speciesName.ToLowerInvariant() == lower)
            {
                return AllSpecies[i];
            }
        }

        return null;
    }
}
