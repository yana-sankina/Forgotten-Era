using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель выбора вида динозавра.
/// Показывает 3 карточки, при клике — записывает выбор в GameSession и загружает игровую сцену.
/// </summary>
public class SpeciesSelectionUI : MonoBehaviour
{
    [Header("Данные видов (SO ассеты)")]
    [SerializeField] private DinosaurSpeciesData tRexData;
    [SerializeField] private DinosaurSpeciesData velociraptorData;
    [SerializeField] private DinosaurSpeciesData troodonData;

    [Header("Кнопки-карточки")]
    [SerializeField] private Button tRexButton;
    [SerializeField] private Button velociraptorButton;
    [SerializeField] private Button troodonButton;

    [Header("Текст описания (опционально)")]
    [SerializeField] private TMP_Text tRexNameText;
    [SerializeField] private TMP_Text tRexDescText;
    [SerializeField] private TMP_Text velociraptorNameText;
    [SerializeField] private TMP_Text velociraptorDescText;
    [SerializeField] private TMP_Text troodonNameText;
    [SerializeField] private TMP_Text troodonDescText;

    [Header("Настройки")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Start()
    {
        // Подключаем кнопки к выбору
        if (tRexButton != null)
            tRexButton.onClick.AddListener(() => SelectSpecies(tRexData));
        if (velociraptorButton != null)
            velociraptorButton.onClick.AddListener(() => SelectSpecies(velociraptorData));
        if (troodonButton != null)
            troodonButton.onClick.AddListener(() => SelectSpecies(troodonData));

        // Заполняем текст из SO (если поля назначены)
        FillCard(tRexNameText, tRexDescText, tRexData);
        FillCard(velociraptorNameText, velociraptorDescText, velociraptorData);
        FillCard(troodonNameText, troodonDescText, troodonData);
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
        Debug.Log("Выбран вид: " + species.speciesName + ". Загрузка игры...");
        SceneManager.LoadScene(gameSceneName);
    }
}
