using System.Collections;
using UnityEngine;

public class PlayerDamageBuff : MonoBehaviour
{
    [SerializeField, KoreanLabel("기본 데미지 배율")] private float baseMultiplier = 1f;

    private Coroutine buffRoutine;

    public static PlayerDamageBuff Instance { get; private set; }
    public float CurrentMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        Instance = this;
        CurrentMultiplier = Mathf.Max(0.1f, baseMultiplier);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Power Orb가 호출한다. 기존 공격력 버프가 남아 있으면 새 지속 시간과 배율로 갱신한다.
    public void SetTemporaryDamageMultiplier(float multiplier, float duration)
    {
        if (duration <= 0f)
            return;

        if (buffRoutine != null)
            StopCoroutine(buffRoutine);

        buffRoutine = StartCoroutine(CoDamageBuff(Mathf.Max(0.1f, multiplier), duration));
    }

    public static float ModifyPlayerDamage(float damage)
    {
        return damage * (Instance != null ? Instance.CurrentMultiplier : 1f);
    }

    private IEnumerator CoDamageBuff(float multiplier, float duration)
    {
        CurrentMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        CurrentMultiplier = Mathf.Max(0.1f, baseMultiplier);
        buffRoutine = null;
    }
}
