using System.Collections;
using UnityEngine;

public class MeleeChargerEnemy : EnemyBrainBase
{
    [SerializeField] private Hitbox attackHitbox;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCooldown = 1f;

    private float nextAttackTime;

    protected override void TickState()
    {
        if (target == null)
        {
            StopMove();
            return;
        }

        if (IsTargetInAttackRange())
        {
            StopMove();
            Face(target.position.x - transform.position.x);
            TryAttack();
            return;
        }

        state = EnemyState.Chase;
        MoveToTarget();
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || attackData == null || attackHitbox == null)
            return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(CoAttack());
    }

    private IEnumerator CoAttack()
    {
        state = EnemyState.Attack;
        if (animator != null)
            animator.SetTrigger("Attack");

        attackHitbox.Open(Team.Enemy, attackData);
        yield return new WaitForSeconds(attackData.activeTime);
        attackHitbox.Close();

        state = EnemyState.Chase;
    }

    public override void OnParried(Vector2 parryPoint, Vector2 parryDirection)
    {
        attackHitbox?.ForceClose();
        base.OnParried(parryPoint, parryDirection);
    }
}
