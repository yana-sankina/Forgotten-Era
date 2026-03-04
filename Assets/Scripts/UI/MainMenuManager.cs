using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет главным меню. Висит на объекте в сцене MainMenu.
/// Кнопки подключаются через инспектор (OnClick).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject selectionPanel;

    private void Start()
    {
        // При старте: показываем меню, скрываем выбор
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (selectionPanel != null) selectionPanel.SetActive(false);

        // Убеждаемся что курсор видим и разблокирован
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Кнопка «Новая игра» → показать панель выбора вида.
    /// </summary>
    public void OnNewGameClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);
    }

    /// <summary>
    /// Кнопка «Назад» на панели выбора → вернуться в главное меню.
    /// </summary>
    public void OnBackClicked()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Кнопка «Выход» → закрыть игру.
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
