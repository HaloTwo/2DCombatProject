using System.Collections;
using UnityEngine;

public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject parryEffectPrefab;

    [Header("Timing")]
    [SerializeField] private float parryWindow = 0.16f;
    [SerializeField] private float guardDamageRate = 0.25f;
    [SerializeField] private float parryHitStop = 0.07f;
    [SerializeField] private float parryKnockback = 9f;

    private float parryEndTime;
    private Coroutine hitStopRoutine;

    public bool IsGuarding => input != null && input.BlockHeld;
    public bool IsParryWindow => IsGuarding && Time.time <= parryEndTime;

    private void Reset()
    {
        input = GetComponent<PlayerInputReader>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (input != null && input.BlockPressed)
            OpenParryWindow();
    }

    // 방어 입력이 시작된 직후 짧은 시간만 패링으로 인정한다.
    private void OpenParryWindow()
    {
        parryEndTime = Time.time + parryWindow;
        if (animator != null)
            animator.SetTrigger("Block");
    }

    // Hurtbox가 데미지 적용 직전에 호출한다. 성공하면 데미지는 취소되고 공격자에게 반격 연출을 보낸다.
    public bool TryParry(DamageInfo info, Component attacker)
    {
        if (!IsParryWindow || attacker == null)
            return false;

        Vector2 point = info.HitPoint;
        Vector2 direction = ((Vector2)attacker.transform.position - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        SpawnParryEffect(point);
        CameraShake.ShakeDefault();
        StartParryHitStop();

        if (attacker.TryGetComponent(out IParryReactable reactable))
            reactable.OnParried(point, direction);

        if (rb != null)
            rb.linearVelocity = new Vector2(-direction.x * Mathf.Max(1.5f, parryKnockback * 0.16f), rb.linearVelocity.y);

        parryEndTime = -999f;
        return true;
    }

    public DamageInfo ReduceGuardDamage(DamageInfo info)
    {
        if (!IsGuarding)
            return info;

        return new DamageInfo(info.AttackerTeam, info.Damage * guardDamageRate, info.HitPoint, info.Knockback * 0.35f, info.HitStopTime);
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

        hitStopRoutine = StartCoroutine(CoParryHitStop());
    }

    private IEnumerator CoParryHitStop()
    {
        float originScale = Time.timeScale;
        Time.timeScale = 0.04f;
        yield return new WaitForSecondsRealtime(parryHitStop);
        Time.timeScale = originScale;
        hitStopRoutine = null;
    }

    public float ParryKnockback => parryKnockback;
}
