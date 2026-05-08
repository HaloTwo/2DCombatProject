using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Health health;

    public Health Health => health;

    private void Reset()
    {
        health = GetComponentInParent<Health>();
    }

    public bool ApplyDamage(DamageInfo info)
    {
        if (health == null) return false;
        return health.TakeDamage(info);
    }
}
