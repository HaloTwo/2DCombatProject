using UnityEngine;

[CreateAssetMenu(menuName = "2D Combat/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("UI")]
    public string displayName;
    public Sprite icon;

    [Header("Skill")]
    public SkillType skillType = SkillType.DashAttack;
    public AttackData attackData;
    public float cooldown = 1.5f;
    public float duration = 0.2f;
    public float range = 2.5f;
    public float force = 16f;
    public GameObject projectilePrefab;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}
