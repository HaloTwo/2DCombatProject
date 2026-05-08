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
    public event Action<Health> OnDead;

    private void Awake()
    {
        currentHp = maxHp;
    }

    // 스폰/웨이브 재사용 시 체력과 무적 상태를 초기화한다.
    public void ResetHealth()
    {
        currentHp = maxHp;
        invincibleEndTime = 0f;
    }

    // 모든 공격 판정은 이 함수로 들어오게 해서 팀 체크와 사망 처리를 한 곳에서 관리한다.
    public bool TakeDamage(DamageInfo info)
    {
        if (IsDead) return false;
        if (info.AttackerTeam == team) return false;
        if (Time.time < invincibleEndTime) return false;

        currentHp = Mathf.Max(0f, currentHp - info.Damage);
        invincibleEndTime = Time.time + invincibleTime;

        OnDamaged?.Invoke(this, info);

        if (currentHp <= 0f)
            OnDead?.Invoke(this);

        return true;
    }
}
