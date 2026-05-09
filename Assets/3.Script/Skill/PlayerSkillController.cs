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
    [SerializeField] private SkillData[] availableSkills;

    private float nextSkillOneTime;
    private float nextSkillTwoTime;
    private bool isUsingSkill;

    public SkillData SkillOne => skillOne;
    public SkillData SkillTwo => skillTwo;
    public SkillData[] AvailableSkills => availableSkills;

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
            case SkillType.RisingSlash:
                StartCoroutine(CoRisingSlash(skill));
                break;
            case SkillType.GroundSlam:
                StartCoroutine(CoGroundSlam(skill));
                break;
            case SkillType.BackStepShot:
                StartCoroutine(CoBackStepShot(skill));
                break;
        }
    }

    // UI 슬롯 드래그 교체 시 실제 스킬 배치를 바꾼다.
    public void SwapSkillSlots()
    {
        (skillOne, skillTwo) = (skillTwo, skillOne);
        (nextSkillOneTime, nextSkillTwoTime) = (nextSkillTwoTime, nextSkillOneTime);
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

    // 전방 판정과 상승 이동을 동시에 주는 띄우기 계열 스킬이다.
    private IEnumerator CoRisingSlash(SkillData skill)
    {
        isUsingSkill = true;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);

        if (rb != null)
            rb.linearVelocity = new Vector2(facing * skill.force * 0.35f, skill.force * 0.55f);

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        yield return new WaitForSeconds(skill.duration);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    // 공중에서 바닥으로 찍고 착지 전후 넓은 판정을 열어 군중 제어 느낌을 만든다.
    private IEnumerator CoGroundSlam(SkillData skill)
    {
        isUsingSkill = true;

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, -Mathf.Abs(skill.force));

        yield return new WaitForSeconds(skill.duration * 0.45f);

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        yield return new WaitForSeconds(skill.duration * 0.55f);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    // 뒤로 빠지면서 투사체를 발사해 근접/원거리 리듬을 섞는다.
    private IEnumerator CoBackStepShot(SkillData skill)
    {
        isUsingSkill = true;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);

        if (rb != null)
            rb.linearVelocity = new Vector2(-facing * Mathf.Abs(skill.force), rb.linearVelocity.y);

        FireProjectile(skill);
        yield return new WaitForSeconds(skill.duration);

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
