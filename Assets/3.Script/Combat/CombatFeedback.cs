using System.Collections;
using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ListRendererCache rendererCache;

    [Header("Knockback")]
    [SerializeField] private bool applyPhysicalKnockback = true;
    [SerializeField] private float knockbackMultiplier = 0.1f;
    [SerializeField] private float maxKnockbackX = 1.2f;
    [SerializeField] private float maxKnockbackY = 0.7f;

    [Header("Damage Text")]
    [SerializeField] private bool spawnDamageText;
    [SerializeField] private Color damageTextColor = new Color(0.88f, 0.98f, 1f, 1f);
    [SerializeField] private Vector3 damageTextOffset = new Vector3(0f, 0.45f, 0f);

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

    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        ApplyKnockback(info);

        if (info.HitStopTime > 0f)
            GlobalHitStop.Play(info.HitStopTime);

        CameraShake.ShakeDefault();
        SpawnDamageText(info);
    }

    private void ApplyKnockback(DamageInfo info)
    {
        if (!applyPhysicalKnockback || rb == null)
            return;

        Vector2 knockback = info.Knockback * knockbackMultiplier;

        knockback.x = Mathf.Clamp(knockback.x, -maxKnockbackX, maxKnockbackX);
        knockback.y = Mathf.Clamp(knockback.y, -maxKnockbackY, maxKnockbackY);

        rb.linearVelocity = new Vector2(knockback.x, knockback.y);
    }

    private void SpawnDamageText(DamageInfo info)
    {
        if (!spawnDamageText)
            return;

        Vector3 position = (Vector3)info.HitPoint + damageTextOffset;
        GameObject go = new GameObject("DamageText");
        go.transform.position = position;

        TextMesh text = go.AddComponent<TextMesh>();
        text.text = Mathf.RoundToInt(info.Damage).ToString();
        text.fontSize = 36;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = GetReadableDamageTextColor();
        text.characterSize = 0.08f;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 80;

        go.AddComponent<WorldDamageText>();
    }

    private Color GetReadableDamageTextColor()
    {
        bool looksYellow = damageTextColor.r > 0.85f && damageTextColor.g > 0.65f && damageTextColor.b < 0.35f;
        return looksYellow ? new Color(0.88f, 0.98f, 1f, 1f) : damageTextColor;
    }
}

public static class GlobalHitStop
{
    private const float HitStopScale = 0.05f;

    private static Runner runner;
    private static int activeCount;
    private static float restoreScale = 1f;

    // 피격 대상이 죽거나 비활성화되어도 히트스톱 복구가 끊기지 않도록 별도 러너에서 실행한다.
    public static void Play(float duration)
    {
        if (duration <= 0f)
            return;

        EnsureRunner();
        runner.StartCoroutine(CoHitStop(duration));
    }

    private static IEnumerator CoHitStop(float duration)
    {
        if (activeCount == 0)
        {
            restoreScale = Time.timeScale <= HitStopScale + 0.001f ? 1f : Time.timeScale;
            Time.timeScale = HitStopScale;
        }

        activeCount++;
        yield return new WaitForSecondsRealtime(duration);
        activeCount = Mathf.Max(0, activeCount - 1);

        if (activeCount == 0)
            Time.timeScale = restoreScale <= 0f ? 1f : restoreScale;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        GameObject go = new GameObject("[GlobalHitStop]");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    private sealed class Runner : MonoBehaviour
    {
        private void OnDestroy()
        {
            if (runner == this)
            {
                activeCount = 0;
                Time.timeScale = restoreScale <= 0f ? 1f : restoreScale;
                runner = null;
            }
        }
    }
}
