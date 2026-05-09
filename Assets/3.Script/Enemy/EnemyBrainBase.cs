using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBrainBase : MonoBehaviour, IParryReactable
{
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Health health;
    [SerializeField] protected Animator animator;

    [Header("Target")]
    [SerializeField] protected string playerTag = "Player";
    [SerializeField] protected float detectRange = 8f;
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float moveSpeed = 1.65f;

    [Header("Platform Chase")]
    [SerializeField] protected LayerMask groundMask;
    [SerializeField] protected float jumpForce = 7f;
    [SerializeField] protected float groundCheckDistance = 0.7f;
    [SerializeField] protected float obstacleCheckDistance = 0.45f;
    [SerializeField] protected float targetHeightJumpThreshold = 0.55f;
    [SerializeField] protected float jumpCooldown = 0.65f;
    [SerializeField] protected float blockedStopTime = 0.2f;

    protected Transform target;
    protected EnemyState state = EnemyState.Idle;
    protected float facing = 1f;
    protected float stunEndTime;
    private float nextJumpTime;
    private float blockedUntilTime;
    private Collider2D[] bodyColliders;
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
                target = player.transform;
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

        if (Time.time < blockedUntilTime)
        {
            StopMove();
            Face(dir.x);
            return;
        }

        if (Mathf.Abs(dir.x) > 0.01f && IsBlockedWithoutUsefulJump(xDirection))
        {
            blockedUntilTime = Time.time + blockedStopTime;
            StopMove();
            Face(dir.x);
            return;
        }

        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        Face(dir.x);
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

        bool targetIsHigher = target.position.y - transform.position.y > targetHeightJumpThreshold;
        bool hasWallAhead = HasObstacleAhead(Mathf.Sign(xDirection));
        bool canUseObstacleJump = hasWallAhead && target.position.y >= transform.position.y - 0.2f;
        if (!targetIsHigher && !canUseObstacleJump)
            return;

        nextJumpTime = Time.time + jumpCooldown;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

    // 지형에 막힌 상태에서 무의미하게 계속 밀어붙이는 것을 줄인다.
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

    // 사망 시 WaveManager가 OnDead 이벤트로 카운트를 줄이고, 적 오브젝트는 비활성화한다.
    protected virtual void HandleDead(Health dead)
    {
        state = EnemyState.Dead;
        StopMove();
        gameObject.SetActive(false);
    }

    // 실제 데미지를 받은 순간 공통 피격 애니메이션을 재생한다.
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
        animator.SetBool("IsMoving", state == EnemyState.Chase);
    }

    // 몹끼리는 서로 밀어서 길막하지 않게 하고, 플레이어/지형과는 기존 물리 충돌을 유지한다.
    private void RegisterEnemyCollision()
    {
        if (bodyColliders == null || bodyColliders.Length == 0)
            bodyColliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < activeEnemies.Count; i++)
            IgnoreCollisionWith(activeEnemies[i], true);

        if (!activeEnemies.Contains(this))
            activeEnemies.Add(this);
    }

    private void UnregisterEnemyCollision()
    {
        activeEnemies.Remove(this);

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
}
