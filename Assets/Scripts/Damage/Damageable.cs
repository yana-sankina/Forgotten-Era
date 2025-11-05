using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    private int currentHP;

    public bool IsDead { get; private set; } = false;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHP -= amount;
        Debug.Log(gameObject.name + " получил " + amount + " урона. Осталось ХП: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;

        IsDead = true;

    }
}