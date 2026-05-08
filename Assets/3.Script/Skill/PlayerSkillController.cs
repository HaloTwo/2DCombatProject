using System.Collections;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Hitbox skillHitbox;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private SkillData skillOne;
    [SerializeField] private SkillData skillTwo;

    private float nextSkillOneTime;
    private float nextSkillTwoTime;
    private bool isUsingSkill;

    private void Reset()
    {
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (input == null) return;

        if (input.SkillOnePressed)
            TryUseSkill(skillOne, ref nextSkillOneTime);

        if (input.SkillTwoPressed)
            TryUseSkill(skillTwo, ref nextSkillTwoTime);
    }

    // 과제 필수 조건인 스킬 2종 이상을 한 컨트롤러에서 관리한다.
    private void TryUseSkill(SkillData skill, ref float nextUseTime)
    {
        if (skill == null || isUsingSkill) return;
        if (Time.time < nextUseTime) return;

        nextUseTime = Time.time + skill.cooldown;

        switch (skill.skillType)
        {
            case SkillType.DashAttack:
                StartCoroutine(CoDashAttack(skill));
                break;
            case SkillType.AreaAttack:
                StartCoroutine(CoAreaAttack(skill));
                break;
            case SkillType.Projectile:
                FireProjectile(skill);
                break;
        }
    }

    private IEnumerator CoDashAttack(SkillData skill)
    {
        isUsingSkill = true;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        if (rb != null)
            rb.linearVelocity = new Vector2(facing * skill.force, 0f);

        yield return new WaitForSeconds(skill.duration);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    private IEnumerator CoAreaAttack(SkillData skill)
    {
        isUsingSkill = true;

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        yield return new WaitForSeconds(skill.duration);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    private void FireProjectile(SkillData skill)
    {
        if (skill.projectilePrefab == null || skill.attackData == null)
            return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(skill.projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(skill.projectilePrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out Projectile projectile))
        {
            float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);
            projectile.Fire(Team.Player, Vector2.right * facing, skill.attackData);
        }
    }
}
