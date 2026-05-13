using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField, KoreanLabel("입력 리더")] private PlayerInputReader input;
    [SerializeField, KoreanLabel("이동 컨트롤러")] private PlayerMovement2D movement;
    [SerializeField, KoreanLabel("기본 공격 히트박스")] private Hitbox basicAttackHitbox;
    [SerializeField, KoreanLabel("기본 공격 데이터")] private AttackData basicAttack;
    [SerializeField, KoreanLabel("애니메이터")] private Animator animator;
    [SerializeField, KoreanLabel("애니메이션 이벤트 판정 사용")] private bool useAnimationEventHitTiming;
    [SerializeField, KoreanLabel("공격 모션 시간")] private float attackMotionTime = 0.42f;
    [SerializeField, KoreanLabel("공격 선딜 시간")] private float attackStartupTime = 0.18f;
    [SerializeField, KoreanLabel("콤보 입력 허용 시간")] private float comboInputWindow = 0.1f;

    [Header("공격 이동")]
    [SerializeField, KoreanLabel("공격 중 이동 잠금 시간")] private float attackMoveLockTime = 0.36f;
    [SerializeField, KoreanLabel("1타 전진 속도")] private float attack1StepSpeed = 2.5f;
    [SerializeField, KoreanLabel("2타 전진 속도")] private float attack2StepSpeed = 3.3f;
    [SerializeField, KoreanLabel("전진 지속 시간")] private float attackStepDuration = 0.08f;

    [SerializeField, KoreanLabel("애니메이터 브릿지")] private PlayerHeroKnightAnimator heroAnimator;

    private bool isAttacking;
    private bool isWaitingComboInput;
    private int basicAttackStep;
    private int activeAttackStepIndex;
    private int comboVersion;

    private void Reset()
    {
        input = GetComponentInParent<PlayerInputReader>();
        movement = GetComponentInParent<PlayerMovement2D>();
        animator = GetComponentInChildren<Animator>();
        heroAnimator = GetComponentInParent<PlayerHeroKnightAnimator>();
        if (heroAnimator == null) heroAnimator = GetComponentInChildren<PlayerHeroKnightAnimator>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (heroAnimator == null) heroAnimator = GetComponentInParent<PlayerHeroKnightAnimator>();
        if (heroAnimator == null) heroAnimator = GetComponentInChildren<PlayerHeroKnightAnimator>();
    }

    private void OnEnable()
    {
        if (basicAttackHitbox != null)
            basicAttackHitbox.OnHit += HandleBasicAttackHit;
    }

    private void OnDisable()
    {
        if (basicAttackHitbox != null)
            basicAttackHitbox.OnHit -= HandleBasicAttackHit;
    }

    private void Update()
    {
        if (input != null && input.AttackPressed)
        {
            TryBasicAttack();
            return;
        }

        if (movement != null && movement.IsInputLocked && !isWaitingComboInput)
            return;
    }

    // 기본 공격은 짧은 판정 시간만 열고 닫아서 빠른 전투 템포를 만든다.
    public void TryBasicAttack()
    {
        if (basicAttack == null || basicAttackHitbox == null)
            return;

        if (isAttacking)
            return;

        if (!isWaitingComboInput)
            basicAttackStep = 0;

        StartBasicAttack();
    }

    private void StartBasicAttack()
    {
        isWaitingComboInput = false;
        comboVersion++;
        StartCoroutine(CoBasicAttack());
    }

    private IEnumerator CoBasicAttack()
    {
        isAttacking = true;

        int stepIndex = basicAttackStep % 2;
        activeAttackStepIndex = stepIndex;
        string attackTrigger = stepIndex == 0 ? "Attack1" : "Attack2";
        basicAttackStep++;

        movement?.LockMovementFor(attackMoveLockTime);

        PlayAttackAnimation(attackTrigger);

        SoundManager.Instance?.PlayWeaponSwing();

        if (useAnimationEventHitTiming)
        {
            yield return new WaitForSeconds(attackMotionTime);
            CloseBasicAttackHitbox();
        }
        else
        {
            yield return new WaitForSeconds(attackStartupTime);

            OpenBasicAttackHitbox();
            yield return new WaitForSeconds(basicAttack.activeTime);
            CloseBasicAttackHitbox();

            yield return new WaitForSeconds(Mathf.Max(0f, attackMotionTime - attackStartupTime - basicAttack.activeTime));
        }

        isAttacking = false;

        int waitVersion = comboVersion;
        isWaitingComboInput = true;
        yield return new WaitForSeconds(comboInputWindow);

        if (comboVersion == waitVersion)
        {
            isWaitingComboInput = false;
            basicAttackStep = 0;
        }
    }

    // Animation Event에서 검이 실제로 지나가는 프레임에 호출한다.
    public void OpenBasicAttackHitbox()
    {
        if (basicAttackHitbox == null || basicAttack == null)
            return;

        movement?.ApplyAttackStep(activeAttackStepIndex == 0 ? attack1StepSpeed : attack2StepSpeed, attackStepDuration);
        basicAttackHitbox.Open(Team.Player, basicAttack);
    }

    // Animation Event에서 휘두르기 판정이 끝나는 프레임에 호출한다.
    public void CloseBasicAttackHitbox()
    {
        basicAttackHitbox?.Close();
        movement?.StopAttackStep();
    }

    private void HandleBasicAttackHit(Health target)
    {
        if (target == null || target.GetComponent<BreakableObject>() == null)
            SoundManager.Instance?.PlayRandomBladeHit();

        movement?.StopAttackStep();
    }

    private void PlayAttackAnimation(string attackTrigger)
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        heroAnimator?.HoldAttackMotion(attackMotionTime);
        animator.SetTrigger(attackTrigger);
    }

    public void OpenAttackHitbox()
    {
        OpenBasicAttackHitbox();
    }

    public void CloseAttackHitbox()
    {
        CloseBasicAttackHitbox();
    }

    public void AttackHitStart()
    {
        OpenBasicAttackHitbox();
    }

    public void AttackHitEnd()
    {
        CloseBasicAttackHitbox();
    }
}
