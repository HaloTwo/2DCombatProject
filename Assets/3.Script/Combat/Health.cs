using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private Team team = Team.Enemy;
    [SerializeField] private float maxHp = 30f;
    [SerializeField] private float invincibleTime = 0.15f;

    private float currentHp;
    private float invincibleEndTime;

    public Team Team => team;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => currentHp <= 0f;

    public event Action<Health, DamageInfo> OnDamaged;
    public event Action<Health> OnChanged;
    public event Action<Health> OnDead;
    public static event Action<Health> OnAnyDead;

    private void Awake()
    {
        currentHp = maxHp;
    }

    // 웨이브 스폰이나 풀 재사용 시 체력과 무적 상태를 초기화한다.
    public void ResetHealth()
    {
        currentHp = maxHp;
        invincibleEndTime = 0f;
        OnChanged?.Invoke(this);
    }

    // 모든 공격 판정은 이 함수로 들어오며, 팀 체크와 사망 이벤트를 한 곳에서 관리한다.
    public bool TakeDamage(DamageInfo info)
    {
        if (IsDead) return false;
        if (info.AttackerTeam == team) return false;
        if (Time.time < invincibleEndTime) return false;

        currentHp = Mathf.Max(0f, currentHp - info.Damage);
        invincibleEndTime = Time.time + invincibleTime;

        OnDamaged?.Invoke(this, info);
        OnChanged?.Invoke(this);

        if (currentHp <= 0f)
        {
            OnDead?.Invoke(this);
            OnAnyDead?.Invoke(this);
        }

        return true;
    }

    // 버프 아이템이나 회복 연출에서 사용한다. 사망한 대상은 회복하지 않는다.
    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnChanged?.Invoke(this);
    }

    public void SetInvincibleFor(float duration)
    {
        if (duration <= 0f)
            return;

        invincibleEndTime = Mathf.Max(invincibleEndTime, Time.time + duration);
    }
}
