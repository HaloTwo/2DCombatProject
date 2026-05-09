using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Hitbox basicAttackHitbox;
    [SerializeField] private AttackData basicAttack;
    [SerializeField] private Animator animator;

    private bool isAttacking;
    private int basicAttackStep;

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

        StartCoroutine(CoBasicAttack());
    }

    private IEnumerator CoBasicAttack()
    {
        isAttacking = true;
        string attackTrigger = basicAttackStep % 2 == 0 ? "Attack1" : "Attack2";
        basicAttackStep++;

        if (animator != null)
            animator.SetTrigger(attackTrigger);

        basicAttackHitbox.Open(Team.Player, basicAttack);
        yield return new WaitForSeconds(basicAttack.activeTime);
        basicAttackHitbox.Close();

        yield return new WaitForSeconds(basicAttack.cooldown);
        isAttacking = false;
    }
}
