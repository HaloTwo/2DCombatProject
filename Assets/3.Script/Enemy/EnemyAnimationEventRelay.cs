using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private MeleeChargerEnemy meleeEnemy;
    [SerializeField] private FlyingMeleeEnemy flyingEnemy;
    [SerializeField] private RangedShooterEnemy rangedEnemy;

    private void Reset()
    {
        BindEnemy();
    }

    private void Awake()
    {
        BindEnemy();
    }

    private void OnValidate()
    {
        BindEnemy();
    }

    private void BindEnemy()
    {
        if (meleeEnemy == null)
            meleeEnemy = GetComponentInParent<MeleeChargerEnemy>();

        if (flyingEnemy == null)
            flyingEnemy = GetComponentInParent<FlyingMeleeEnemy>();

        if (rangedEnemy == null)
            rangedEnemy = GetComponentInParent<RangedShooterEnemy>();
    }

    public void OpenEnemyAttackHitbox()
    {
        meleeEnemy?.OpenAttackHitbox();
        flyingEnemy?.OpenAttackHitbox();
    }

    public void CloseEnemyAttackHitbox()
    {
        meleeEnemy?.CloseAttackHitbox();
        flyingEnemy?.CloseAttackHitbox();
    }

    public void FireEnemyProjectile()
    {
        rangedEnemy?.FireProjectileByAnimationEvent();
    }

    public void ShootEnemyProjectile()
    {
        FireEnemyProjectile();
    }

    public void FireProjectile()
    {
        FireEnemyProjectile();
    }

}
