/// <summary>
/// Статический класс для передачи данных между сценами.
/// Хранит выбранный вид, флаг загрузки из сейва и реестр всех видов.
/// </summary>
public static class GameSession
{
    public static DinosaurSpeciesData SelectedSpecies;
    
    public static bool IsLoadingFromSave;
    
    public static int ActiveSaveSlot;
    
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
