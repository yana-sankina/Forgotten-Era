using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Синхронизирует Animator врага с его NavMeshAgent и EnemyAI.
/// Передаёт параметры: Speed (0..1), Attack (trigger).
/// Безопасно пропускает параметры которых нет в контроллере.
/// </summary>
public class EnemyAnimatorSync : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyAI enemyAI;
    private bool hasValidAnimator;
    private bool wasAttacking;
    private HashSet<int> existingParams = new HashSet<int>();

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyAI = GetComponent<EnemyAI>();

        hasValidAnimator = (animator != null && animator.runtimeAnimatorController != null);

        if (hasValidAnimator)
        {
            existingParams.Clear();
            foreach (var param in animator.parameters)
            {
                existingParams.Add(param.nameHash);
            }
        }
    }

    private void Update()
    {
        if (!hasValidAnimator || agent == null)
            return;

        float normalizedSpeed = 0f;
        if (agent.speed > 0.01f)
            normalizedSpeed = agent.velocity.magnitude / agent.speed;

        if (existingParams.Contains(SpeedHash))
            animator.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);

        // Триггер Attack — срабатывает один раз при начале атаки
        if (enemyAI != null && existingParams.Contains(AttackHash))
        {
            bool attacking = enemyAI.IsAttacking;
            if (attacking && !wasAttacking)
            {
                animator.SetTrigger(AttackHash);
            }
            wasAttacking = attacking;
        }
    }
}
