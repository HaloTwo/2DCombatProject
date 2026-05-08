using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBrainBase : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Health health;
    [SerializeField] protected Animator animator;

    [Header("Target")]
    [SerializeField] protected string playerTag = "Player";
    [SerializeField] protected float detectRange = 8f;
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float moveSpeed = 3f;

    protected Transform target;
    protected EnemyState state = EnemyState.Idle;
    protected float facing = 1f;

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
    }

    protected virtual void OnEnable()
    {
        if (health != null)
            health.OnDead += HandleDead;
    }

    protected virtual void OnDisable()
    {
        if (health != null)
            health.OnDead -= HandleDead;
    }

    protected virtual void Update()
    {
        if (state == EnemyState.Dead)
            return;

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
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        Face(dir.x);
    }

    protected void StopMove()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", state == EnemyState.Chase);
    }
}
