using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerGuard guard;

    private bool lastHitBlocked;

    public Health Health => health;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (guard == null) guard = GetComponentInParent<PlayerGuard>();
    }

    public bool ApplyDamage(DamageInfo info, Component attacker = null)
    {
        lastHitBlocked = false;

        if (health == null) return false;
        if (guard != null && attacker != null && guard.TryParry(info, attacker))
        {
            lastHitBlocked = true;
            return false;
        }

        if (guard != null)
            info = guard.ReduceGuardDamage(info);

        return health.TakeDamage(info);
    }

    public bool ConsumeLastBlockedHit()
    {
        bool blocked = lastHitBlocked;
        lastHitBlocked = false;
        return blocked;
    }
}
