using UnityEngine;

public class EliteMeleeBossEnemy : MeleeChargerEnemy
{
    [Header("Boss Phase")]
    [SerializeField, KoreanLabel("2페이즈 체력 비율")] private float phaseTwoHpRatio = 0.5f;
    [SerializeField, KoreanLabel("2페이즈 속도 배율")] private float phaseTwoSpeedMultiplier = 1.25f;
    [SerializeField, KoreanLabel("2페이즈 색상")] private Color phaseTwoColor = new Color(1f, 0.38f, 0.38f, 1f);
    [SerializeField, KoreanLabel("보스 등장 흔들림")] private bool shakeOnSpawn = true;

    [Header("Boss Hit Reaction")]
    [SerializeField, KoreanLabel("피격 모션 누적 데미지")] private float hitReactionDamageThreshold = 45f;
    [SerializeField, KoreanLabel("피격 모션 쿨타임")] private float hitReactionCooldown = 1.2f;
    [SerializeField, KoreanLabel("피격 경직 시간")] private float hitReactionStunTime = 0.18f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float baseMoveSpeed;
    private float accumulatedHitDamage;
    private float nextHitReactionTime;
    private bool phaseTwoActive;

    protected override void Awake()
    {
        base.Awake();
        baseMoveSpeed = moveSpeed;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        phaseTwoActive = false;
        moveSpeed = baseMoveSpeed;
        accumulatedHitDamage = 0f;
        nextHitReactionTime = 0f;
        ApplyRendererColors(false);

        if (shakeOnSpawn)
            CameraShake.ShakeDefault();
    }

    protected override void Update()
    {
        base.Update();
        TryEnterPhaseTwo();
    }

    // 보스는 일반 몹처럼 매 타격마다 Hurt 모션이 끊기면 전투 템포가 깨진다.
    // 데미지는 그대로 받되, 일정 누적 데미지와 쿨타임을 만족할 때만 짧게 경직시킨다.
    protected override void HandleDamaged(Health damaged, DamageInfo info)
    {
        if (damaged == null || damaged.IsDead)
            return;

        accumulatedHitDamage += Mathf.Max(0f, info.Damage);
        if (Time.time < nextHitReactionTime || accumulatedHitDamage < hitReactionDamageThreshold)
            return;

        accumulatedHitDamage = 0f;
        nextHitReactionTime = Time.time + Mathf.Max(0.1f, hitReactionCooldown);
        state = EnemyState.Hit;
        stunEndTime = Mathf.Max(stunEndTime, Time.time + Mathf.Max(0.05f, hitReactionStunTime));

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    // 체력이 절반 이하로 내려가면 같은 AI를 유지하면서 압박감만 올린다.
    // 새 패턴을 많이 늘리기보다 제출 안정성을 우선한 미니보스 연출이다.
    private void TryEnterPhaseTwo()
    {
        if (phaseTwoActive || health == null || health.MaxHp <= 0f || health.IsDead)
            return;

        if (health.CurrentHp / health.MaxHp > phaseTwoHpRatio)
            return;

        phaseTwoActive = true;
        moveSpeed = baseMoveSpeed * Mathf.Max(1f, phaseTwoSpeedMultiplier);
        ApplyRendererColors(true);
        CameraShake.ShakeDefault();
    }

    private void ApplyRendererColors(bool phaseTwo)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].color = phaseTwo ? Color.Lerp(baseColors[i], phaseTwoColor, 0.45f) : baseColors[i];
        }
    }
}
