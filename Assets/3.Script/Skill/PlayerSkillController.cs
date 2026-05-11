using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHeroKnightAnimator heroAnimator;
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
    [SerializeField, KoreanLabel("피격 플래시 이펙트")] private GameObject hitFlashEffectPrefab;

    [Header("Ground Slam")]
    [SerializeField, KoreanLabel("슬램 충격파 반경")] private float groundSlamRadius = 2.3f;
    [SerializeField, KoreanLabel("슬램 넉백 힘")] private float groundSlamKnockbackForce = 8.5f;
    [SerializeField, KoreanLabel("슬램 넉백 상승 힘")] private float groundSlamKnockbackUpForce = 2.2f;
    [SerializeField, KoreanLabel("슬램 카메라 흔들림 시간")] private float groundSlamShakeDuration = 0.12f;
    [SerializeField, KoreanLabel("슬램 카메라 흔들림 세기")] private float groundSlamShakePower = 0.13f;
    [SerializeField, KoreanLabel("슬램 히트스톱")] private float groundSlamHitStop = 0.06f;
    [SerializeField, KoreanLabel("슬램 착지 대기 최대 시간")] private float groundSlamMaxFallWait = 0.65f;

    [SerializeField, KoreanLabel("슬램 자동 점프 힘")] private float groundSlamJumpPower = 8.5f;
    [SerializeField, KoreanLabel("슬램 낙하 전 대기 시간")] private float groundSlamDiveDelay = 0.16f;

    [Header("Skill Feel")]
    [SerializeField, KoreanLabel("스킬 시전 이동 잠금 여유")] private float skillInputLockPadding = 0.08f;
    [SerializeField, KoreanLabel("투사체 발사 딜레이")] private float projectileStartupDelay = 0.08f;
    [SerializeField, KoreanLabel("기본 스킬 흔들림 시간")] private float skillShakeDuration = 0.06f;
    [SerializeField, KoreanLabel("기본 스킬 흔들림 세기")] private float skillShakePower = 0.07f;
    [SerializeField, KoreanLabel("투사체 반동 속도")] private float projectileRecoilSpeed = 2.2f;

    [Header("Dash Attack")]
    [SerializeField, KoreanLabel("대시공격 슬로우 시간")] private float dashAttackSlowDuration = 0.18f;
    [SerializeField, KoreanLabel("대시공격 적 속도 배율")] private float dashAttackEnemySlowMultiplier = 0.35f;
    [SerializeField, KoreanLabel("대시공격 투사체 속도 배율")] private float dashAttackProjectileSlowMultiplier = 0.35f;
    [SerializeField, KoreanLabel("대시공격 경로 판정 높이")] private float dashAttackPathHeight = 1.15f;
    [SerializeField, KoreanLabel("대시공격 경로 여유 폭")] private float dashAttackPathPadding = 0.75f;

    [Header("Rising Slash")]
    [SerializeField, KoreanLabel("공중공격 판정 크기")] private Vector2 risingSlashAreaSize = new Vector2(2.1f, 1.5f);
    [SerializeField, KoreanLabel("공중공격 판정 위치")] private Vector2 risingSlashAreaOffset = new Vector2(0.85f, 0.45f);
    [SerializeField, KoreanLabel("공중공격 적 띄우기 힘")] private float risingSlashEnemyLiftForce = 9f;
    [SerializeField, KoreanLabel("공중공격 본인 상승 힘")] private float risingSlashSelfLiftForce = 8.5f;
    [SerializeField, KoreanLabel("공중공격 전진 힘")] private float risingSlashForwardForce = 2.5f;

    private float nextSkillOneTime;
    private float nextSkillTwoTime;
    private bool isUsingSkill;
    private SkillData activeRisingSlashSkill;
    private float activeRisingSlashFacing = 1f;

    public SkillData SkillOne => skillOne;
    public SkillData SkillTwo => skillTwo;
    public SkillData[] AvailableSkills => availableSkills != null && availableSkills.Length > 0 ? availableSkills : new[] { skillOne, skillTwo };

    private void Reset()
    {
        input = GetComponent<PlayerInputReader>();
        movement = GetComponent<PlayerMovement2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        heroAnimator = GetComponent<PlayerHeroKnightAnimator>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (heroAnimator == null) heroAnimator = GetComponent<PlayerHeroKnightAnimator>();
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

    // 스킬 쿨타임과 사용 중 상태를 확인한 뒤 타입별 실행 루틴으로 넘긴다.
    private void TryUseSkill(SkillData skill, ref float nextUseTime)
    {
        if (!CanUseSkill(skill, nextUseTime))
            return;

        nextUseTime = Time.time + skill.cooldown;
        TriggerSkillAnimation(skill);
        PlaySkillSfx(skill);

        switch (skill.skillType)
        {
            case SkillType.DashAttack:
                StartCoroutine(CoDashAttack(skill));
                break;
            case SkillType.Projectile:
                StartCoroutine(CoProjectileSlash(skill));
                break;
            case SkillType.RisingSlash:
                StartCoroutine(CoRisingSlash(skill));
                break;
            case SkillType.GroundSlam:
                StartCoroutine(CoGroundSlam(skill));
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
            SkillType.RisingSlash => "Attack4",
            SkillType.Projectile => "Attack3",
            SkillType.DashAttack => "Dash",
            _ => "Attack1"
        };

        animator.SetTrigger(triggerName);
    }

    public void SwapSkillSlots()
    {
        (skillOne, skillTwo) = (skillTwo, skillOne);
        (nextSkillOneTime, nextSkillTwoTime) = (nextSkillTwoTime, nextSkillOneTime);
    }

    // 스킬 선택 UI에서 특정 슬롯에 새 스킬을 장착할 때 호출한다.
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
        float facing = GetFacing();
        Vector2 startPosition = rb != null ? rb.position : (Vector2)transform.position;

        FocusModeController.PlayBriefPreviewSlow(
            dashAttackSlowDuration,
            dashAttackEnemySlowMultiplier,
            dashAttackProjectileSlowMultiplier
        );

        movement?.LockMovementFor(skill.duration + skillInputLockPadding);
        PlaySkillShake(skillShakeDuration, skillShakePower);
        heroAnimator?.PlayDashGhostBurst(skill.duration);

        if (movement != null)
            movement.ApplyAttackStep(Mathf.Abs(skill.force), skill.duration, true);
        else if (rb != null)
            rb.linearVelocity = new Vector2(facing * skill.force, 0f);

        yield return new WaitForSeconds(skill.duration);

        Vector2 endPosition = rb != null ? rb.position : (Vector2)transform.position;
        ApplyDashAttackPathDamage(skill, startPosition, endPosition, facing);
        isUsingSkill = false;
    }

    private IEnumerator CoAreaAttack(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        movement?.LockMovementFor(skill.duration + skillInputLockPadding);
        PlaySkillShake(skillShakeDuration, skillShakePower);
        SpawnEffect(slashEffectPrefab, transform.position + new Vector3(facing * 0.65f, 0.05f, 0f), new Vector3(facing, 1.35f, 1f));

        if (skillHitbox != null && skill.attackData != null)
            skillHitbox.Open(Team.Player, skill.attackData);

        yield return new WaitForSeconds(skill.duration);

        skillHitbox?.Close();
        isUsingSkill = false;
    }

    // 앞쪽 판정과 상승 이동을 같이 주는 띄우기 계열 스킬이다.
    private IEnumerator CoRisingSlash(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        activeRisingSlashSkill = skill;
        activeRisingSlashFacing = facing;
        movement?.LockMovementFor(skill.duration + skillInputLockPadding);
        PlaySkillShake(skillShakeDuration, skillShakePower);
        SpawnEffect(risingEffectPrefab, transform.position + new Vector3(facing * 0.55f, 0.4f, 0f), new Vector3(facing, 1f, 1f));

        if (rb != null)
            rb.linearVelocity = new Vector2(facing * risingSlashForwardForce, risingSlashSelfLiftForce);

        yield return new WaitForSeconds(skill.duration);

        activeRisingSlashSkill = null;
        isUsingSkill = false;
    }

    // 공중에서 바닥으로 내려찍고 착지 시 광역 피해와 넉백을 만든다.
    private IEnumerator CoGroundSlam(SkillData skill)
    {
        isUsingSkill = true;
        movement?.LockMovementFor(groundSlamDiveDelay + groundSlamMaxFallWait + skill.duration + skillInputLockPadding);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, Mathf.Abs(groundSlamJumpPower));
            yield return new WaitForSeconds(Mathf.Max(0f, groundSlamDiveDelay));
            rb.linearVelocity = new Vector2(0f, -Mathf.Abs(skill.force));
        }

        float waitEndTime = Time.time + Mathf.Max(0.05f, groundSlamMaxFallWait);
        yield return new WaitUntil(() => Time.time >= waitEndTime || movement == null || (movement.IsGrounded && (rb == null || rb.linearVelocity.y <= 0.01f)));

        Vector3 slamCenter = transform.position + new Vector3(0f, -0.45f, 0f);
        SpawnEffect(slamEffectPrefab, slamCenter, Vector3.one);
        SpawnEffect(slamDustPrefab, slamCenter, Vector3.one);
        ApplyGroundSlamArea(skill, slamCenter);

        PlaySkillShake(groundSlamShakeDuration, groundSlamShakePower);
        GlobalHitStop.Play(groundSlamHitStop);

        yield return new WaitForSeconds(skill.duration * 0.55f);

        isUsingSkill = false;
    }

    // Attack4 애니메이션 첫 번째 베기 프레임에서 호출한다. 적을 먼저 띄우는 약한 타격이다.
    public void RisingSlashHit1()
    {
        ApplyRisingSlashArea(activeRisingSlashSkill, activeRisingSlashFacing, 0.85f);
    }

    // Attack4 애니메이션 두 번째 베기 프레임에서 호출한다. 같은 범위를 다시 긁어서 2타 데미지를 준다.
    public void RisingSlashHit2()
    {
        ApplyRisingSlashArea(activeRisingSlashSkill, activeRisingSlashFacing, 1f);
    }

    public void SkillHit1()
    {
        RisingSlashHit1();
    }

    public void SkillHit2()
    {
        RisingSlashHit2();
    }

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
                direction = GetFacing() < 0f ? Vector2.left : Vector2.right;

            Vector2 knockback = new Vector2(direction.x * groundSlamKnockbackForce, groundSlamKnockbackUpForce);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage);
            DamageInfo info = new DamageInfo(Team.Player, damage, hit.ClosestPoint(center), knockback, skill.attackData.hitStopTime);

            if (!hurtbox.ApplyDamage(info, this))
                continue;

            SpawnHitFlash(info.HitPoint);

            if (hurtbox.Health.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(direction, groundSlamKnockbackForce, groundSlamKnockbackUpForce, 0.18f);
        }
    }

    // 라이징 슬래시는 애니메이션 중 한 번만 전방 위쪽 영역을 긁고, 맞은 적과 플레이어를 함께 띄운다.
    private void ApplyRisingSlashArea(SkillData skill, float facing, float damageMultiplier)
    {
        if (skill == null || skill.attackData == null)
            return;

        Vector2 center = (Vector2)transform.position + new Vector2(risingSlashAreaOffset.x * facing, risingSlashAreaOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, risingSlashAreaSize, 0f);
        HashSet<Health> damagedTargets = new();
        bool hitAny = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Health == null)
                continue;

            Health targetHealth = hurtbox.Health;
            if (targetHealth.Team == Team.Player || damagedTargets.Contains(targetHealth))
                continue;

            Vector2 hitPoint = hit.ClosestPoint(center);
            Vector2 knockback = new Vector2(Mathf.Abs(skill.attackData.knockback.x) * 0.35f * facing, risingSlashEnemyLiftForce);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage * damageMultiplier);
            DamageInfo info = new DamageInfo(Team.Player, damage, hitPoint, knockback, skill.attackData.hitStopTime);

            if (!hurtbox.ApplyDamage(info, this))
                continue;

            damagedTargets.Add(targetHealth);
            hitAny = true;
            SpawnHitFlash(hitPoint);

            if (targetHealth.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(new Vector2(facing, 1f), Mathf.Abs(knockback.x), risingSlashEnemyLiftForce, 0.16f);
        }

        if (!hitAny)
            return;

        GlobalHitStop.Play(skill.attackData.hitStopTime);
        PlaySkillShake(skillShakeDuration, skillShakePower);
    }

    // 대시공격은 빠르게 지나가는 동안의 경로 전체를 한 번 훑는다.
    // 히트박스 프레임을 놓쳐도 지나간 몬스터에게는 한 번만 데미지가 들어가게 한다.
    private void ApplyDashAttackPathDamage(SkillData skill, Vector2 start, Vector2 end, float facing)
    {
        if (skill == null || skill.attackData == null)
            return;

        float width = Mathf.Max(0.75f, Mathf.Abs(end.x - start.x) + dashAttackPathPadding);
        Vector2 center = (start + end) * 0.5f + Vector2.right * facing * 0.25f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(width, dashAttackPathHeight), 0f);
        HashSet<Health> damagedTargets = new();
        bool hitAny = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Health == null)
                continue;

            Health targetHealth = hurtbox.Health;
            if (targetHealth.Team == Team.Player || damagedTargets.Contains(targetHealth))
                continue;

            Vector2 hitPoint = hit.ClosestPoint(center);
            Vector2 knockback = new Vector2(Mathf.Abs(skill.attackData.knockback.x) * facing, skill.attackData.knockback.y);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage);
            DamageInfo info = new DamageInfo(Team.Player, damage, hitPoint, knockback, skill.attackData.hitStopTime);

            if (!hurtbox.ApplyDamage(info, this))
                continue;

            damagedTargets.Add(targetHealth);
            hitAny = true;
            SpawnHitFlash(hitPoint);

            if (targetHealth.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(Vector2.right * facing, Mathf.Abs(knockback.x), knockback.y, 0.1f);
        }

        if (!hitAny)
            return;

        GlobalHitStop.Play(skill.attackData.hitStopTime);
        PlaySkillShake(skillShakeDuration * 1.2f, skillShakePower * 1.2f);
    }

    // 뒤로 빠지면서 검기를 쏘는 원거리 리듬용 스킬이다.
    private IEnumerator CoBackStepShot(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        movement?.LockMovementFor(skill.duration + skillInputLockPadding);
        PlaySkillShake(skillShakeDuration, skillShakePower);

        if (rb != null)
            rb.linearVelocity = new Vector2(-facing * Mathf.Abs(skill.force), rb.linearVelocity.y);

        yield return new WaitForSeconds(projectileStartupDelay);
        FireProjectile(skill);
        SpawnEffect(slashEffectPrefab, transform.position + new Vector3(facing * 0.55f, 0.2f, 0f), new Vector3(facing, 1f, 1f));
        yield return new WaitForSeconds(skill.duration);

        isUsingSkill = false;
    }

    // 검기 계열은 버튼 즉시 발사가 아니라 짧은 스타트업 후 발사해서 모션과 타이밍을 맞춘다.
    private IEnumerator CoProjectileSlash(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        movement?.LockMovementFor(skill.duration + projectileStartupDelay + skillInputLockPadding);

        if (rb != null && projectileRecoilSpeed > 0f)
            rb.linearVelocity = new Vector2(-facing * projectileRecoilSpeed, rb.linearVelocity.y);

        yield return new WaitForSeconds(projectileStartupDelay);

        FireProjectile(skill);
        PlaySkillShake(skillShakeDuration, skillShakePower);

        yield return new WaitForSeconds(skill.duration);
        isUsingSkill = false;
    }

    private void FireProjectile(SkillData skill)
    {
        if (skill.projectilePrefab == null || skill.attackData == null)
            return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        float facing = GetFacing();
        spawnPos += new Vector3(facing * 0.35f, 0f, 0f);

        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(skill.projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(skill.projectilePrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out Projectile projectile))
            projectile.Fire(Team.Player, Vector2.right * facing, skill.attackData);
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

    private void SpawnHitFlash(Vector2 position)
    {
        SpawnEffect(hitFlashEffectPrefab, position, Vector3.one);
    }

    private float GetFacing()
    {
        return movement != null ? movement.Facing : Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);
    }

    private void PlaySkillShake(float duration, float power)
    {
        if (CameraShake.Instance != null && duration > 0f && power > 0f)
            CameraShake.Instance.Shake(duration, power);
    }

    private void PlaySkillSfx(SkillData skill)
    {
        if (SoundManager.Instance == null || skill == null)
            return;

        SFXType sfxType = skill.skillType switch
        {
            SkillType.DashAttack => SFXType.Dash,
            SkillType.Projectile => SFXType.Projectile,
            SkillType.GroundSlam => SFXType.Slam,
            _ => SFXType.Skill
        };

        SoundManager.Instance.PlaySFX(sfxType);
    }
}
