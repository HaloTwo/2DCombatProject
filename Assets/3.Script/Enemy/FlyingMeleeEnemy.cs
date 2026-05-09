using System.Collections;
using UnityEngine;

public class FlyingMeleeEnemy : EnemyBrainBase
{
    [SerializeField] private Hitbox attackHitbox;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float hoverHeightOffset = 0.15f;

    private float nextAttackTime;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        SetBodyCollidersTrigger();
    }

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
        MoveFlyingToTarget();
    }

    private void MoveFlyingToTarget()
    {
        Vector2 targetPosition = target.position + Vector3.up * hoverHeightOffset;
        Vector2 direction = (targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        Face(direction.x);
    }

    // 비행형 몬스터는 지형 충돌로 멈추면 추격감이 깨지므로 몸통 콜라이더를 트리거로 돌린다.
    private void SetBodyCollidersTrigger()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D targetCollider = colliders[i];
            if (targetCollider == null || targetCollider.GetComponent<Hitbox>() != null)
                continue;

            targetCollider.isTrigger = true;
        }
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

        yield return new WaitForSeconds(0.12f);
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
