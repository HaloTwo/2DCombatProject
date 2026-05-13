using UnityEngine;

public class MeleeChargerEnemy : EnemyBrainBase
{
    [SerializeField] private Hitbox attackHitbox;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackStateDuration = 0.55f;

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

        return IsTargetInsideAttackHitbox(attackHitbox) || IsTargetInAttackRange();
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
