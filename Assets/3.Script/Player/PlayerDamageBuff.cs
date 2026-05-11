using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamageBuff : Singleton<PlayerDamageBuff>
{
    [SerializeField, KoreanLabel("기본 데미지 배율")] private float baseMultiplier = 1f;

    private readonly List<DamageBuffEntry> activeBuffs = new();
    private Coroutine buffRoutine;

    public float CurrentMultiplier { get; private set; } = 1f;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        CurrentMultiplier = Mathf.Max(0.1f, baseMultiplier);
    }

    // Power Orb에서 호출한다. 각 공격력 버프는 독립 지속 시간을 갖고, 살아있는 버프 중 가장 높은 배율을 적용한다.
    public void SetTemporaryDamageMultiplier(float multiplier, float duration)
    {
        if (duration <= 0f)
            return;

        activeBuffs.Add(new DamageBuffEntry(Mathf.Max(0.1f, multiplier), Time.time + duration));
        RefreshMultiplier();

        if (buffRoutine != null)
            return;

        buffRoutine = StartCoroutine(CoDamageBuff());
    }

    public static float ModifyPlayerDamage(float damage)
    {
        return damage * (Instance != null ? Instance.CurrentMultiplier : 1f);
    }

    private IEnumerator CoDamageBuff()
    {
        while (activeBuffs.Count > 0)
        {
            RefreshMultiplier();
            yield return null;
        }

        CurrentMultiplier = Mathf.Max(0.1f, baseMultiplier);
        buffRoutine = null;
    }

    private void RefreshMultiplier()
    {
        float multiplier = Mathf.Max(0.1f, baseMultiplier);
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeBuffs[i].EndTime)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

            multiplier = Mathf.Max(multiplier, activeBuffs[i].Multiplier);
        }

        CurrentMultiplier = multiplier;
    }

    private readonly struct DamageBuffEntry
    {
        public readonly float Multiplier;
        public readonly float EndTime;

        public DamageBuffEntry(float multiplier, float endTime)
        {
            Multiplier = multiplier;
            EndTime = endTime;
        }
    }
}
