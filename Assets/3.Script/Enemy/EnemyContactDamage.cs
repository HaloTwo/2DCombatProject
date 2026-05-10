using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("몸박 데미지")]
    [SerializeField, KoreanLabel("몸박 데미지")] private float damage = 5f;
    [SerializeField, KoreanLabel("몸박 넉백")] private Vector2 knockback = new Vector2(0.4f, 0.2f);
    [SerializeField, KoreanLabel("몸박 넉백 끄기")] private bool disableContactKnockback = true;
    [SerializeField, KoreanLabel("히트스톱 시간")] private float hitStopTime = 0.03f;
    [SerializeField, KoreanLabel("몸박 데미지 쿨타임")] private float damageCooldown = 0.65f;
    [SerializeField, KoreanLabel("몸박 전용 콜라이더")] private Collider2D contactArea;

    private float nextDamageTime;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        if (contactArea == null)
            contactArea = GetComponent<Collider2D>();

        contactFilter = ContactFilter2D.noFilter;
    }

    private void Update()
    {
        TryApplyOverlapDamage();
    }

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
        Vector2 finalKnockback = disableContactKnockback ? Vector2.zero : new Vector2(knockback.x * Mathf.Sign(direction.x == 0f ? transform.localScale.x : direction.x), knockback.y);
        DamageInfo info = new DamageInfo(Team.Enemy, damage, target.ClosestPoint(transform.position), finalKnockback, hitStopTime);

        if (hurtbox.ApplyDamage(info, this))
            nextDamageTime = Time.time + damageCooldown;
    }

    // 플레이어-적 물리 충돌을 무시해도 몸이 겹치면 접촉 데미지는 들어가게 처리한다.
    private void TryApplyOverlapDamage()
    {
        if (Time.time < nextDamageTime || contactArea == null)
            return;

        Bounds bounds = contactArea.bounds;
        int count = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, contactFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = overlapResults[i];
            if (overlap == null || overlap == contactArea)
                continue;

            TryApplyContactDamage(overlap);

            if (Time.time < nextDamageTime)
                return;
        }
    }
}
