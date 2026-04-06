using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Damageable))]
public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead
    }

    [Header("Performance")]
    [Tooltip("Как часто обновлять путь в Chase (сек).")]
    [SerializeField] private float repathInterval = 0.25f;

    [Header("Stuck Recovery")]
    [SerializeField] private float stuckSpeedThreshold = 0.05f;
    [SerializeField] private float stuckTimeSeconds = 1.5f;

    [Header("Target Lookup")]
    [Tooltip("Как часто пытаться заново найти игрока по тегу, если ссылка потеряна.")]
    [SerializeField] private float playerLookupInterval = 1f;

    private NavMeshAgent agent;
    private Damageable damageable;
    private EnemyHitbox[] enemyHitboxes;
    private Rigidbody rb;

    private EnemyRuntimeProfile profile;
    private Transform player;

    private State state = State.Patrol;
    private Vector3 spawnOrigin;

    private float repathTimer;
    private float stuckTimer;
    private float patrolPointTimer;
    private float lostTargetTimer;
    private float attackWindowTimer;
    private float attackCooldownTimer;
    private float attackTurnLockTimer;
    private float currentAttackTurnSpeed;
    private float fleeTimer;
    private float playerLookupTimer;

    private Vector3 currentMoveTarget;
    private bool hasMoveTarget;
    private bool isAttacking;

    /// <summary>true когда хитбокс активен (для анимации атаки)</summary>
    public bool IsAttacking => isAttacking;

    public void Configure(EnemyRuntimeProfile runtimeProfile, Transform playerTransform, EnemyHitbox[] hitboxes)
    {
        profile = runtimeProfile;
        if (playerTransform != null)
            player = playerTransform;
        enemyHitboxes = hitboxes;

        SetHitboxActive(false);
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody>();

        spawnOrigin = transform.position;

        if (rb != null)
            rb.isKinematic = true;
    }

    private void Start()
    {
        if (enemyHitboxes == null || enemyHitboxes.Length == 0)
            enemyHitboxes = GetComponentsInChildren<EnemyHitbox>(true);
        SetHitboxActive(false);

        EnsurePlayerReferenceImmediate();

        EnsureOnNavMesh();
        spawnOrigin = transform.position;
        EnterPatrol();
    }

    private void Update()
    {
        if (damageable == null)
            return;

        EnsurePlayerReference();

        if (damageable.IsDead || damageable.IsCorpse)
        {
            EnterDead();
            return;
        }

        if (damageable.IsStunned)
        {
            SetHitboxActive(false);
            if (agent != null)
                agent.isStopped = true;
            return;
        }

        float hpPct = GetHpPercent();

        if (state != State.Flee && hpPct < GetProfile().fleeHpThreshold)
        {
            EnterFlee();
        }

        switch (state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.Attack:
                TickAttack();
                break;
            case State.Flee:
                TickFlee();
                break;
            case State.Dead:
                // nothing
                break;
        }
    }

    private void TickPatrol()
    {
        var cfg = GetProfile();
        SetAgentSpeed(cfg.patrolSpeed);
        if (agent != null)
            agent.isStopped = false;

        if (player != null && GetFlatDistanceToPlayer() <= cfg.detectionRadius)
        {
            EnterChase();
            return;
        }

        patrolPointTimer += Time.deltaTime;

        if (!hasMoveTarget || HasReachedDestination() || patrolPointTimer >= cfg.patrolPointTimeout || IsStuck())
        {
            if (TryPickRandomNavMeshPoint(spawnOrigin, cfg.patrolRadius, out Vector3 point))
            {
                MoveTo(point);
                patrolPointTimer = 0f;
            }
            else
            {
                hasMoveTarget = false;
                if (agent != null)
                    agent.isStopped = true;
            }
        }
    }

    private void TickChase()
    {
        var cfg = GetProfile();
        SetAgentSpeed(cfg.chaseSpeed);
        if (agent != null)
            agent.isStopped = false;

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        float dist = GetFlatDistanceToPlayer();
        if (dist <= cfg.attackRange)
        {
            EnterAttack();
            return;
        }

        if (dist > cfg.loseTargetRadius)
            lostTargetTimer += Time.deltaTime;
        else
            lostTargetTimer = 0f;

        if (lostTargetTimer >= cfg.loseTargetDelaySeconds)
        {
            EnterPatrol();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f || IsStuck())
        {
            repathTimer = Mathf.Max(0.05f, repathInterval);
            MoveTo(player.position);
        }
    }

    private void TickAttack()
    {
        var cfg = GetProfile();

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        float dist = GetFlatDistanceToPlayer();
        if (dist > cfg.attackRange + cfg.attackRangeHysteresis)
        {
            EnterChase();
            return;
        }

        if (agent != null)
        {
            agent.isStopped = true;
        }

        float lockMul = 1f;
        if (attackTurnLockTimer > 0f)
        {
            attackTurnLockTimer -= Time.deltaTime;
            lockMul = Mathf.Clamp01(cfg.recoveryTurnSpeedMultiplier);
        }

        float accel = Mathf.Max(0f, cfg.attackTurnAcceleration);
        float baseMul = Mathf.Max(0f, cfg.attackTurnSpeedMultiplier);
        float targetTurnSpeed = Mathf.Max(0f, cfg.attackTurnSpeed * baseMul * lockMul);
        currentAttackTurnSpeed = Mathf.MoveTowards(currentAttackTurnSpeed, targetTurnSpeed, accel * Time.deltaTime);
        FaceTarget(player.position, currentAttackTurnSpeed);

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (attackWindowTimer > 0f)
        {
            attackWindowTimer -= Time.deltaTime;
            if (attackWindowTimer <= 0f)
            {
                SetHitboxActive(false);
            }
        }

        if (attackCooldownTimer <= 0f && attackWindowTimer <= 0f)
        {
            // Start a new swing
            SetHitboxActive(true);
            float attackWindow = Mathf.Max(cfg.attackDuration, cfg.minAttackWindowDuration);
            attackWindowTimer = attackWindow;
            attackCooldownTimer = cfg.attackCooldown;

            // Не блокируем поворот на весь цикл: оставляем гарантированное окно,
            // когда враг может довернуться к игроку перед следующим свингом.
            float requestedLock = Mathf.Max(0f, attackWindow + cfg.postAttackTurnDelay);
            float maxAllowedLock = Mathf.Max(0f, cfg.attackCooldown - Mathf.Max(0f, cfg.minFreeTurnTimeBeforeNextSwing));
            attackTurnLockTimer = Mathf.Min(requestedLock, maxAllowedLock);

            LogDebug("Attack swing started");
        }
    }

    private void TickFlee()
    {
        var cfg = GetProfile();
        SetAgentSpeed(cfg.fleeSpeed);
        if (agent != null)
            agent.isStopped = false;

        fleeTimer -= Time.deltaTime;
        if (fleeTimer <= 0f)
        {
            float hpPct = GetHpPercent();
            if (player != null &&
                GetFlatDistanceToPlayer() <= cfg.detectionRadius &&
                hpPct >= cfg.reengageHpThreshold)
            {
                EnterChase();
            }
            else
            {
                EnterPatrol();
            }
            return;
        }

        if (!hasMoveTarget || HasReachedDestination() || IsStuck())
        {
            Vector3 fleeCenter = (player != null) ? player.position : transform.position;
            Vector3 away = (transform.position - fleeCenter);
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = Random.insideUnitSphere;
            away.Normalize();

            float fleeDistance = Mathf.Max(cfg.fleeDistance, cfg.attackRange * 3f);
            Vector3 candidate = transform.position + away * fleeDistance;
            candidate.y = transform.position.y;

            if (TryPickRandomNavMeshPoint(candidate, cfg.fleePickRadius, out Vector3 point))
            {
                MoveTo(point);
            }
            else if (TryPickRandomNavMeshPoint(transform.position, cfg.fleePickRadius, out Vector3 fallback))
            {
                MoveTo(fallback);
            }
            else
            {
                hasMoveTarget = false;
                agent.isStopped = true;
            }
        }
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        lostTargetTimer = 0f;
        patrolPointTimer = 999f;
        SetHitboxActive(false);
        if (agent != null)
            agent.isStopped = false;
        hasMoveTarget = false;
        LogDebug("State -> Patrol");
    }

    private void EnterChase()
    {
        if (state == State.Chase)
            return;
        state = State.Chase;
        repathTimer = 0f;
        SetHitboxActive(false);
        if (agent != null)
            agent.isStopped = false;
        hasMoveTarget = false;
        LogDebug("State -> Chase");
    }

    private void EnterAttack()
    {
        if (state == State.Attack)
            return;
        state = State.Attack;
        attackWindowTimer = 0f;
        attackCooldownTimer = 0f;
        attackTurnLockTimer = 0f;
        currentAttackTurnSpeed = 0f;
        SetHitboxActive(false);
        if (agent != null)
            agent.isStopped = true;
        LogDebug("State -> Attack");
    }

    private void EnterFlee()
    {
        if (state == State.Flee)
            return;

        state = State.Flee;
        fleeTimer = GetProfile().fleeDuration;
        SetHitboxActive(false);
        if (agent != null)
            agent.isStopped = false;
        hasMoveTarget = false;
        LogDebug("State -> Flee");
    }

    private void EnterDead()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;
        SetHitboxActive(false);
        if (agent != null)
            agent.isStopped = true;
        LogDebug("State -> Dead");
    }

    private EnemyRuntimeProfile GetProfile()
    {
        // Safe defaults if profile isn't assigned yet.
        if (profile != null)
            return profile;

        return EnemyRuntimeProfileDefaults.Instance;
    }

    private float GetHpPercent()
    {
        if (damageable == null) return 1f;
        int maxHp = damageable.MaxHP;
        if (maxHp <= 0) return 1f;
        return Mathf.Clamp01(damageable.CurrentHP / (float)maxHp);
    }

    private void EnsureOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void SetAgentSpeed(float speed)
    {
        if (agent == null) return;
        agent.speed = Mathf.Max(0f, speed);
    }

    private void MoveTo(Vector3 destination)
    {
        if (agent == null) return;
        agent.isStopped = false;
        agent.SetDestination(destination);
        currentMoveTarget = destination;
        hasMoveTarget = true;
    }

    private bool HasReachedDestination()
    {
        if (agent == null) return true;
        if (agent.pathPending) return false;
        if (agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.2f)) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    private bool IsStuck()
    {
        if (agent == null) return false;
        if (!agent.hasPath || agent.pathPending) return false;

        bool tryingToMove = agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.5f);
        if (!tryingToMove)
        {
            stuckTimer = 0f;
            return false;
        }

        if (agent.velocity.magnitude < stuckSpeedThreshold)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        return stuckTimer >= stuckTimeSeconds;
    }

    private bool TryPickRandomNavMeshPoint(Vector3 center, float radius, out Vector3 point)
    {
        float r = Mathf.Max(0.1f, radius);
        for (int i = 0; i < 8; i++)
        {
            Vector3 random = center + Random.insideUnitSphere * r;
            random.y = center.y;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, r, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = default;
        return false;
    }

    private void FaceTarget(Vector3 targetPosition, float turnSpeed)
    {
        Vector3 dir = (targetPosition - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Max(0f, turnSpeed) * Time.deltaTime);
    }

    private void SetHitboxActive(bool active)
    {
        isAttacking = active;

        if (enemyHitboxes == null || enemyHitboxes.Length == 0)
            return;

        for (int i = 0; i < enemyHitboxes.Length; i++)
        {
            EnemyHitbox hitbox = enemyHitboxes[i];
            if (hitbox == null) continue;

            GameObject go = hitbox.gameObject;
            if (go == gameObject)
            {
                // Если hitbox висит на корне врага — не трогаем активность всего объекта.
                if (hitbox.enabled != active)
                    hitbox.enabled = active;
                continue;
            }

            if (go.activeSelf != active)
                go.SetActive(active);
        }
    }

    private void EnsurePlayerReference()
    {
        if (player != null)
            return;

        playerLookupTimer -= Time.deltaTime;
        if (playerLookupTimer > 0f)
            return;

        playerLookupTimer = Mathf.Max(0.2f, playerLookupInterval);
        EnsurePlayerReferenceImmediate();
    }

    private void EnsurePlayerReferenceImmediate()
    {
        if (player != null)
            return;

#if UNITY_2023_1_OR_NEWER
        PlayerHealth foundHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
#else
        PlayerHealth foundHealth = FindObjectOfType<PlayerHealth>();
#endif
        if (foundHealth != null)
        {
            player = foundHealth.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private float GetFlatDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void LogDebug(string message)
    {
        EnemyRuntimeProfile cfg = GetProfile();
        if (cfg == null || !cfg.enableDebugLogs)
            return;

        Debug.Log("[EnemyAI] " + gameObject.name + ": " + message, this);
    }
}

/// <summary>
/// Fallback значения, если враг заспавнен без EnemyRuntimeProfile (на тестовых болванках).
/// </summary>
internal class EnemyRuntimeProfileDefaults : EnemyRuntimeProfile
{
    private static EnemyRuntimeProfileDefaults instance;
    public static EnemyRuntimeProfileDefaults Instance
    {
        get
        {
            if (instance == null)
            {
                instance = ScriptableObject.CreateInstance<EnemyRuntimeProfileDefaults>();
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.detectionRadius = 20f;
                instance.loseTargetRadius = 28f;
                instance.loseTargetDelaySeconds = 2f;
                instance.patrolRadius = 12f;
                instance.patrolPointTimeout = 6f;
                instance.patrolSpeed = 3.5f;
                instance.chaseSpeed = 5.5f;
                instance.fleeSpeed = 6.5f;
                instance.attackRange = 2.2f;
                instance.attackRangeHysteresis = 0.6f;
                instance.attackDuration = 0.25f;
                instance.attackCooldown = 1.0f;
                instance.attackTurnSpeed = 8f;
                instance.postAttackTurnDelay = 0.35f;
                instance.attackTurnAcceleration = 18f;
                instance.attackTurnSpeedMultiplier = 0.35f;
                instance.recoveryTurnSpeedMultiplier = 0.25f;
                instance.minAttackWindowDuration = 0.4f;
                instance.minFreeTurnTimeBeforeNextSwing = 0.25f;
                instance.fleeHpThreshold = 0.5f;
                instance.fleeDuration = 4f;
                instance.reengageHpThreshold = 0.65f;
                instance.fleeDistance = 12f;
                instance.fleePickRadius = 6f;
            }
            return instance;
        }
    }
}
