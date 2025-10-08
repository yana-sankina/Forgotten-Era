using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUIController : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthBar;
    public TMP_Text healthText;

    private PlayerHealth playerHealth;
    private int lastHP;

    void Start()
    {
        playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            lastHP = playerHealth.currentHP;

        UpdateUI();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.currentHP != lastHP)
        {
            UpdateUI();
            lastHP = playerHealth.currentHP;
        }
    }

    private void UpdateUI()
    {
        if (healthBar != null)
            healthBar.value = (float)playerHealth.currentHP / playerHealth.maxHP;

        if (healthText != null)
            healthText.text = playerHealth.currentHP + " / " + playerHealth.maxHP;
    }
}
