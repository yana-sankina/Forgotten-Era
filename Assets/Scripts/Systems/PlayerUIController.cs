using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIController : MonoBehaviour
{
    [Header("UI Елементи здоров'я")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("UI Елементы Росту")]
    [SerializeField] private Slider growthBar;
    
    [Header("UI Елементи Смерті")]
    [SerializeField] private GameObject deathScreenPanel;
    
    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Subscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
        EventBroker.Subscribe<PlayerGrowthChangedEvent>(UpdateGrowthUI);
    }
    
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Unsubscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
        EventBroker.Unsubscribe<PlayerGrowthChangedEvent>(UpdateGrowthUI);
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
    
    private void UpdateGrowthUI(PlayerGrowthChangedEvent e)
    {
        if (growthBar == null) return;
        
        growthBar.value = (e.MaxGrowth > 0) ? (e.CurrentGrowth / e.MaxGrowth) : 0;
    }
}