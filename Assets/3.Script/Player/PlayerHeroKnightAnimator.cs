using System.Collections.Generic;
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

    private readonly Dictionary<string, AnimatorControllerParameterType> parameterTypes = new();

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
    }

    private void Update()
    {
        if (animator == null)
            return;

        UpdateMovementParameters();
        UpdateActionTriggers();
        UpdateFacing();
    }

    // Hero Knight 에셋의 Animator 파라미터를 우리 Player 입력/물리 상태에 맞춰 갱신한다.
    private void UpdateMovementParameters()
    {
        bool grounded = movement != null && movement.IsGrounded;
        float horizontal = input != null ? input.Move.x : 0f;
        float verticalSpeed = rb != null ? rb.linearVelocity.y : 0f;

        SetBool("Grounded", grounded);
        SetBool("IsMoving", grounded && Mathf.Abs(horizontal) > 0.01f);
        SetFloat("AirSpeedY", verticalSpeed);
        SetInteger("AnimState", grounded && Mathf.Abs(horizontal) > 0.01f ? 1 : 0);
        SetBool("IdleBlock", input != null && input.BlockHeld);
    }

    // 실제 전투 판정은 PlayerCombat/PlayerSkillController가 담당하고, 여기서는 애니메이션 트리거만 맞춘다.
    private void UpdateActionTriggers()
    {
        if (input == null)
            return;

        if (input.JumpPressed)
            SetTrigger("Jump");

        if (input.DashPressed)
            SetTriggerFirst("Dash", "Roll");

        if (input.SkillOnePressed)
            SetTrigger("Attack2");

        if (input.SkillTwoPressed)
            SetTrigger("Attack3");

        if (input.BlockPressed)
            SetTrigger("Block");
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = false;
    }

    private void HandleDamaged(Health target, DamageInfo info)
    {
        SetTrigger("Hurt");
    }

    private void HandleDead(Health target)
    {
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
