using UnityEngine;

public class FlyingMeleeEnemy : EnemyBrainBase
{
    [Header("비행 근접 공격")]
    [SerializeField, KoreanLabel("공격 히트박스")] private Hitbox attackHitbox;
    [SerializeField, KoreanLabel("공격 데이터")] private AttackData attackData;
    [SerializeField, KoreanLabel("공격 쿨타임")] private float attackCooldown = 1f;
    [SerializeField, KoreanLabel("비행 목표 높이 보정")] private float hoverHeightOffset = 0f;
    [SerializeField, KoreanLabel("공격 상태 지속 시간")] private float attackStateDuration = 0.55f;
    [SerializeField, KoreanLabel("비행 Y축 데드존")] private float verticalDeadZone = 0.08f;
    [SerializeField, KoreanLabel("공격 Y축 허용 오차")] private float attackVerticalTolerance = 0.12f;
    [SerializeField, KoreanLabel("최대 공격 높이 보정")] private float maxAttackHeightOffset = 0f;
    [SerializeField, KoreanLabel("비행 공격 시작 거리")] private float attackStartDistance = 0.47f;
    [SerializeField, KoreanLabel("비행 접근 정지 거리")] private float attackStopDistance = 0.47f;

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
            StopFlying();
            return;
        }

        if (state == EnemyState.Idle)
        {
            StopFlying();
            return;
        }

        if (state == EnemyState.Attack)
        {
            StopFlying();
            Face(target.position.x - transform.position.x);

            if (Time.time >= attackEndTime)
            {
                CloseAttackHitbox();
                state = EnemyState.Chase;
            }

            return;
        }

        if (CanStartAttack())
        {
            StopFlying();
            Face(target.position.x - transform.position.x);
            TryAttack();
            return;
        }

        state = EnemyState.Chase;
        MoveFlyingToTarget();
    }

    private void MoveFlyingToTarget()
    {
        // 비행 몬스터가 플레이어보다 위에 멈춰서 공격하지 않도록 목표 높이를 플레이어 중심 이하로 제한한다.
        float heightOffset = Mathf.Min(hoverHeightOffset, maxAttackHeightOffset, 0f);
        Vector2 targetPosition = target.position + Vector3.up * heightOffset;
        Vector2 delta = targetPosition - rb.position;
        float deadZone = Mathf.Min(verticalDeadZone, 0.08f);
        float stopDistance = Mathf.Max(attackStopDistance, 0.1f);
        float velocityX = Mathf.Abs(delta.x) <= stopDistance ? 0f : Mathf.Sign(delta.x) * EffectiveMoveSpeed;
        float velocityY = Mathf.Abs(delta.y) < deadZone ? 0f : Mathf.Sign(delta.y) * EffectiveMoveSpeed;

        rb.linearVelocity = new Vector2(velocityX, velocityY);
        Face(delta.x);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || attackData == null || attackHitbox == null)
            return;

        nextAttackTime = Time.time + ScaleEnemyDuration(attackCooldown);
        state = EnemyState.Attack;
        attackEndTime = Time.time + ScaleEnemyDuration(attackStateDuration);
        StopFlying();

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    private bool CanStartAttack()
    {
        if (target == null)
            return false;

        if (Time.time < nextAttackTime || attackData == null || attackHitbox == null)
            return false;

        if (IsTargetInsideAttackHitbox(attackHitbox))
            return true;

        // 비행 몬스터는 플레이어 root 높이와 실제 몸체 중심이 다를 수 있어 Y축 허용치를 넉넉하게 본다.
        float startDistance = Mathf.Max(attackStartDistance, attackRange);
        float yTolerance = Mathf.Max(attackVerticalTolerance, 0.55f);
        return Mathf.Abs(target.position.x - transform.position.x) <= startDistance
            && Mathf.Abs(target.position.y - transform.position.y) <= yTolerance;
    }

    private void StopFlying()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
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
