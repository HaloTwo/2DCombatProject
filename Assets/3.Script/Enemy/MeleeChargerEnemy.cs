using UnityEngine;

public class MeleeChargerEnemy : EnemyBrainBase
{
    [SerializeField] private Hitbox attackHitbox;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackStateDuration = 0.55f;
    [SerializeField, KoreanLabel("근접 공격 시작 X 보정")] private float attackStartXPadding = 0.28f;
    [SerializeField, KoreanLabel("근접 공격 Y 허용치")] private float attackStartYTolerance = 0.85f;

    private float nextAttackTime;
    private float attackEndTime;

    protected override void TickState()
    {
        if (target == null)
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

        if (state == EnemyState.Idle)
        {
            PatrolGround();
            return;
        }

        if (CanStartMeleeAttack())
        {
            StopMove();
            Face(target.position.x - transform.position.x);
            TryAttack();
            return;
        }

        state = EnemyState.Chase;
        MoveToTarget();
    }

    private bool CanStartMeleeAttack()
    {
        if (target == null)
            return false;

        return IsTargetInsideAttackHitbox(attackHitbox) || IsTargetInMeleeStartRange();
    }

    // 근접 몬스터는 콜라이더 중심이 조금 어긋나도 공격을 시작해야 한다.
    // 원형 거리만 쓰면 보스처럼 큰 프리팹에서 플레이어가 살짝 위/아래에 있을 때 공격이 빠질 수 있다.
    private bool IsTargetInMeleeStartRange()
    {
        Vector2 delta = (Vector2)target.position - (Vector2)transform.position;
        float xRange = Mathf.Max(attackRange, 0.1f) + Mathf.Max(0f, attackStartXPadding);
        float yRange = Mathf.Max(0.1f, attackStartYTolerance);
        return Mathf.Abs(delta.x) <= xRange && Mathf.Abs(delta.y) <= yRange;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || attackData == null || attackHitbox == null)
            return;

        nextAttackTime = Time.time + ScaleEnemyDuration(attackCooldown);
        state = EnemyState.Attack;
        attackEndTime = Time.time + ScaleEnemyDuration(attackStateDuration);

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // 공격 애니메이션 이벤트에서 실제 타격 프레임에 호출된다.
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
