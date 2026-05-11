using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerSkillController skillController;

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
        if (skillController == null)
            skillController = GetComponentInParent<PlayerSkillController>();
    }

    private void BindCombat()
    {
        if (combat == null)
            combat = GetComponentInParent<PlayerCombat>();
        if (skillController == null)
            skillController = GetComponentInParent<PlayerSkillController>();
    }

    public void OpenBasicAttackHitbox()
    {
        BindCombat();
        if (skillController != null && skillController.IsUsingSkill)
            return;

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

    public void RisingSlashHit1()
    {
        BindCombat();
        skillController?.RisingSlashHit1();
    }

    public void RisingSlashHit2()
    {
        BindCombat();
        skillController?.RisingSlashHit2();
    }

    public void SkillHit1()
    {
        RisingSlashHit1();
    }

    public void SkillHit2()
    {
        RisingSlashHit2();
    }
}
