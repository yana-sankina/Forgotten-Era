using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Мост между игровой логикой и Animator.
/// Подписывается на события, читает скорость, управляет параметрами аниматора.
/// Безопасно работает без Animator Controller — просто ничего не делает.
/// </summary>
public class DinosaurAnimator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private PlayerInput playerInput;
    private HashSet<int> existingParams = new HashSet<int>();

    // Кэшируем хеши параметров
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int TurnSpeedHash = Animator.StringToHash("TurnSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AbilityAttackHash = Animator.StringToHash("AbilityAttack");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private float maxSpeed = 6f;
    private bool wasGrounded = true;

    public void Init(Animator modelAnimator)
    {
        animator = modelAnimator;
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("DinosaurAnimator: Animator или Controller не найден — анимации отключены.");
            animator = null;
            return;
        }

        existingParams.Clear();
        foreach (var param in animator.parameters)
        {
            existingParams.Add(param.nameHash);
        }
    }

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerAttackEvent>(OnAttack);
        EventBroker.Subscribe<AbilityUsedEvent>(OnAbilityUsed);
        EventBroker.Subscribe<PlayerDiedEvent>(OnDied);
        EventBroker.Subscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerAttackEvent>(OnAttack);
        EventBroker.Unsubscribe<AbilityUsedEvent>(OnAbilityUsed);
        EventBroker.Unsubscribe<PlayerDiedEvent>(OnDied);
        EventBroker.Unsubscribe<PlayerStatsUpdatedEvent>(OnStatsUpdated);
    }

    private void Update()
    {
        if (animator == null || rb == null) return;

        // Горизонтальная скорость (0..1)
        Vector3 hVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float normalizedSpeed = hVel.magnitude / Mathf.Max(maxSpeed, 0.01f);
        SetFloat(SpeedHash, normalizedSpeed);

        // Бег (Shift зажат + двигается)
        bool isRunning = playerInput != null && playerInput.IsSprinting && normalizedSpeed > 0.1f;
        SetBool(IsRunningHash, isRunning);

        // Направление поворота (-1 = влево, 0 = прямо, 1 = вправо)
        if (playerInput != null)
        {
            SetFloat(TurnSpeedHash, playerInput.MovementInput.x);
        }

        // На земле
        bool grounded = Mathf.Abs(rb.linearVelocity.y) < 0.1f;
        SetBool(IsGroundedHash, grounded);

        // Прыжок: был на земле → оторвался вверх
        if (wasGrounded && !grounded && rb.linearVelocity.y > 1f)
        {
            SetTrigger(JumpHash);
        }
        wasGrounded = grounded;
    }

    private void OnStatsUpdated(PlayerStatsUpdatedEvent e)
    {
        maxSpeed = e.NewMoveSpeed;
    }

    private void OnAttack(PlayerAttackEvent e)
    {
        SetTrigger(AttackHash);
    }

    private void OnAbilityUsed(AbilityUsedEvent e)
    {
        SetTrigger(AbilityAttackHash);
    }

    private void OnDied(PlayerDiedEvent e)
    {
        SetTrigger(DieHash);
    }

    // === Безопасные обёртки ===

    private void SetFloat(int hash, float value)
    {
        if (animator != null && existingParams.Contains(hash))
            animator.SetFloat(hash, value);
    }

    private void SetBool(int hash, bool value)
    {
        if (animator != null && existingParams.Contains(hash))
            animator.SetBool(hash, value);
    }

    private void SetTrigger(int hash)
    {
        if (animator != null && existingParams.Contains(hash))
            animator.SetTrigger(hash);
    }
}
