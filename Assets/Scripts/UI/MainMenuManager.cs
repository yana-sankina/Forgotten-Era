using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет главным меню. Висит на объекте в сцене MainMenu.
/// Кнопки подключаются через инспектор (OnClick).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject loadSlotsPanel;

    [Header("Кнопки")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button[] loadSlotButtons = new Button[3];
    [SerializeField] private TMP_Text[] loadSlotTexts = new TMP_Text[3];
    [SerializeField] private bool updateLoadSlotTexts = false;

    [Header("Настройки")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Start()
    {
        // При старте: показываем меню, скрываем остальное
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (loadSlotsPanel != null) loadSlotsPanel.SetActive(false);

        // Убеждаемся что курсор видим и разблокирован
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Кнопка Продолжить — активна только если есть хотя бы один сейв
        if (continueButton != null)
        {
            continueButton.interactable = SaveSystem.HasAnySave();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        // Кнопки слотов загрузки
        for (int i = 0; i < loadSlotButtons.Length; i++)
        {
            int slotIndex = i;
            if (loadSlotButtons[i] != null)
                loadSlotButtons[i].onClick.AddListener(() => OnLoadSlotSelected(slotIndex));
        }
    }

    /// <summary>
    /// Кнопка «Новая игра» → показать панель выбора вида.
    /// </summary>
    public void OnNewGameClicked()
    {
        GameSession.IsLoadingFromSave = false;
        GameSession.ActiveSaveSlot = 0;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (loadSlotsPanel != null) loadSlotsPanel.SetActive(false);
    }

    /// <summary>
    /// Кнопка «Продолжить» → показать панель слотов загрузки.
    /// </summary>
    public void OnContinueClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (loadSlotsPanel != null) loadSlotsPanel.SetActive(true);
        RefreshLoadSlots();
    }

    /// <summary>
    /// Выбран слот загрузки.
    /// </summary>
    private void OnLoadSlotSelected(int slotIndex)
    {
        if (!SaveSystem.HasSave(slotIndex))
        {
            Debug.LogWarning("Слот " + slotIndex + " пуст!");
            return;
        }

        SaveData data = SaveSystem.Load(slotIndex);
        if (data == null) return;

        DinosaurSpeciesData species = GameSession.FindSpeciesByName(data.speciesName);
        if (species == null)
        {
            Debug.LogError("Вид '" + data.speciesName + "' не найден в реестре AllSpecies!");
            return;
        }

        GameSession.SelectedSpecies = species;
        GameSession.IsLoadingFromSave = true;
        GameSession.ActiveSaveSlot = slotIndex;
        SceneManager.LoadScene(gameSceneName);
    }

    private void RefreshLoadSlots()
    {
        for (int i = 0; i < SaveSystem.MAX_SLOTS; i++)
        {
            if (updateLoadSlotTexts && i < loadSlotTexts.Length && loadSlotTexts[i] != null)
            {
                if (SaveSystem.HasSave(i))
                {
                    SaveData info = SaveSystem.GetSaveInfo(i);
                    if (info != null)
                    {
                        loadSlotTexts[i].text = string.Format("Слот {0}: {1} | Ур.{2} | {3}",
                            i + 1, info.speciesName, info.currentLevel, info.saveTimestamp);
                    }
                    else
                    {
                        loadSlotTexts[i].text = "Слот " + (i + 1) + ": [повреждён]";
                    }
                }
                else
                {
                    loadSlotTexts[i].text = "Слот " + (i + 1) + ": Пустой";
                }
            }

            if (i < loadSlotButtons.Length && loadSlotButtons[i] != null)
                loadSlotButtons[i].interactable = SaveSystem.HasSave(i);
        }
    }

    /// <summary>
    /// Кнопка «Назад» → вернуться в главное меню.
    /// </summary>
    public void OnBackClicked()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (loadSlotsPanel != null) loadSlotsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Кнопка «Выход» → закрыть игру.
    /// </summary>
    public void OnQuitClicked()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
