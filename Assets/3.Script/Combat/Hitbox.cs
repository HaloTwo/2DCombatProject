using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private Team ownerTeam = Team.Player;
    [SerializeField] private AttackData attackData;
    [SerializeField] private Collider2D hitCollider;

    private readonly HashSet<Health> hitTargets = new();

    private void Reset()
    {
        hitCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();

        SetActive(false);
    }

    // 애니메이션 이벤트나 PlayerCombat에서 공격 판정이 살아있는 짧은 시간만 켠다.
    public void Open(Team team, AttackData data)
    {
        ownerTeam = team;
        attackData = data;
        hitTargets.Clear();
        SetActive(true);
    }

    public void Close()
    {
        SetActive(false);
        hitTargets.Clear();
    }

    private void SetActive(bool active)
    {
        if (hitCollider != null)
            hitCollider.enabled = active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (attackData == null) return;
        if (!other.TryGetComponent(out Hurtbox hurtbox)) return;
        if (hurtbox.Health == null || hitTargets.Contains(hurtbox.Health)) return;

        Vector2 hitPoint = other.ClosestPoint(transform.position);
        Vector2 direction = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        Vector2 knockback = new Vector2(attackData.knockback.x * Mathf.Sign(direction.x == 0f ? transform.right.x : direction.x), attackData.knockback.y);

        DamageInfo info = new DamageInfo(ownerTeam, attackData.damage, hitPoint, knockback, attackData.hitStopTime);
        if (hurtbox.ApplyDamage(info))
            hitTargets.Add(hurtbox.Health);
    }
}
