// Находится на Игроке
using UnityEngine;
using System.Collections;

public class PlayerStamina : MonoBehaviour
{
    [Header("Налаштування")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDecayRate = 10f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float regenDelay = 2f;
    public bool CanSprint { get; private set; } = true; 

    private float currentStamina;
    private PlayerInput playerInput;
    private Coroutine regenCoroutine;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        currentStamina = maxStamina;
        PublishStaminaEvent();
    }

    private void Update()
    {
        bool isSprinting = playerInput.IsSprinting && CanSprint;

        if (isSprinting && playerInput.MovementInput.y > 0)
        {
            HandleConsumption();
        }
        else
        {
            HandleRegeneration();
        }
    }

    private void HandleConsumption()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        currentStamina -= staminaDecayRate * Time.deltaTime;
        if (currentStamina <= 0)
        {
            currentStamina = 0;
            CanSprint = false;
        }
        
        PublishStaminaEvent();
    }

    private void HandleRegeneration()
    {
        if (currentStamina < maxStamina && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(RegenDelayCoroutine());
        }
    }

    private IEnumerator RegenDelayCoroutine()
    {
        yield return new WaitForSeconds(regenDelay);
        
        while (currentStamina < maxStamina && !playerInput.IsSprinting)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            
            if (currentStamina > 0) 
            {
                CanSprint = true;
            }

            PublishStaminaEvent();
            yield return null;
        }
        
        regenCoroutine = null;
    }

    private void PublishStaminaEvent()
    {
        EventBroker.Publish(new PlayerStaminaChangedEvent 
        { 
            Current = currentStamina, 
            Max = maxStamina 
        });
    }
}
