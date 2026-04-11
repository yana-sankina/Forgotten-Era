using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель выбора вида динозавра.
/// Показывает 3 карточки, при клике - записывает выбор в GameSession и загружает игровую сцену.
/// </summary>
public class SpeciesSelectionUI : MonoBehaviour
{
    [Header("Данные видов (SO ассеты)")]
    [SerializeField] private DinosaurSpeciesData tRexData;
    [FormerlySerializedAs("velociraptorData")]
    [SerializeField] private DinosaurSpeciesData dakotaraptorData;
    [FormerlySerializedAs("troodonData")]
    [SerializeField] private DinosaurSpeciesData pectinodonData;

    [Header("Кнопки-карточки")]
    [SerializeField] private Button tRexButton;
    [FormerlySerializedAs("velociraptorButton")]
    [SerializeField] private Button dakotaraptorButton;
    [FormerlySerializedAs("troodonButton")]
    [SerializeField] private Button pectinodonButton;

    [Header("Текст описания (опционально)")]
    [SerializeField] private TMP_Text tRexNameText;
    [SerializeField] private TMP_Text tRexDescText;
    [FormerlySerializedAs("velociraptorNameText")]
    [SerializeField] private TMP_Text dakotaraptorNameText;
    [FormerlySerializedAs("velociraptorDescText")]
    [SerializeField] private TMP_Text dakotaraptorDescText;
    [FormerlySerializedAs("troodonNameText")]
    [SerializeField] private TMP_Text pectinodonNameText;
    [FormerlySerializedAs("troodonDescText")]
    [SerializeField] private TMP_Text pectinodonDescText;

    [Header("Настройки")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        GameSession.AllSpecies = new[]
        {
            tRexData,
            dakotaraptorData,
            pectinodonData
        };
    }

    private void Start()
    {
        // Подключаем кнопки к выбору
        if (tRexButton != null)
            tRexButton.onClick.AddListener(() => SelectSpecies(tRexData));
        if (dakotaraptorButton != null)
            dakotaraptorButton.onClick.AddListener(() => SelectSpecies(dakotaraptorData));
        if (pectinodonButton != null)
            pectinodonButton.onClick.AddListener(() => SelectSpecies(pectinodonData));

        // Заполняем текст из SO (если поля назначены)
        FillCard(tRexNameText, tRexDescText, tRexData);
        FillCard(dakotaraptorNameText, dakotaraptorDescText, dakotaraptorData);
        FillCard(pectinodonNameText, pectinodonDescText, pectinodonData);
    }

    private void FillCard(TMP_Text nameText, TMP_Text descText, DinosaurSpeciesData data)
    {
        if (data == null) return;
        if (nameText != null) nameText.text = data.speciesName;
        if (descText != null) descText.text = data.description;
    }

    private void SelectSpecies(DinosaurSpeciesData species)
    {
        if (species == null)
        {
            Debug.LogError("DinosaurSpeciesData не назначен для этой кнопки!");
            return;
        }

        GameSession.SelectedSpecies = species;
        GameSession.IsLoadingFromSave = false;
        GameSession.ActiveSaveSlot = 0;
        SceneManager.LoadScene(gameSceneName);
    }
}
