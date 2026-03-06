using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Меню прокачки характеристик.
/// Открывается на Tab, показывает доступные очки и кнопки +HP/+ATK/+SPD.
/// Кнопки подключать через инспектор или будут найдены по имени.
/// </summary>
public class UpgradeMenuUI : MonoBehaviour
{
    [Header("Панель")]
    [SerializeField] private GameObject upgradePanel;

    [Header("Тексты")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text atkValueText;
    [SerializeField] private TMP_Text spdValueText;

    [Header("Кнопки")]
    [SerializeField] private Button hpButton;
    [SerializeField] private Button atkButton;
    [SerializeField] private Button spdButton;

    [Header("XP Bar (опционально)")]
    [SerializeField] private Slider xpBar;

    private StatUpgradeSystem upgradeSystem;
    private PlayerInput playerInput;
    private bool isOpen = false;

    // Кэшируем текущие значения для отображения
    private int currentLevel = 1;
    private int currentXP = 0;
    private int xpToNext = 100;
    private int currentMaxHP = 0;
    private int currentATK = 0;
    private float currentSPD = 0f;

    private void Awake()
    {
        upgradeSystem = FindFirstObjectByType<StatUpgradeSystem>();
        playerInput = FindFirstObjectByType<PlayerInput>();

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        // Блокируем XP бар от перетаскивания, но оставляем непрозрачным
        if (xpBar != null)
        {
            xpBar.interactable = false;
            var colors = xpBar.colors;
            colors.disabledColor = Color.white;
            xpBar.colors = colors;
        }
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<ExperienceChangedEvent>(OnXPChanged);
        EventBroker.Subscribe<StatPointsChangedEvent>(OnPointsChanged);
        EventBroker.Subscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);

        // Подключаем кнопки
        if (hpButton != null) hpButton.onClick.AddListener(OnHPClicked);
        if (atkButton != null) atkButton.onClick.AddListener(OnATKClicked);
        if (spdButton != null) spdButton.onClick.AddListener(OnSPDClicked);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<ExperienceChangedEvent>(OnXPChanged);
        EventBroker.Unsubscribe<StatPointsChangedEvent>(OnPointsChanged);
        EventBroker.Unsubscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);

        if (hpButton != null) hpButton.onClick.RemoveListener(OnHPClicked);
        if (atkButton != null) atkButton.onClick.RemoveListener(OnATKClicked);
        if (spdButton != null) spdButton.onClick.RemoveListener(OnSPDClicked);
    }

    private void Update()
    {
        if (playerInput != null && playerInput.UpgradeMenuInput)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;

        if (upgradePanel != null)
            upgradePanel.SetActive(isOpen);

        // Курсор: показать при открытии, скрыть при закрытии
        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (isOpen)
            UpdateAllTexts();
    }

    private void OnXPChanged(ExperienceChangedEvent e)
    {
        currentLevel = e.Level;
        currentXP = e.CurrentXP;
        xpToNext = e.XPToNextLevel;

        if (isOpen) UpdateAllTexts();
    }

    private void OnPointsChanged(StatPointsChangedEvent e)
    {
        if (isOpen) UpdateAllTexts();
    }

    private void OnStatsUpdated(PlayerStatsUpdatedEvent e)
    {
        currentMaxHP = e.NewMaxHP;
        currentATK = e.NewAttackDamage;
        currentSPD = e.NewMoveSpeed;

        if (isOpen) UpdateAllTexts();
    }

    private void OnHPClicked()
    {
        if (upgradeSystem != null) upgradeSystem.UpgradeHP();
    }

    private void OnATKClicked()
    {
        if (upgradeSystem != null) upgradeSystem.UpgradeATK();
    }

    private void OnSPDClicked()
    {
        if (upgradeSystem != null) upgradeSystem.UpgradeSPD();
    }

    private void UpdateAllTexts()
    {
        int points = (upgradeSystem != null) ? upgradeSystem.AvailablePoints : 0;

        if (levelText != null) levelText.text = "Уровень " + currentLevel;
        if (xpText != null) xpText.text = currentXP + " / " + xpToNext + " XP";
        if (pointsText != null) pointsText.text = "Очки: " + points;
        if (hpValueText != null) hpValueText.text = "HP: " + currentMaxHP;
        if (atkValueText != null) atkValueText.text = "ATK: " + currentATK;
        if (spdValueText != null) spdValueText.text = "SPD: " + currentSPD.ToString("F1");

        if (xpBar != null)
            xpBar.value = (xpToNext > 0) ? (float)currentXP / xpToNext : 0;

        // Отключаем кнопки если нет очков
        bool hasPoints = points > 0;
        if (hpButton != null) hpButton.interactable = hasPoints;
        if (atkButton != null) atkButton.interactable = hasPoints;
        if (spdButton != null) spdButton.interactable = hasPoints;
    }
}
