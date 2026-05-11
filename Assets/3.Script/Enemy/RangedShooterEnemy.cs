using UnityEngine;

public class RangedShooterEnemy : EnemyBrainBase
{
    [SerializeField, KoreanLabel("투사체 공격 데이터")] private AttackData projectileAttackData;
    [SerializeField, KoreanLabel("투사체 프리팹")] private GameObject projectilePrefab;
    [SerializeField, KoreanLabel("발사 위치")] private Transform firePoint;
    [SerializeField, KoreanLabel("유지 거리")] private float keepDistance = 4f;
    [SerializeField, KoreanLabel("발사 쿨타임")] private float fireCooldown = 1.25f;
    [SerializeField, KoreanLabel("공격 중 정지 시간")] private float attackMotionLockTime = 0.55f;
    [SerializeField, KoreanLabel("포물선 조준 높이 보정")] private float arcTargetHeightOffset = 0.35f;

    private float nextFireTime;
    private float attackLockedUntilTime;

    protected override void TickState()
    {
        if (target == null)
        {
            PatrolGround();
            return;
        }

        if (state == EnemyState.Idle)
        {
            PatrolGround();
            return;
        }

        Vector2 toTarget = (Vector2)target.position - rb.position;
        Face(toTarget.x);

        if (Time.time < attackLockedUntilTime)
        {
            StopMove();
            return;
        }

        float distanceX = Mathf.Abs(toTarget.x);
        if (distanceX < keepDistance)
            MoveHorizontally(-Mathf.Sign(toTarget.x));
        else if (distanceX > keepDistance + 1f)
            MoveHorizontally(Mathf.Sign(toTarget.x));
        else
            StopMove();

        TryFire();
    }

    private void MoveHorizontally(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
        {
            StopMove();
            return;
        }

        if (IsBlockedWithoutUsefulJump(direction))
        {
            StopMove();
            return;
        }

        rb.linearVelocity = new Vector2(direction * EffectiveMoveSpeed, rb.linearVelocity.y);
        TryPlatformJump(direction);
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime || projectilePrefab == null || projectileAttackData == null)
            return;

        nextFireTime = Time.time + ScaleEnemyDuration(fireCooldown);
        attackLockedUntilTime = Time.time + ScaleEnemyDuration(attackMotionLockTime);
        StopMove();

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // 원거리 공격 애니메이션의 던지는 프레임에서 호출한다.
    public void FireProjectileByAnimationEvent()
    {
        if (projectilePrefab == null || projectileAttackData == null)
            return;

        if (target != null)
            Face(target.position.x - transform.position.x);

        StopMove();
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out Projectile projectile))
        {
            Vector2 targetPosition = target != null
                ? (Vector2)target.position + Vector2.up * arcTargetHeightOffset
                : (Vector2)spawnPos + Vector2.right * facing * keepDistance;

            projectile.FireArc(Team.Enemy, targetPosition, projectileAttackData);
        }
    }

    public void FireProjectile()
    {
        FireProjectileByAnimationEvent();
    }

    public void ShootProjectile()
    {
        FireProjectileByAnimationEvent();
    }
}
