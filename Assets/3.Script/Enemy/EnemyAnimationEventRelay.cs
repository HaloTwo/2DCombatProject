using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private MeleeChargerEnemy meleeEnemy;
    [SerializeField] private FlyingMeleeEnemy flyingEnemy;

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

}
