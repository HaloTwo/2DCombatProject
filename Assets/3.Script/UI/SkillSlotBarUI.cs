using UnityEngine;

public class SkillSlotBarUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillController skillController;
    [SerializeField] private SkillSlotUI slotOne;
    [SerializeField] private SkillSlotUI slotTwo;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (skillController == null)
            return;

        slotOne?.SetCooldown(skillController.GetCooldownRatio(0), skillController.GetCooldownRemaining(0));
        slotTwo?.SetCooldown(skillController.GetCooldownRatio(1), skillController.GetCooldownRemaining(1));
    }

    public void Bind(PlayerSkillController controller)
    {
        skillController = controller;
        Refresh();
    }

    public void RequestSwap(SkillSlotUI from, SkillSlotUI to)
    {
        if (skillController == null || from == null || to == null || from == to)
            return;

        // UI 슬롯 교체가 실제 PlayerSkillController의 스킬 배치 변경으로 이어진다.
        skillController.SwapSkillSlots();
        Refresh();
    }

    public void Refresh()
    {
        if (slotOne != null)
        {
            slotOne.Bind(this, 0);
            slotOne.SetLabel("A", GetSkillName(skillController != null ? skillController.SkillOne : null));
        }

        if (slotTwo != null)
        {
            slotTwo.Bind(this, 1);
            slotTwo.SetLabel("S", GetSkillName(skillController != null ? skillController.SkillTwo : null));
        }
    }

    private static string GetSkillName(SkillData skill)
    {
        if (skill == null) return "Empty";

        return skill.skillType switch
        {
            SkillType.DashAttack => "Dash",
            SkillType.AreaAttack => "Area",
            SkillType.Projectile => "Shot",
            _ => skill.name
        };
    }
}
