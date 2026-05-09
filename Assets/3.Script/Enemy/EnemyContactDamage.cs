using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 5f;
    [SerializeField] private Vector2 knockback = new Vector2(4f, 2f);
    [SerializeField] private float hitStopTime = 0.03f;
    [SerializeField] private float damageCooldown = 0.65f;

    private float nextDamageTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyContactDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyContactDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    private void TryApplyContactDamage(Collider2D target)
    {
        if (Time.time < nextDamageTime)
            return;

        if (target == null || !target.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Health == null || hurtbox.Health.Team != Team.Player)
            return;

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        Vector2 finalKnockback = new Vector2(knockback.x * Mathf.Sign(direction.x == 0f ? transform.localScale.x : direction.x), knockback.y);
        DamageInfo info = new DamageInfo(Team.Enemy, damage, target.ClosestPoint(transform.position), finalKnockback, hitStopTime);

        if (hurtbox.ApplyDamage(info, this))
            nextDamageTime = Time.time + damageCooldown;
    }
}
