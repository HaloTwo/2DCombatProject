using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerGuard guard;

    public Health Health => health;

    private void Reset()
    {
        health = GetComponentInParent<Health>();
        guard = GetComponentInParent<PlayerGuard>();
    }

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (guard == null) guard = GetComponentInParent<PlayerGuard>();
    }

    public bool ApplyDamage(DamageInfo info, Component attacker = null)
    {
        if (health == null) return false;
        if (guard != null && attacker != null && guard.TryParry(info, attacker))
            return false;

        if (guard != null)
            info = guard.ReduceGuardDamage(info);

        return health.TakeDamage(info);
    }
}
