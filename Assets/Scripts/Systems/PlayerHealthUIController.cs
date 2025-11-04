using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUIController : MonoBehaviour
{
    [Header("UI Елементи здоров'я")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("UI Елементи Смерті")]
    [SerializeField] private GameObject deathScreenPanel;
    
    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Subscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }
    
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Unsubscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }
    
    private void UpdateHealthUI(PlayerHealthChangedEvent e)
    {
        if (healthBar != null)
        {
            healthBar.value = (e.MaxHP > 0) ? ((float)e.CurrentHP / e.MaxHP) : 0;
        }
        
        if (healthText != null)
        {
            healthText.text = e.CurrentHP + " / " + e.MaxHP;
        }
    }
    
    private void ShowDeathScreen(PlayerDiedEvent e)
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
        }
    }
    
    private void OnPlayerRespawn(PlayerRespawnedEvent e)
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
        
    }
}