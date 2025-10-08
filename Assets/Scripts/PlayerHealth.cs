using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 100;
    public int currentHP;
    public int healAmount = 3;
    public float healDelay = 20f;

    [Header("Controls / Keys")]
    public KeyCode damageKey = KeyCode.I;
    public KeyCode healKey = KeyCode.K;
    public KeyCode respawnKey = KeyCode.R;
    public Vector3 respawnPosition = Vector3.zero;
    public PlayerMovement playerController;

    private Coroutine healCoroutine;
    private bool isDead = false;

    void Start()
    {
        currentHP = maxHP;
        playerController.enabled = true;

    }

    void Update()
    {
        if (Input.GetKeyDown(damageKey))
        {
            TakeDamage(20);
        }

        if (Input.GetKeyDown(healKey))
        {
            Heal(20);
        }

        if (Input.GetKeyDown(respawnKey) && isDead)
        {
            Respawn();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log("HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            if (healCoroutine != null) StopCoroutine(healCoroutine);
            healCoroutine = StartCoroutine(HealAfterDelay());
        }
    }

    private IEnumerator HealAfterDelay()
    {
        yield return new WaitForSeconds(healDelay);
        while (currentHP < maxHP && !isDead)
        {
            currentHP += healAmount;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
            Debug.Log("HP восстановлено: " + currentHP);
            yield return new WaitForSeconds(healDelay);
        }
        healCoroutine = null;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log("HP: " + currentHP);
    }

    void Die()
    {
        Debug.Log("Игрок погиб!");
        isDead = true;

        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
            healCoroutine = null;
        }

        playerController.enabled = false;
    }

    public void Respawn()
    {
        Debug.Log("Игрок воскрес!");
        isDead = false;
        currentHP = maxHP;

        transform.position = respawnPosition;

        playerController.enabled = true;
    }
}
