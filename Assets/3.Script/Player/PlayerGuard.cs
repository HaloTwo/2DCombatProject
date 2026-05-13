using System.Collections;
using UnityEngine;

public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject parryEffectPrefab;
    readonly string guardFallbackTriggerName = "Block";

    [Header("Timing")]
    [SerializeField] private float parryWindow = 0.16f;
    [SerializeField] private float guardDamageRate = 0.5f;
    [SerializeField] private float parryHitStop = 0.07f;
    [SerializeField] private float parryKnockback = 9f;
    [SerializeField, KoreanLabel("패링 시작 정지 시간")] private float parryStartLockTime = 0.12f;
    [SerializeField, KoreanLabel("패링 성공 정지 시간")] private float parrySuccessLockTime = 0.16f;

    private float parryEndTime;
    private Coroutine hitStopRoutine;

    public bool IsGuarding => input != null && input.BlockHeld && (!IsInputLocked() || Time.time <= parryEndTime);
    public bool IsParryWindow => IsGuarding && Time.time <= parryEndTime;

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
        if (input != null && !IsInputLocked() && input.BlockPressed)
            OpenParryWindow();
    }

    // 방어 입력이 시작된 직후 짧은 시간만 패링으로 인정한다.
    private void OpenParryWindow()
    {
        parryEndTime = Time.time + parryWindow;
        movement?.LockMovementFor(parryStartLockTime);
        SetAnimatorTrigger(guardFallbackTriggerName);
    }

    // Hurtbox가 데미지 적용 직전에 호출한다. 패링 성공 시 데미지는 막고, 투사체만 반사한다.
    // 근접 공격은 적 모션을 끊지 않고 해당 히트만 소비해서 보스/일반몹 공격 흐름을 유지한다.
    public bool TryParry(DamageInfo info, Component attacker)
    {
        if (!IsParryWindow)
            return false;

        Vector2 point = info.HitPoint;
        Vector2 attackerPosition = attacker != null ? attacker.transform.position : transform.position + transform.right;
        Vector2 direction = (attackerPosition - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        SpawnParryEffect(point);
        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxGuardParry, SFXType.Guard);
        CameraShake.ShakeDefault();
        StartParryHitStop();

        if (attacker != null && attacker.TryGetComponent(out Projectile projectile))
            projectile.OnParried(point, direction);

        if (rb != null)
            rb.linearVelocity = new Vector2(-direction.x * Mathf.Max(1.5f, parryKnockback * 0.16f), rb.linearVelocity.y);

        movement?.LockMovementFor(parrySuccessLockTime);

        parryEndTime = -999f;
        return true;
    }

    public DamageInfo ReduceGuardDamage(DamageInfo info)
    {
        if (!IsGuarding)
            return info;

        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxGuardBlock, SFXType.Guard);
        return new DamageInfo(info.AttackerTeam, info.Damage * guardDamageRate, info.HitPoint, info.Knockback * 0.35f, info.HitStopTime);
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator == null)
            return;

        if (HasTrigger(triggerName))
        {
            animator.SetTrigger(triggerName);
            return;
        }

        if (!string.IsNullOrEmpty(guardFallbackTriggerName) && HasTrigger(guardFallbackTriggerName))
            animator.SetTrigger(guardFallbackTriggerName);
    }

    private bool HasTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == triggerName && parameters[i].type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private void SpawnParryEffect(Vector2 point)
    {
        if (parryEffectPrefab == null)
            return;

        GameObject effect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(parryEffectPrefab, point, Quaternion.identity)
            : Instantiate(parryEffectPrefab, point, Quaternion.identity);

        if (effect.TryGetComponent(out TimedAutoRelease autoRelease))
            autoRelease.Play();
    }

    private void StartParryHitStop()
    {
        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);

        GlobalHitStop.Play(parryHitStop);
        hitStopRoutine = StartCoroutine(CoParryHitStopMarker());
    }

    private IEnumerator CoParryHitStopMarker()
    {
        yield return new WaitForSecondsRealtime(parryHitStop);
        hitStopRoutine = null;
    }

    public float ParryKnockback => parryKnockback;

    private bool IsInputLocked()
    {
        return movement != null && movement.IsInputLocked;
    }
}
