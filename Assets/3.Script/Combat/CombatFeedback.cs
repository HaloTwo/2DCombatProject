using System.Collections;
using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ListRendererCache rendererCache;

    private Coroutine flashRoutine;

    private void Reset()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        rendererCache = GetComponent<ListRendererCache>();
    }

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rendererCache == null) rendererCache = GetComponent<ListRendererCache>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    // 피격 결과에 맞춰 넉백, 히트스톱, 피격 플래시, 카메라 쉐이크를 실행한다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(info.Knockback.x, info.Knockback.y);

        if (info.HitStopTime > 0f)
            StartCoroutine(CoHitStop(info.HitStopTime));

        CameraShake.ShakeDefault();

        if (rendererCache != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(CoFlash());
        }
    }

    // 짧은 전역 슬로우로 타격 순간을 강조한다. 추후 TimeController로 중앙화하면 중첩 제어가 더 쉬워진다.
    private IEnumerator CoHitStop(float duration)
    {
        float originScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originScale;
    }

    private IEnumerator CoFlash()
    {
        rendererCache.SetColor(Color.white);
        yield return new WaitForSeconds(0.08f);
        rendererCache.Restore();
    }
}
