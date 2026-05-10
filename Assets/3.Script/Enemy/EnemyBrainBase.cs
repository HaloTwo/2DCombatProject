using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBrainBase : MonoBehaviour, IParryReactable
{
    [SerializeField, KoreanLabel("리지드바디")] protected Rigidbody2D rb;
    [SerializeField, KoreanLabel("체력")] protected Health health;
    [SerializeField, KoreanLabel("애니메이터")] protected Animator animator;

    [Header("타겟")]
    [SerializeField, KoreanLabel("플레이어 태그")] protected string playerTag = "Player";
    [SerializeField, KoreanLabel("감지 범위")] protected float detectRange = 8f;
    [SerializeField, KoreanLabel("공격 범위(예비값)")] protected float attackRange = 1.2f;
    [SerializeField, KoreanLabel("이동 속도")] protected float moveSpeed = 1.65f;

    [Header("순찰")]
    [SerializeField, KoreanLabel("대기 중 순찰")] protected bool patrolWhenIdle = true;
    [SerializeField, KoreanLabel("순찰 속도 배율")] protected float patrolSpeedMultiplier = 0.55f;
    [SerializeField, KoreanLabel("순찰 벽 감지 거리")] protected float patrolWallCheckDistance = 0.18f;
    [SerializeField, KoreanLabel("낭떠러지 앞쪽 검사 거리")] protected float patrolEdgeCheckForward = 0.72f;
    [SerializeField, KoreanLabel("낭떠러지 아래 검사 거리")] protected float patrolEdgeCheckDown = 1.25f;
    [SerializeField, KoreanLabel("방향 전환 쿨타임")] protected float patrolTurnCooldown = 0.45f;

    [Header("플랫폼 추적")]
    [SerializeField, KoreanLabel("지형 레이어")] protected LayerMask groundMask;
    [SerializeField, KoreanLabel("점프 힘")] protected float jumpForce = 7f;
    [SerializeField, KoreanLabel("바닥 검사 거리")] protected float groundCheckDistance = 0.7f;
    [SerializeField, KoreanLabel("장애물 검사 거리")] protected float obstacleCheckDistance = 0.45f;
    [SerializeField, KoreanLabel("추적 점프 높이 기준")] protected float targetHeightJumpThreshold = 0.55f;
    [SerializeField, KoreanLabel("최소 플랫폼 점프 높이")] protected float minPlatformJumpHeight = 1.1f;
    [SerializeField, KoreanLabel("점프 쿨타임")] protected float jumpCooldown = 0.65f;
    [SerializeField, KoreanLabel("막힘 회피 시간")] protected float blockedStopTime = 0.2f;

    protected Transform target;
    protected EnemyState state = EnemyState.Idle;
    protected float facing = 1f;
    protected float patrolDirection = 1f;
    protected float stunEndTime;
    protected float EffectiveMoveSpeed => moveSpeed * FocusModeController.EnemySpeedMultiplier;
    protected float EnemyTimeScale => Mathf.Clamp(FocusModeController.EnemySpeedMultiplier, 0.05f, 1f);
    private float nextJumpTime;
    private float blockedUntilTime;
    private float nextPatrolTurnTime;
    private float nextPlayerCollisionRefreshTime;
    private Collider2D[] bodyColliders;
    private Collider2D[] playerColliders;
    private static readonly System.Collections.Generic.List<EnemyBrainBase> activeEnemies = new();

    protected virtual void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<Health>();
        bodyColliders = GetComponentsInChildren<Collider2D>();

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default");
    }

    protected virtual void OnEnable()
    {
        RegisterEnemyCollision();

        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnDead += HandleDead;
        }
    }

    protected virtual void OnDisable()
    {
        UnregisterEnemyCollision();

        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDead -= HandleDead;
        }
    }

    protected virtual void Update()
    {
        if (state == EnemyState.Dead)
            return;

        if (Time.time < stunEndTime)
        {
            StopMove();
            UpdateAnimator();
            return;
        }

        ResolveTarget();
        RefreshPlayerCollisionIgnore();
        TickState();
        UpdateAnimator();
    }

    protected abstract void TickState();

    // 플레이어를 찾고 감지 범위에 따라 Idle/Chase 상태를 전환한다.
    protected void ResolveTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
                playerColliders = player.GetComponentsInChildren<Collider2D>();
                IgnorePlayerCollision(true);
            }
            else
            {
                PlayerMovement2D playerMovement = FindFirstObjectByType<PlayerMovement2D>();
                if (playerMovement != null)
                {
                    target = playerMovement.transform;
                    playerColliders = playerMovement.GetComponentsInChildren<Collider2D>();
                    IgnorePlayerCollision(true);
                }
            }
        }

        if (target == null)
        {
            state = EnemyState.Idle;
            return;
        }

        float distSqr = ((Vector2)target.position - rb.position).sqrMagnitude;
        if (distSqr > detectRange * detectRange)
            state = EnemyState.Idle;
        else if (state == EnemyState.Idle)
            state = EnemyState.Chase;
    }

    protected bool IsTargetInAttackRange()
    {
        if (target == null) return false;
        return ((Vector2)target.position - rb.position).sqrMagnitude <= attackRange * attackRange;
    }

    protected bool IsTargetInsideAttackHitbox(Hitbox attackHitbox)
    {
        if (target == null || attackHitbox == null)
            return false;

        Collider2D attackCollider = attackHitbox.GetComponent<Collider2D>();
        if (attackCollider == null)
            return IsTargetInAttackRange();

        Bounds attackBounds = GetColliderWorldBounds(attackCollider);
        Collider2D[] targetColliders = target.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < targetColliders.Length; i++)
        {
            Collider2D targetCollider = targetColliders[i];
            if (targetCollider == null || targetCollider.isTrigger)
                continue;

            Health targetHealth = targetCollider.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth.Team != Team.Player)
                continue;

            if (attackBounds.Intersects(targetCollider.bounds))
                return true;
        }

        return false;
    }

    protected void MoveToTarget()
    {
        if (target == null)
        {
            StopMove();
            return;
        }

        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        float xDirection = Mathf.Sign(dir.x);

        if (Mathf.Abs(dir.x) > 0.01f)
            TryPlatformJump(xDirection);

        if (Mathf.Abs(dir.x) > 0.01f && IsBlockedWithoutUsefulJump(xDirection))
        {
            blockedUntilTime = Time.time + blockedStopTime;
            rb.linearVelocity = new Vector2(-xDirection * EffectiveMoveSpeed * patrolSpeedMultiplier, rb.linearVelocity.y);
            Face(dir.x);
            return;
        }

        rb.linearVelocity = new Vector2(dir.x * EffectiveMoveSpeed, rb.linearVelocity.y);
        Face(dir.x);
    }

    // 플레이어를 감지하지 못한 지상 몬스터가 낭떠러지나 벽을 만나기 전까지 좌우로 순찰한다.
    protected void PatrolGround()
    {
        if (!patrolWhenIdle || rb == null)
        {
            StopMove();
            return;
        }

        if (!IsGrounded())
        {
            StopMove();
            return;
        }

        float direction = Mathf.Sign(patrolDirection == 0f ? facing : patrolDirection);
        if (Time.time >= nextPatrolTurnTime && (HasPatrolWallAhead(direction) || !HasGroundAhead(direction)))
        {
            direction *= -1f;
            patrolDirection = direction;
            nextPatrolTurnTime = Time.time + patrolTurnCooldown;
        }

        rb.linearVelocity = new Vector2(direction * EffectiveMoveSpeed * patrolSpeedMultiplier, rb.linearVelocity.y);
        Face(direction);
    }

    protected void StopMove()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected void TryPlatformJump(float xDirection)
    {
        if (rb == null || target == null || Time.time < nextJumpTime || Mathf.Abs(xDirection) < 0.01f)
            return;

        if (!IsGrounded())
            return;

        if (target.TryGetComponent(out PlayerMovement2D playerMovement) && !playerMovement.IsGrounded)
            return;

        float requiredHeight = Mathf.Max(targetHeightJumpThreshold, minPlatformJumpHeight);
        bool targetIsHigher = target.position.y - transform.position.y > requiredHeight;
        if (!targetIsHigher)
            return;

        nextJumpTime = Time.time + ScaleEnemyDuration(jumpCooldown);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    protected float ScaleEnemyDuration(float duration)
    {
        return duration / EnemyTimeScale;
    }

    protected bool IsGrounded()
    {
        if (rb == null)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    protected bool HasObstacleAhead(float direction)
    {
        Vector2 origin = rb.position + Vector2.up * 0.15f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, obstacleCheckDistance, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    // 순찰 중 바로 앞 벽만 짧게 검사해서 코너에 닿았다고 과하게 방향 전환하지 않게 한다.
    protected bool HasPatrolWallAhead(float direction)
    {
        Vector2 origin = rb.position + Vector2.up * 0.15f;
        float distance = Mathf.Min(patrolWallCheckDistance, 0.18f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, distance, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    // 이동 방향 앞쪽 바닥을 넉넉히 확인해서 좁은 발판에서 제자리 왕복하는 빈도를 줄인다.
    protected bool HasGroundAhead(float direction)
    {
        float forwardDistance = Mathf.Max(patrolEdgeCheckForward, 0.72f);
        float downDistance = Mathf.Max(patrolEdgeCheckDown, 1.25f);
        Vector2 origin = rb.position + new Vector2(direction * forwardDistance, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, downDistance, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    // 비활성화된 공격 히트박스도 공격 사정거리 판단에 쓸 수 있게 월드 Bounds를 계산한다.
    private Bounds GetColliderWorldBounds(Collider2D source)
    {
        if (source is BoxCollider2D box)
        {
            Vector3 center = box.transform.TransformPoint(box.offset);
            Vector3 size = new Vector3(Mathf.Abs(box.size.x * box.transform.lossyScale.x), Mathf.Abs(box.size.y * box.transform.lossyScale.y), 0.1f);
            return new Bounds(center, size);
        }

        if (source is CircleCollider2D circle)
        {
            Vector3 center = circle.transform.TransformPoint(circle.offset);
            float radius = circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y));
            return new Bounds(center, new Vector3(radius * 2f, radius * 2f, 0.1f));
        }

        if (source is CapsuleCollider2D capsule)
        {
            Vector3 center = capsule.transform.TransformPoint(capsule.offset);
            Vector3 size = new Vector3(Mathf.Abs(capsule.size.x * capsule.transform.lossyScale.x), Mathf.Abs(capsule.size.y * capsule.transform.lossyScale.y), 0.1f);
            return new Bounds(center, size);
        }

        return source.bounds;
    }

    protected bool IsBlockedWithoutUsefulJump(float direction)
    {
        if (!IsGrounded() || !HasObstacleAhead(direction))
            return false;

        bool targetIsHigher = target != null && target.position.y - transform.position.y > targetHeightJumpThreshold;
        return !targetIsHigher && Time.time < nextJumpTime;
    }

    protected void Face(float xDirection)
    {
        if (Mathf.Abs(xDirection) < 0.01f) return;

        facing = Mathf.Sign(xDirection);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        transform.localScale = scale;
    }

    // 사망 시 스폰/웨이브 시스템이 재사용할 수 있도록 오브젝트를 비활성화한다.
    protected virtual void HandleDead(Health dead)
    {
        state = EnemyState.Dead;
        StopMove();
        gameObject.SetActive(false);
    }

    // 피격 직후에는 짧게 경직시키고 공통 피격 애니메이션을 재생한다.
    protected virtual void HandleDamaged(Health damaged, DamageInfo info)
    {
        state = EnemyState.Hit;
        stunEndTime = Mathf.Max(stunEndTime, Time.time + 0.12f);

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    public virtual void OnParried(Vector2 parryPoint, Vector2 parryDirection)
    {
        stunEndTime = Time.time + 0.45f;
        state = EnemyState.Hit;
        rb.linearVelocity = new Vector2(parryDirection.x * 8f, 3.5f);

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.speed = EnemyTimeScale;
        animator.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.05f && state != EnemyState.Attack && state != EnemyState.Hit);
    }

    // 몹끼리는 서로 밀어서 길막하지 않게 하고, 플레이어/지형과는 기존 물리 충돌을 유지한다.
    // 몬스터끼리는 서로 밀리지 않게 하고, 플레이어와의 물리 충돌도 무시한다.
    private void RegisterEnemyCollision()
    {
        if (bodyColliders == null || bodyColliders.Length == 0)
            bodyColliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < activeEnemies.Count; i++)
            IgnoreCollisionWith(activeEnemies[i], true);

        if (!activeEnemies.Contains(this))
            activeEnemies.Add(this);

        IgnorePlayerCollision(true);
    }

    private void UnregisterEnemyCollision()
    {
        activeEnemies.Remove(this);
        IgnorePlayerCollision(false);

        for (int i = 0; i < activeEnemies.Count; i++)
            IgnoreCollisionWith(activeEnemies[i], false);
    }

    private void IgnoreCollisionWith(EnemyBrainBase other, bool ignore)
    {
        if (other == null || other.bodyColliders == null)
            return;

        for (int i = 0; i < bodyColliders.Length; i++)
        {
            Collider2D own = bodyColliders[i];
            if (own == null || own.isTrigger)
                continue;

            for (int j = 0; j < other.bodyColliders.Length; j++)
            {
                Collider2D targetCollider = other.bodyColliders[j];
                if (targetCollider == null || targetCollider.isTrigger)
                    continue;

                Physics2D.IgnoreCollision(own, targetCollider, ignore);
            }
        }
    }

    private void IgnorePlayerCollision(bool ignore)
    {
        if (bodyColliders == null || bodyColliders.Length == 0)
            bodyColliders = GetComponentsInChildren<Collider2D>();

        if ((playerColliders == null || playerColliders.Length == 0) && target != null)
            playerColliders = target.GetComponentsInChildren<Collider2D>();

        if (playerColliders == null)
            return;

        for (int i = 0; i < bodyColliders.Length; i++)
        {
            Collider2D own = bodyColliders[i];
            if (own == null || own.isTrigger)
                continue;

            for (int j = 0; j < playerColliders.Length; j++)
            {
                Collider2D playerCollider = playerColliders[j];
                if (playerCollider == null || playerCollider.isTrigger)
                    continue;

                Physics2D.IgnoreCollision(own, playerCollider, ignore);
            }
        }
    }

    // 플레이어 프리팹/리스폰 타이밍에 콜라이더 목록이 바뀌어도 적과 플레이어가 서로 밀어내지 않게 재적용한다.
    // 플레이어 프리팹/리스폰 타이밍에 콜라이더 목록이 바뀌어도 충돌 무시를 다시 적용한다.
    private void RefreshPlayerCollisionIgnore()
    {
        if (Time.time < nextPlayerCollisionRefreshTime)
            return;

        nextPlayerCollisionRefreshTime = Time.time + 0.2f;

        if (target == null)
            return;

        Collider2D[] latestPlayerColliders = target.GetComponentsInChildren<Collider2D>();
        if (latestPlayerColliders == null || latestPlayerColliders.Length == 0)
            return;

        playerColliders = latestPlayerColliders;
        IgnorePlayerCollision(true);
    }
}
