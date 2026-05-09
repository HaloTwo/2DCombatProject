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

    // 공격 애니메이션 또는 전투 컨트롤러가 판정이 살아있는 짧은 시간 동안 호출한다.
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
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (attackData == null) return;
        if (!other.TryGetComponent(out Hurtbox hurtbox)) return;
        if (hurtbox.Health == null || hitTargets.Contains(hurtbox.Health)) return;

        Vector2 hitPoint = other.ClosestPoint(transform.position);
        Vector2 direction = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        Vector2 knockback = new Vector2(attackData.knockback.x * Mathf.Sign(direction.x == 0f ? transform.right.x : direction.x), attackData.knockback.y);

        // 데미지, 넉백, 히트스톱을 하나로 묶어 피격 대상에게 전달한다.
        DamageInfo info = new DamageInfo(ownerTeam, attackData.damage, hitPoint, knockback, attackData.hitStopTime);
        if (hurtbox.ApplyDamage(info, this))
            hitTargets.Add(hurtbox.Health);
    }

    public void ForceClose()
    {
        Close();
    }
}
