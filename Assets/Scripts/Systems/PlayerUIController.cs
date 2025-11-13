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
    
    [Header("UI Елементи Потреб")]
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Slider thirstBar;
    
    [Header("UI Елементи Смерті")]
    [SerializeField] private GameObject deathScreenPanel;
    
    [Header("UI Елементи Витривалості")]
    [SerializeField] private Slider staminaBar;
    
    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Subscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
        EventBroker.Subscribe<PlayerGrowthChangedEvent>(UpdateGrowthUI);
        EventBroker.Subscribe<PlayerHungerChangedEvent>(UpdateHungerUI);
        EventBroker.Subscribe<PlayerThirstChangedEvent>(UpdateThirstUI);
        EventBroker.Subscribe<PlayerStaminaChangedEvent>(UpdateStaminaUI);
    }
    
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerHealthChangedEvent>(UpdateHealthUI);
        EventBroker.Unsubscribe<PlayerDiedEvent>(ShowDeathScreen);
        EventBroker.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
        EventBroker.Unsubscribe<PlayerGrowthChangedEvent>(UpdateGrowthUI);
        EventBroker.Unsubscribe<PlayerHungerChangedEvent>(UpdateHungerUI);
        EventBroker.Unsubscribe<PlayerThirstChangedEvent>(UpdateThirstUI);
        EventBroker.Unsubscribe<PlayerStaminaChangedEvent>(UpdateStaminaUI);
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
    
    private void UpdateHungerUI(PlayerHungerChangedEvent e)
    {
        if (hungerBar != null)
        {
            hungerBar.value = (e.Max > 0) ? (e.Current / e.Max) : 0;
        }
    }
    
    private void UpdateThirstUI(PlayerThirstChangedEvent e)
    {
        if (thirstBar != null)
        {
            thirstBar.value = (e.Max > 0) ? (e.Current / e.Max) : 0;
        }
    }
    
    private void UpdateStaminaUI(PlayerStaminaChangedEvent e)
    {
        if (staminaBar != null)
        {
            staminaBar.value = (e.Max > 0) ? (e.Current / e.Max) : 0;
        }
    }
}