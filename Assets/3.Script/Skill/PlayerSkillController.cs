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

    [SerializeField, KoreanLabel("슬램 착지 후 정지 시간")] private float groundSlamRecoveryLockTime = 1.2f;
    [SerializeField, KoreanLabel("슬램 이펙트 기준 반경")] private float groundSlamEffectBaseRadius = 2.3f;
    [SerializeField, KoreanLabel("슬램 범위 기즈모 표시")] private bool showGroundSlamGizmo = true;
    [SerializeField, KoreanLabel("슬램 범위 기즈모 색상")] private Color groundSlamGizmoColor = new Color(0.25f, 0.75f, 1f, 0.55f);

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

    [SerializeField, KoreanLabel("대쉬공격 연속 타격 간격")] private float dashAttackHitInterval = 0.045f;
    [SerializeField, KoreanLabel("대쉬공격 마무리 슬로우 시간")] private float dashAttackFinishSlowDuration = 0.08f;
    [SerializeField, KoreanLabel("대쉬공격 마무리 슬로우 배율")] private float dashAttackFinishSlowMultiplier = 0.25f;

    [Header("Rising Slash")]
    [SerializeField, KoreanLabel("공중공격 판정 크기")] private Vector2 risingSlashAreaSize = new Vector2(2.1f, 1.5f);
    [SerializeField, KoreanLabel("공중공격 판정 위치")] private Vector2 risingSlashAreaOffset = new Vector2(0.85f, 0.45f);
    [SerializeField, KoreanLabel("공중공격 적 띄우기 힘")] private float risingSlashEnemyLiftForce = 9f;
    [SerializeField, KoreanLabel("공중공격 본인 상승 힘")] private float risingSlashSelfLiftForce = 8.5f;
    [SerializeField, KoreanLabel("공중공격 전진 힘")] private float risingSlashForwardForce = 2.5f;

    [SerializeField, KoreanLabel("공중공격 1타 데미지 배율")] private float risingSlashFirstHitDamageMultiplier = 0.85f;
    [SerializeField, KoreanLabel("공중공격 2타 데미지 배율")] private float risingSlashSecondHitDamageMultiplier = 1f;
    [SerializeField, KoreanLabel("공중공격 2타 뒤로 날림 힘")] private float risingSlashSecondHitKnockbackForce = 6.5f;
    [SerializeField, KoreanLabel("공중공격 2타 위로 뜨는 힘")] private float risingSlashSecondHitUpForce = 1.1f;

    private float nextSkillOneTime;
    private float nextSkillTwoTime;
    private bool isUsingSkill;
    private SkillData activeRisingSlashSkill;
    private float activeRisingSlashFacing = 1f;
    private SkillData lastRisingSlashSkill;
    private float lastRisingSlashFacing = 1f;
    private float risingSlashEventValidUntil;
    private bool risingSlashHit1Triggered;
    private bool risingSlashHit2Triggered;
    private readonly List<DashAttackHitCandidate> dashAttackCandidates = new();

    public SkillData SkillOne => skillOne;
    public SkillData SkillTwo => skillTwo;
    public SkillData[] AvailableSkills => availableSkills != null && availableSkills.Length > 0 ? availableSkills : new[] { skillOne, skillTwo };
    public bool IsUsingSkill => isUsingSkill;

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
        yield return CoApplyDashAttackPathDamage(skill, startPosition, endPosition, facing);
        isUsingSkill = false;
    }

    // 앞쪽 판정과 상승 이동을 같이 주는 띄우기 계열 스킬이다.
    private IEnumerator CoRisingSlash(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        activeRisingSlashSkill = skill;
        activeRisingSlashFacing = facing;
        lastRisingSlashSkill = skill;
        lastRisingSlashFacing = facing;
        risingSlashEventValidUntil = Time.time + Mathf.Max(1.2f, skill.duration + 0.8f);
        risingSlashHit1Triggered = false;
        risingSlashHit2Triggered = false;
        movement?.LockMovementFor(skill.duration + skillInputLockPadding);
        PlaySkillShake(skillShakeDuration, skillShakePower);
        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxRisingSlashStart, SFXType.Skill);
        SpawnEffect(risingEffectPrefab, transform.position + new Vector3(facing * 0.55f, 0.4f, 0f), new Vector3(facing, 1f, 1f));

        if (rb != null)
            rb.linearVelocity = new Vector2(facing * risingSlashForwardForce, risingSlashSelfLiftForce);

        yield return new WaitForSeconds(0.08f);
        if (!risingSlashHit1Triggered)
            TryApplyRisingSlashHit(risingSlashFirstHitDamageMultiplier, true);

        yield return new WaitForSeconds(0.14f);
        if (!risingSlashHit2Triggered)
            TryApplyRisingSlashHit(risingSlashSecondHitDamageMultiplier, false);

        yield return new WaitForSeconds(Mathf.Max(0f, skill.duration - 0.22f));

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
            SoundManager.Instance?.PlayJump();
            yield return new WaitForSeconds(Mathf.Max(0f, groundSlamDiveDelay));
            rb.linearVelocity = new Vector2(0f, -Mathf.Abs(skill.force));
        }

        float waitEndTime = Time.time + Mathf.Max(0.05f, groundSlamMaxFallWait);
        yield return new WaitUntil(() => Time.time >= waitEndTime || movement == null || (movement.IsGrounded && (rb == null || rb.linearVelocity.y <= 0.01f)));

        Vector3 slamCenter = GetGroundSlamCenter();
        Vector3 slamEffectScale = Vector3.one * Mathf.Max(0.1f, groundSlamRadius / Mathf.Max(0.1f, groundSlamEffectBaseRadius));
        movement?.LockMovementFor(groundSlamRecoveryLockTime);
        movement?.StopAttackStep();
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        SpawnEffect(slamEffectPrefab, slamCenter, slamEffectScale);
        SpawnEffect(slamDustPrefab, slamCenter, Vector3.one);
        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxSlamShockwave, SFXType.Slam);
        ApplyGroundSlamArea(skill, slamCenter);

        PlaySkillShake(groundSlamShakeDuration, groundSlamShakePower);
        GlobalHitStop.Play(groundSlamHitStop);

        yield return new WaitForSeconds(Mathf.Max(skill.duration * 0.55f, groundSlamRecoveryLockTime));

        movement?.StopAttackStep();
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isUsingSkill = false;
    }

    // Attack4 애니메이션 첫 번째 베기 프레임에서 호출한다. 적을 먼저 띄우는 약한 타격이다.
    public void RisingSlashHit1()
    {
        TryApplyRisingSlashHit(risingSlashFirstHitDamageMultiplier, true);
    }

    // Attack4 애니메이션 두 번째 베기 프레임에서 호출한다. 같은 범위를 다시 긁어서 2타 데미지를 준다.
    public void RisingSlashHit2()
    {
        TryApplyRisingSlashHit(risingSlashSecondHitDamageMultiplier, false);
    }

    public void SkillHit1()
    {
        RisingSlashHit1();
    }

    public void SkillHit2()
    {
        RisingSlashHit2();
    }

    // Attack4 애니메이션 이벤트가 SkillData.duration보다 늦게 호출돼도 마지막 공중공격 정보를 짧게 유지해서 데미지를 넣는다.
    private void TryApplyRisingSlashHit(float damageMultiplier, bool firstHit)
    {
        SkillData skill = activeRisingSlashSkill;
        float facing = activeRisingSlashFacing;

        if (skill == null && Time.time <= risingSlashEventValidUntil)
        {
            skill = lastRisingSlashSkill;
            facing = lastRisingSlashFacing;
        }

        if (skill == null)
            return;

        if (firstHit)
            risingSlashHit1Triggered = true;
        else
            risingSlashHit2Triggered = true;

        ApplyRisingSlashArea(skill, facing, damageMultiplier, firstHit);
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
    private void ApplyRisingSlashArea(SkillData skill, float facing, float damageMultiplier, bool firstHit)
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
            Vector2 knockback = firstHit
                ? new Vector2(Mathf.Abs(skill.attackData.knockback.x) * 0.35f * facing, risingSlashEnemyLiftForce)
                : new Vector2(risingSlashSecondHitKnockbackForce * facing, risingSlashSecondHitUpForce);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage * damageMultiplier);
            DamageInfo info = new DamageInfo(Team.Player, damage, hitPoint, knockback, skill.attackData.hitStopTime);

            if (!hurtbox.ApplyDamage(info, this))
                continue;

            damagedTargets.Add(targetHealth);
            hitAny = true;
            SoundManager.Instance?.PlayRandomBladeHit();
            SpawnHitFlash(hitPoint);

            if (targetHealth.TryGetComponent(out EnemyBrainBase enemyBrain))
            {
                Vector2 launchDirection = firstHit ? new Vector2(facing, 1f) : Vector2.right * facing;
                enemyBrain.ApplyFocusBurstKnockback(launchDirection, Mathf.Abs(knockback.x), knockback.y, 0.16f);
            }
        }

        if (!hitAny)
            return;

        GlobalHitStop.Play(skill.attackData.hitStopTime);
        PlaySkillShake(skillShakeDuration, skillShakePower);
    }

    // 대시공격은 빠르게 지나가는 동안의 경로 전체를 한 번 훑는다.
    // 히트박스 프레임을 놓쳐도 지나간 몬스터에게는 한 번만 데미지가 들어가게 한다.
    private IEnumerator CoApplyDashAttackPathDamage(SkillData skill, Vector2 start, Vector2 end, float facing)
    {
        if (skill == null || skill.attackData == null)
            yield break;

        float width = Mathf.Max(0.75f, Mathf.Abs(end.x - start.x) + dashAttackPathPadding);
        Vector2 center = (start + end) * 0.5f + Vector2.right * facing * 0.25f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(width, dashAttackPathHeight), 0f);
        HashSet<Health> damagedTargets = new();
        dashAttackCandidates.Clear();
        bool hitAny = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Health == null)
                continue;

            Health targetHealth = hurtbox.Health;
            if (targetHealth.Team == Team.Player || damagedTargets.Contains(targetHealth))
                continue;

            damagedTargets.Add(targetHealth);
            float order = hit.bounds.center.x * facing;
            dashAttackCandidates.Add(new DashAttackHitCandidate(hit, hurtbox, targetHealth, order));
        }

        dashAttackCandidates.Sort((a, b) => a.Order.CompareTo(b.Order));

        for (int i = 0; i < dashAttackCandidates.Count; i++)
        {
            DashAttackHitCandidate candidate = dashAttackCandidates[i];
            if (candidate.Hurtbox == null || candidate.Health == null)
                continue;

            Collider2D hit = candidate.Collider;
            Vector2 hitPoint = hit.ClosestPoint(center);
            Vector2 knockback = new Vector2(Mathf.Abs(skill.attackData.knockback.x) * facing, skill.attackData.knockback.y);
            float damage = PlayerDamageBuff.ModifyPlayerDamage(skill.attackData.damage);
            DamageInfo info = new DamageInfo(Team.Player, damage, hitPoint, knockback, skill.attackData.hitStopTime);

            if (!candidate.Hurtbox.ApplyDamage(info, this))
                continue;

            hitAny = true;
            SoundManager.Instance?.PlayRandomDashAttackHit();
            SpawnHitFlash(hitPoint);

            if (candidate.Health.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(Vector2.right * facing, Mathf.Abs(knockback.x), knockback.y, 0.1f);

            if (i < dashAttackCandidates.Count - 1)
                yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, dashAttackHitInterval));
        }

        if (!hitAny)
            yield break;

        FocusModeController.PlayBriefPreviewSlow(dashAttackFinishSlowDuration, dashAttackFinishSlowMultiplier, dashAttackFinishSlowMultiplier);
        GlobalHitStop.Play(skill.attackData.hitStopTime);
        PlaySkillShake(skillShakeDuration * 1.2f, skillShakePower * 1.2f);
    }

    // 뒤로 빠지면서 검기를 쏘는 원거리 리듬용 스킬이다.
    // 검기 계열은 버튼 즉시 발사가 아니라 짧은 스타트업 후 발사해서 모션과 타이밍을 맞춘다.
    private IEnumerator CoProjectileSlash(SkillData skill)
    {
        isUsingSkill = true;
        float facing = GetFacing();
        movement?.LockMovementFor(skill.duration + projectileStartupDelay + skillInputLockPadding);

        if (rb != null && projectileRecoilSpeed > 0f)
            rb.linearVelocity = new Vector2(-facing * projectileRecoilSpeed, rb.linearVelocity.y);

        yield return new WaitForSeconds(projectileStartupDelay);

        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxSwordAreaAttack, SFXType.Projectile);
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

        if (skill.skillType == SkillType.DashAttack)
        {
            SoundManager.Instance.PlayNamedSFX(SoundManager.SfxDashAttackStart, SFXType.Dash);
            return;
        }

        if (skill.skillType == SkillType.GroundSlam || skill.skillType == SkillType.Projectile)
            return;

        SFXType sfxType = skill.skillType switch
        {
            SkillType.Projectile => SFXType.Projectile,
            _ => SFXType.Skill
        };

        SoundManager.Instance.PlaySFX(sfxType);
    }

    private Vector3 GetGroundSlamCenter()
    {
        return transform.position + new Vector3(0f, -0.45f, 0f);
    }

    private readonly struct DashAttackHitCandidate
    {
        public readonly Collider2D Collider;
        public readonly Hurtbox Hurtbox;
        public readonly Health Health;
        public readonly float Order;

        public DashAttackHitCandidate(Collider2D collider, Hurtbox hurtbox, Health health, float order)
        {
            Collider = collider;
            Hurtbox = hurtbox;
            Health = health;
            Order = order;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGroundSlamGizmo || groundSlamRadius <= 0f)
            return;

        Gizmos.color = groundSlamGizmoColor;
        Gizmos.DrawWireSphere(GetGroundSlamCenter(), groundSlamRadius);
    }
}
