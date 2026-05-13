using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHeroKnightAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Health health;
    [SerializeField] private bool useNoBloodDeath;
    [SerializeField] private string damagedTriggerName = "Block";

    [Header("Dash Effect")]
    [SerializeField] private Color dashGhostColor = new Color(0.55f, 0.85f, 1f, 0.48f);
    [SerializeField] private float dashGhostInterval = 0.035f;
    [SerializeField] private float dashGhostLifeTime = 0.22f;
    [SerializeField] private float dashAlpha = 0.62f;

    private readonly Dictionary<string, AnimatorControllerParameterType> parameterTypes = new();
    private bool wasDashing;
    private bool isDead;
    private float nextDashGhostTime;
    private float attackMotionHoldUntilTime;
    private Color originColor = Color.white;
    private Coroutine dashGhostBurstRoutine;

    private void Reset()
    {
        BindReferences();
    }

    private void Awake()
    {
        BindReferences();
        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        if (health == null) return;

        health.OnDamaged += HandleDamaged;
        health.OnDead += HandleDead;
    }

    private void OnDisable()
    {
        if (health == null) return;

        health.OnDamaged -= HandleDamaged;
        health.OnDead -= HandleDead;

        if (dashGhostBurstRoutine != null)
        {
            StopCoroutine(dashGhostBurstRoutine);
            dashGhostBurstRoutine = null;
        }
    }

    private void Update()
    {
        if (animator == null)
            return;

        if (isDead)
            return;

        UpdateMovementParameters();
        UpdateActionTriggers();
        UpdateDashEffect();
        UpdateFacing();
    }

    // Hero Knight 에셋의 Animator 파라미터를 우리 Player 입력/물리 상태에 맞춰 갱신한다.
    private void UpdateMovementParameters()
    {
        bool grounded = movement != null && movement.IsGrounded;
        bool inputLocked = movement != null && movement.IsInputLocked;
        float horizontal = !inputLocked && input != null ? input.Move.x : 0f;
        float verticalSpeed = rb != null ? rb.linearVelocity.y : 0f;

        SetBool("Grounded", grounded);
        if (Time.time < attackMotionHoldUntilTime)
        {
            SetBool("IsMoving", false);
            SetFloat("AirSpeedY", verticalSpeed);
            SetInteger("AnimState", 0);
            SetBool("IdleBlock", false);
            return;
        }

        SetBool("IsMoving", grounded && Mathf.Abs(horizontal) > 0.01f);
        SetFloat("AirSpeedY", verticalSpeed);
        SetInteger("AnimState", grounded && Mathf.Abs(horizontal) > 0.01f ? 1 : 0);
        SetBool("IdleBlock", !inputLocked && input != null && input.BlockHeld);
    }

    // 실제 전투 판정은 PlayerCombat/PlayerSkillController가 담당하고, 여기서는 애니메이션 트리거만 맞춘다.
    private void UpdateActionTriggers()
    {
        if (input == null || (movement != null && movement.IsInputLocked))
            return;

        if (input.JumpPressed)
            SetTrigger("Jump");

        if (input.DashPressed)
            SetTriggerFirst("Dash", "Roll");

        // 가드/패링 시작 트리거는 PlayerGuard에서만 처리한다.
    }

    // PlayerCombat이 공격 트리거를 넣는 순간 호출한다. 공격 시작 프레임의 파라미터 충돌을 막는다.
    public void HoldAttackMotion(float duration)
    {
        attackMotionHoldUntilTime = Mathf.Max(attackMotionHoldUntilTime, Time.time + Mathf.Max(0f, duration));
        SetBool("IsMoving", false);
        SetInteger("AnimState", 0);
        SetBool("IdleBlock", false);
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = false;
    }

    private void HandleDamaged(Health target, DamageInfo info)
    {
        if (isDead)
            return;

        SetTrigger(damagedTriggerName);
    }

    private void HandleDead(Health target)
    {
        isDead = true;
        SetBool("noBlood", useNoBloodDeath);
        SetTrigger("Death");
    }

    private void BindReferences()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<Health>();
        if (spriteRenderer != null) originColor = spriteRenderer.color;
    }

    private void UpdateDashEffect()
    {
        if (movement == null || spriteRenderer == null)
            return;

        if (movement.IsDashing)
        {
            if (!wasDashing)
            {
                originColor = spriteRenderer.color;
                spriteRenderer.color = new Color(originColor.r, originColor.g, originColor.b, dashAlpha);
                nextDashGhostTime = 0f;
            }

            if (Time.time >= nextDashGhostTime)
                SpawnDashGhost();
        }
        else if (wasDashing)
        {
            spriteRenderer.color = originColor;
        }

        wasDashing = movement.IsDashing;
    }

    private void SpawnDashGhost()
    {
        nextDashGhostTime = Time.time + dashGhostInterval;
        SpawnGhost(dashGhostColor);
    }

    // 대시 스킬처럼 Movement의 IsDashing이 켜지지 않는 액션에서도 같은 잔상 연출을 재사용한다.
    public void PlayDashGhostBurst(float duration)
    {
        if (duration <= 0f || spriteRenderer == null)
            return;

        if (dashGhostBurstRoutine != null)
            StopCoroutine(dashGhostBurstRoutine);

        dashGhostBurstRoutine = StartCoroutine(CoDashGhostBurst(duration));
    }

    private IEnumerator CoDashGhostBurst(float duration)
    {
        Color cachedOrigin = spriteRenderer.color;
        spriteRenderer.color = new Color(cachedOrigin.r, cachedOrigin.g, cachedOrigin.b, dashAlpha);

        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            SpawnGhost(dashGhostColor);
            yield return new WaitForSeconds(dashGhostInterval);
        }

        if ((movement == null || !movement.IsDashing) && spriteRenderer != null)
            spriteRenderer.color = cachedOrigin;

        dashGhostBurstRoutine = null;
    }

    private void SpawnGhost(Color color)
    {
        if (spriteRenderer == null)
            return;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = spriteRenderer.transform.position;
        ghost.transform.rotation = spriteRenderer.transform.rotation;
        ghost.transform.localScale = spriteRenderer.transform.lossyScale;

        SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = spriteRenderer.sprite;
        renderer.flipX = spriteRenderer.flipX;
        renderer.flipY = spriteRenderer.flipY;
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        renderer.color = color;

        StartCoroutine(CoFadeDashGhost(renderer, ghost));
    }

    private IEnumerator CoFadeDashGhost(SpriteRenderer renderer, GameObject ghost)
    {
        float elapsed = 0f;
        Color startColor = renderer.color;

        while (elapsed < dashGhostLifeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / dashGhostLifeTime);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghost);
    }

    private void CacheAnimatorParameters()
    {
        parameterTypes.Clear();
        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
            parameterTypes[parameters[i].name] = parameters[i].type;
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        return parameterTypes.TryGetValue(parameterName, out AnimatorControllerParameterType cachedType) && cachedType == type;
    }

    private void SetBool(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            animator.SetBool(parameterName, value);
    }

    private void SetFloat(string parameterName, float value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Float))
            animator.SetFloat(parameterName, value);
    }

    private void SetInteger(string parameterName, int value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
            animator.SetInteger(parameterName, value);
    }

    private void SetTrigger(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(parameterName);
    }

    private void SetTriggerFirst(string primaryParameter, string fallbackParameter)
    {
        if (HasParameter(primaryParameter, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(primaryParameter);
            return;
        }

        SetTrigger(fallbackParameter);
    }
}
