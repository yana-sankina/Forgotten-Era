using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Меню паузы (ESC). Сохранение/загрузка по слотам.
/// Висит на Canvas в игровой сцене.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject slotsPanel;

    [Header("Кнопки главного меню паузы")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Слоты (3 кнопки)")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private TMP_Text[] slotTexts = new TMP_Text[3];
    [SerializeField] private bool updateSlotTexts = false;

    [Header("Настройки")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private PlayerInput playerInput;
    private bool isPaused = false;
    private bool isSaveMode = true; // true = сохраняем, false = загружаем

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (slotsPanel != null) slotsPanel.SetActive(false);

        FindPlayerInput();

        // Подключаем кнопки
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (saveButton != null) saveButton.onClick.AddListener(OpenSaveSlots);
        if (loadButton != null) loadButton.onClick.AddListener(OpenLoadSlots);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FindPlayerInput();

            if (slotsPanel != null && slotsPanel.activeSelf)
            {
                // Если открыта панель слотов — закрываем её
                CloseSlotsPanel();
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void FindPlayerInput()
    {
        if (playerInput != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerInput = player.GetComponent<PlayerInput>();

        if (playerInput == null)
            playerInput = UnityEngine.Object.FindAnyObjectByType<PlayerInput>();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerInput != null) playerInput.SetMenuLock(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (slotsPanel != null) slotsPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerInput != null) playerInput.SetMenuLock(false);
    }

    private void OpenSaveSlots()
    {
        isSaveMode = true;
        ShowSlotsPanel();
    }

    private void OpenLoadSlots()
    {
        isSaveMode = false;
        ShowSlotsPanel();
    }

    private void ShowSlotsPanel()
    {
        if (slotsPanel != null) slotsPanel.SetActive(true);
        RefreshSlotTexts();
    }

    private void CloseSlotsPanel()
    {
        if (slotsPanel != null) slotsPanel.SetActive(false);
    }

    private void RefreshSlotTexts()
    {
        if (!updateSlotTexts)
            return;

        for (int i = 0; i < SaveSystem.MAX_SLOTS; i++)
        {
            if (i >= slotTexts.Length || slotTexts[i] == null) continue;

            if (SaveSystem.HasSave(i))
            {
                SaveData info = SaveSystem.GetSaveInfo(i);
                if (info != null)
                {
                    slotTexts[i].text = string.Format("Слот {0}: {1} | Ур.{2} | {3}",
                        i + 1, info.speciesName, info.currentLevel, info.saveTimestamp);
                }
                else
                {
                    slotTexts[i].text = "Слот " + (i + 1) + ": [повреждён]";
                }
            }
            else
            {
                slotTexts[i].text = "Слот " + (i + 1) + ": Пустой";
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (isSaveMode)
        {
            // Сохраняем
            if (SaveSystem.Save(slotIndex))
            {
                GameSession.ActiveSaveSlot = slotIndex;
                RefreshSlotTexts();
            }
        }
        else
        {
            // Загружаем
            if (!SaveSystem.HasSave(slotIndex))
            {
                Debug.LogWarning("Слот " + slotIndex + " пуст!");
                return;
            }

            SaveData data = SaveSystem.Load(slotIndex);
            if (data == null) return;

            // Ищем SO вида по имени
            DinosaurSpeciesData species = GameSession.FindSpeciesByName(data.speciesName);
            if (species == null)
            {
                Debug.LogError("Вид '" + data.speciesName + "' не найден в реестре!");
                return;
            }

            Time.timeScale = 1f;
            GameSession.SelectedSpecies = species;
            GameSession.IsLoadingFromSave = true;
            GameSession.ActiveSaveSlot = slotIndex;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (playerInput != null) playerInput.SetMenuLock(false);
        SaveSystem.Save(GameSession.ActiveSaveSlot);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
