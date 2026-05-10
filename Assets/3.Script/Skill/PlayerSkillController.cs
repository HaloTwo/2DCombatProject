using System.Collections;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Hitbox skillHitbox;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private SkillData skillOne;
    [SerializeField] private SkillData skillTwo;
    [SerializeField] private SkillData[] availableSkills;

    [Header("Effects")]
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject risingEffectPrefab;
    [SerializeField] private GameObject slamEffectPrefab;
    [SerializeField] private GameObject slamDustPrefab;

    [Header("Ground Slam")]
    [SerializeField, KoreanLabel("슬램 충격파 반경")] private float groundSlamRadius = 2.3f;
    [SerializeField, KoreanLabel("슬램 넉백 힘")] private float groundSlamKnockbackForce = 8.5f;
    [SerializeField, KoreanLabel("슬램 넉백 상승 힘")] private float groundSlamKnockbackUpForce = 2.2f;
    [SerializeField, KoreanLabel("슬램 카메라 흔들림 시간")] private float groundSlamShakeDuration = 0.12f;
    [SerializeField, KoreanLabel("슬램 카메라 흔들림 힘")] private float groundSlamShakePower = 0.13f;
    [SerializeField, KoreanLabel("슬램 히트스톱")] private float groundSlamHitStop = 0.06f;
    [SerializeField, KoreanLabel("슬램 착지 대기 최대 시간")] private float groundSlamMaxFallWait = 0.65f;

    private float nextSkillOneTime;
    private float nextSkillTwoTime;
    private bool isUsingSkill;

    public SkillData SkillOne => skillOne;
    public SkillData SkillTwo => skillTwo;
    public SkillData[] AvailableSkills => availableSkills != null && availableSkills.Length > 0 ? availableSkills : new[] { skillOne, skillTwo };

    private void Reset()
    {
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (input == null) return;
        if (movement != null && movement.IsInputLocked) return;

        if (input.SkillOnePressed)
            TryUseSkill(skillOne, ref nextSkillOneTime);

        if (input.SkillTwoPressed)
            TryUseSkill(skillTwo, ref nextSkillTwoTime);
    }

    // 과제 필수 조건인 스킬 2종 이상을 한 컨트롤러에서 관리한다.
    private void TryUseSkill(SkillData skill, ref float nextUseTime)
    {
        if (!CanUseSkill(skill, nextUseTime))
            return;

        nextUseTime = Time.time + skill.cooldown;
        TriggerSkillAnimation(skill);

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

    private bool CanUseSkill(SkillData skill, float nextUseTime)
    {
        if (skill == null || isUsingSkill)
            return false;

        if (movement != null && movement.IsInputLocked)
            return false;

        return Time.time >= nextUseTime;
    }

    private void TriggerSkillAnimation(SkillData skill)
    {
        if (animator == null || skill == null)
            return;

        string triggerName = skill.skillType switch
        {
            SkillType.GroundSlam => "Attack1",
            SkillType.RisingSlash => "Attack3",
            SkillType.Projectile => "Attack3",
            SkillType.BackStepShot => "Attack3",
            SkillType.DashAttack => "Dash",
            _ => "Attack1"
        };

        animator.SetTrigger(triggerName);
    }

    // UI 슬롯 드래그 교체 시 실제 스킬 배치를 바꾼다.
    public void SwapSkillSlots()
    {
        (skillOne, skillTwo) = (skillTwo, skillOne);
        (nextSkillOneTime, nextSkillTwoTime) = (nextSkillTwoTime, nextSkillOneTime);
    }

    // 스킬 선택 UI에서 특정 슬롯에 새 스킬을 장착할 때 호출한다. 교체된 슬롯의 쿨다운은 즉시 사용할 수 있게 초기화한다.
    public void SetSkillSlot(int slotIndex, SkillData skill)
    {
        if (skill == null)
            return;

        if (slotIndex == 0)
        {
            skillOne = skill;
            nextSkillOneTime = 0f;
        }
        else
        {
            skillTwo = skill;
            nextSkillTwoTime = 0f;
        }
    }

    public float GetCooldownRemaining(int slotIndex)
    {
        SkillData skill = slotIndex == 0 ? skillOne : skillTwo;
        if (skill == null)
            return 0f;

        float nextUseTime = slotIndex == 0 ? nextSkillOneTime : nextSkillTwoTime;
        return Mathf.Max(0f, nextUseTime - Time.time);
    }

    public float GetCooldownRatio(int slotIndex)
    {
        SkillData skill = slotIndex == 0 ? skillOne : skillTwo;
        if (skill == null || skill.cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetCooldownRemaining(slotIndex) / skill.cooldown);
    }

    private IEnumerator CoDashAttack(SkillData skill)
    {
        isUsingSkill = true;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);
        SpawnEffect(slashEffectPrefab, transform.position + new Vector3(facing * 0.85f, 0.1f, 0f), new Vector3(facing, 1f, 1f));

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
        SpawnEffect(slashEffectPrefab, transform.position + new Vector3(0.65f * (movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x)), 0.05f, 0f), new Vector3(movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x), 1.35f, 1f));

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
        SpawnEffect(risingEffectPrefab, transform.position + new Vector3(facing * 0.55f, 0.4f, 0f), new Vector3(facing, 1f, 1f));

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
        movement?.LockMovementFor(Mathf.Max(skill.duration, 0.35f));

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, -Mathf.Abs(skill.force));

        float waitEndTime = Time.time + Mathf.Max(0.05f, groundSlamMaxFallWait);
        yield return new WaitUntil(() => Time.time >= waitEndTime || movement == null || movement.IsGrounded);

        Vector3 slamCenter = transform.position + new Vector3(0f, -0.45f, 0f);
        SpawnEffect(slamEffectPrefab, slamCenter, Vector3.one);
        SpawnEffect(slamDustPrefab, slamCenter, Vector3.one);
        ApplyGroundSlamArea(skill, slamCenter);

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(groundSlamShakeDuration, groundSlamShakePower);

        GlobalHitStop.Play(groundSlamHitStop);

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        yield return new WaitForSeconds(skill.duration * 0.55f);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    // 슬램 착지 타이밍에 원형 범위 피해와 바깥쪽 넉백을 같이 적용해 광역기 느낌을 만든다.
    private void ApplyGroundSlamArea(SkillData skill, Vector2 center)
    {
        if (skill == null || skill.attackData == null || groundSlamRadius <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, groundSlamRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Health == null || hurtbox.Health.Team == Team.Player)
                continue;

            Vector2 direction = ((Vector2)hurtbox.Health.transform.position - center).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = movement != null && movement.Facing < 0f ? Vector2.left : Vector2.right;

            Vector2 knockback = new Vector2(direction.x * groundSlamKnockbackForce, groundSlamKnockbackUpForce);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage);
            DamageInfo info = new DamageInfo(Team.Player, damage, hit.ClosestPoint(center), knockback, skill.attackData.hitStopTime);

            if (hurtbox.ApplyDamage(info, this) && hurtbox.Health.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(direction, groundSlamKnockbackForce, groundSlamKnockbackUpForce, 0.18f);
        }
    }

    // 뒤로 빠지면서 투사체를 발사해 근접/원거리 리듬을 섞는다.
    private IEnumerator CoBackStepShot(SkillData skill)
    {
        isUsingSkill = true;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);

        if (rb != null)
            rb.linearVelocity = new Vector2(-facing * Mathf.Abs(skill.force), rb.linearVelocity.y);

        FireProjectile(skill);
        SpawnEffect(slashEffectPrefab, transform.position + new Vector3(facing * 0.55f, 0.2f, 0f), new Vector3(facing, 1f, 1f));
        yield return new WaitForSeconds(skill.duration);

        isUsingSkill = false;
    }

    private void FireProjectile(SkillData skill)
    {
        if (skill.projectilePrefab == null || skill.attackData == null)
            return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        float facing = movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x);
        spawnPos += new Vector3(facing * 0.35f, 0f, 0f);

        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(skill.projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(skill.projectilePrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out Projectile projectile))
        {
            projectile.Fire(Team.Player, Vector2.right * facing, skill.attackData);
        }
    }

    private void SpawnEffect(GameObject prefab, Vector3 position, Vector3 scale)
    {
        if (prefab == null)
            return;

        GameObject effect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(prefab, position, Quaternion.identity)
            : Instantiate(prefab, position, Quaternion.identity);

        Vector3 baseScale = prefab.transform.localScale;
        effect.transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * scale.x, baseScale.y * scale.y, baseScale.z * scale.z);

        if (effect.TryGetComponent(out TimedAutoRelease autoRelease))
            autoRelease.Play();
    }
}
