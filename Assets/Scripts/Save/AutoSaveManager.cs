using UnityEngine;
using System.Collections;

/// <summary>
/// Автосохранение каждые N секунд. Висит на объекте в игровой сцене.
/// </summary>
public class AutoSaveManager : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Интервал автосохранения в секундах (300 = 5 минут)")]
    [SerializeField] private float autoSaveInterval = 300f;

    [Tooltip("В какой слот автосохранять (0-2)")]
    [SerializeField] private int autoSaveSlot = 0;

    [Tooltip("Показывать надпись в консоли при автосейве")]
    [SerializeField] private bool logAutoSaves = true;

    private void Start()
    {
        StartCoroutine(AutoSaveLoop());
    }

    private IEnumerator AutoSaveLoop()
    {
        // Ждём пока игрок полностью инициализируется
        yield return new WaitForSeconds(5f);

        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            if (Time.timeScale > 0f) // Не сохраняем на паузе
            {
                int slot = GetCurrentAutoSaveSlot();
                SaveSystem.Save(slot);
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveSystem.Save(GetCurrentAutoSaveSlot());
    }

    private int GetCurrentAutoSaveSlot()
    {
        if (GameSession.ActiveSaveSlot >= 0 && GameSession.ActiveSaveSlot < SaveSystem.MAX_SLOTS)
            return GameSession.ActiveSaveSlot;

        return Mathf.Clamp(autoSaveSlot, 0, SaveSystem.MAX_SLOTS - 1);
    }
}
