using UnityEngine;

public class FlyingMeleeEnemy : EnemyBrainBase
{
    [SerializeField] private Hitbox attackHitbox;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float hoverHeightOffset = 0.15f;
    [SerializeField] private float attackStateDuration = 0.55f;

    private float nextAttackTime;
    private float attackEndTime;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 콜라이더 크기/오프셋/트리거 여부는 프리팹 인스펙터에서 맞춘 값을 그대로 따른다.
    }

    protected override void TickState()
    {
        if (target == null)
        {
            StopMove();
            return;
        }

        if (state == EnemyState.Idle)
        {
            StopMove();
            return;
        }

        if (state == EnemyState.Attack)
        {
            StopMove();
            Face(target.position.x - transform.position.x);

            if (Time.time >= attackEndTime)
            {
                CloseAttackHitbox();
                state = EnemyState.Chase;
            }

            return;
        }

        if (IsTargetInsideAttackHitbox(attackHitbox))
        {
            StopMove();
            Face(target.position.x - transform.position.x);
            TryAttack();
            return;
        }

        state = EnemyState.Chase;
        MoveFlyingToTarget();
    }

    private void MoveFlyingToTarget()
    {
        Vector2 targetPosition = target.position + Vector3.up * hoverHeightOffset;
        Vector2 direction = (targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        Face(direction.x);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || attackData == null || attackHitbox == null)
            return;

        nextAttackTime = Time.time + attackCooldown;
        state = EnemyState.Attack;
        attackEndTime = Time.time + attackStateDuration;

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // 공격 애니메이션 이벤트에서 판정이 실제로 닿는 프레임에 호출한다.
    public void OpenAttackHitbox()
    {
        if (state != EnemyState.Attack || attackHitbox == null || attackData == null)
            return;

        attackHitbox.Open(Team.Enemy, attackData);
    }

    public void CloseAttackHitbox()
    {
        attackHitbox?.Close();
    }

    public override void OnParried(Vector2 parryPoint, Vector2 parryDirection)
    {
        attackHitbox?.ForceClose();
        base.OnParried(parryPoint, parryDirection);
    }
}
