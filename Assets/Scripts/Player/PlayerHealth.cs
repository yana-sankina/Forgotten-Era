using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP;
    public int currentHP;

    private bool isDead = false;

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }
    
    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }
    
    private void OnStatsUpdated(PlayerStatsUpdatedEvent e)
    {
        int oldMaxHP = maxHP;
        maxHP = e.NewMaxHP;
        
        int hpGained = maxHP - oldMaxHP;
        if (!isDead && hpGained > 0 && currentHP > 0)
        {
            Heal(hpGained);
        }
        else if (currentHP == 0 && !isDead)
        {
            currentHP = maxHP;
     
            EventBroker.Publish(new PlayerHealthChangedEvent { CurrentHP = currentHP, MaxHP = maxHP });
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        EventBroker.Publish(new PlayerHealthChangedEvent { CurrentHP = currentHP, MaxHP = maxHP });

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        EventBroker.Publish(new PlayerHealthChangedEvent { CurrentHP = currentHP, MaxHP = maxHP });
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        EventBroker.Publish(new PlayerDiedEvent());
    }

    public void Respawn(Vector3 position)
    {
        isDead = false;
        currentHP = maxHP;

        EventBroker.Publish(new PlayerHealthChangedEvent { CurrentHP = currentHP, MaxHP = maxHP });
        EventBroker.Publish(new PlayerRespawnedEvent { Position = position });

        transform.position = position;
    }
}
