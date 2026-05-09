using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Hitbox basicAttackHitbox;
    [SerializeField] private AttackData basicAttack;
    [SerializeField] private Animator animator;
    [SerializeField] private bool useAnimationEventHitTiming;
    [SerializeField] private float attackMotionTime = 0.42f;
    [SerializeField] private float attackStartupTime = 0.18f;
    [SerializeField] private float comboInputWindow = 0.1f;

    private bool isAttacking;
    private bool isWaitingComboInput;
    private int basicAttackStep;
    private int comboVersion;

    private void Reset()
    {
        input = GetComponent<PlayerInputReader>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (input != null && input.AttackPressed)
            TryBasicAttack();
    }

    // 기본 공격은 짧은 판정 시간만 열고 닫아서 빠른 전투 템포를 만든다.
    public void TryBasicAttack()
    {
        if (isAttacking || basicAttack == null || basicAttackHitbox == null)
            return;

        if (!isWaitingComboInput)
            basicAttackStep = 0;

        isWaitingComboInput = false;
        comboVersion++;
        StartCoroutine(CoBasicAttack());
    }

    private IEnumerator CoBasicAttack()
    {
        isAttacking = true;

        string attackTrigger = basicAttackStep % 2 == 0 ? "Attack1" : "Attack2";
        basicAttackStep++;

        if (animator != null)
            animator.SetTrigger(attackTrigger);

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

        basicAttackHitbox.Open(Team.Player, basicAttack);
    }

    // Animation Event에서 휘두르기 판정이 끝나는 프레임에 호출한다.
    public void CloseBasicAttackHitbox()
    {
        basicAttackHitbox?.Close();
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
