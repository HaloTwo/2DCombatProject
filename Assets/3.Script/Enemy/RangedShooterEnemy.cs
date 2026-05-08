using UnityEngine;

public class RangedShooterEnemy : EnemyBrainBase
{
    [SerializeField] private AttackData projectileAttackData;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float keepDistance = 4f;
    [SerializeField] private float fireCooldown = 1.25f;

    private float nextFireTime;

    protected override void TickState()
    {
        if (target == null)
        {
            StopMove();
            return;
        }

        Vector2 toTarget = (Vector2)target.position - rb.position;
        Face(toTarget.x);

        if (Mathf.Abs(toTarget.x) < keepDistance)
            rb.linearVelocity = new Vector2(-Mathf.Sign(toTarget.x) * moveSpeed, rb.linearVelocity.y);
        else if (Mathf.Abs(toTarget.x) > keepDistance + 1f)
            rb.linearVelocity = new Vector2(Mathf.Sign(toTarget.x) * moveSpeed, rb.linearVelocity.y);
        else
            StopMove();

        TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime || projectilePrefab == null || projectileAttackData == null)
            return;

        nextFireTime = Time.time + fireCooldown;
        if (animator != null)
            animator.SetTrigger("Attack");

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out Projectile projectile))
            projectile.Fire(Team.Enemy, Vector2.right * facing, projectileAttackData);
    }
}
