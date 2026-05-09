using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;

    private void Reset()
    {
        BindCombat();
    }

    private void Awake()
    {
        BindCombat();
    }

    private void OnValidate()
    {
        if (combat == null)
            combat = GetComponentInParent<PlayerCombat>();
    }

    private void BindCombat()
    {
        if (combat == null)
            combat = GetComponentInParent<PlayerCombat>();
    }

    public void OpenBasicAttackHitbox()
    {
        BindCombat();
        combat?.OpenBasicAttackHitbox();
    }

    public void CloseBasicAttackHitbox()
    {
        BindCombat();
        combat?.CloseBasicAttackHitbox();
    }

    public void OpenAttackHitbox()
    {
        OpenBasicAttackHitbox();
    }

    public void CloseAttackHitbox()
    {
        CloseBasicAttackHitbox();
    }

    public void AttackHitStart()
    {
        OpenBasicAttackHitbox();
    }

    public void AttackHitEnd()
    {
        CloseBasicAttackHitbox();
    }

    public void AE_AttackStart()
    {
        OpenBasicAttackHitbox();
    }

    public void AE_AttackEnd()
    {
        CloseBasicAttackHitbox();
    }
}
