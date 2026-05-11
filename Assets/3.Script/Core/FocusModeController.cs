using System;
using System.Collections;
using UnityEngine;

public sealed class FocusModeController : MonoBehaviour
{
    private static FocusModeController instance;
    private Coroutine focusRoutine;
    private Coroutine previewRoutine;

    public static bool IsActive { get; private set; }
    public static float EnemySpeedMultiplier { get; private set; } = 1f;
    public static float ProjectileSpeedMultiplier { get; private set; } = 1f;

    public static event Action<bool> OnFocusChanged;
    public static event Action<float> OnFocusProgressChanged;

    public static void BeginPreviewSlow(float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        if (IsActive)
            return;

        EnemySpeedMultiplier = Mathf.Clamp(enemySpeedMultiplier, 0.05f, 1f);
        ProjectileSpeedMultiplier = Mathf.Clamp(projectileSpeedMultiplier, 0.05f, 1f);
    }

    public static void EndPreviewSlow()
    {
        if (IsActive)
            return;

        EnemySpeedMultiplier = 1f;
        ProjectileSpeedMultiplier = 1f;
    }

    // 짧은 대시공격/연출용 슬로우다. 포커스 모드가 켜져 있으면 포커스 값을 건드리지 않는다.
    public static void PlayBriefPreviewSlow(float duration, float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        if (duration <= 0f || IsActive)
            return;

        EnsureInstance();

        if (instance.previewRoutine != null)
            instance.StopCoroutine(instance.previewRoutine);

        instance.previewRoutine = instance.StartCoroutine(
            instance.CoPreviewSlow(duration, enemySpeedMultiplier, projectileSpeedMultiplier)
        );
    }

    public static void Activate(float duration, float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        if (duration <= 0f)
            return;

        EnsureInstance();

        if (instance.focusRoutine != null)
            instance.StopCoroutine(instance.focusRoutine);

        instance.focusRoutine = instance.StartCoroutine(
            instance.CoFocus(duration, enemySpeedMultiplier, projectileSpeedMultiplier)
        );
    }

    public static void Stop()
    {
        if (instance != null && instance.focusRoutine != null)
        {
            instance.StopCoroutine(instance.focusRoutine);
            instance.focusRoutine = null;
        }

        if (instance != null && instance.previewRoutine != null)
        {
            instance.StopCoroutine(instance.previewRoutine);
            instance.previewRoutine = null;
        }

        Restore();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("[FocusModeController]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FocusModeController>();
    }

    private IEnumerator CoFocus(float duration, float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        IsActive = true;
        EnemySpeedMultiplier = Mathf.Clamp(enemySpeedMultiplier, 0.05f, 1f);
        ProjectileSpeedMultiplier = Mathf.Clamp(projectileSpeedMultiplier, 0.05f, 1f);

        OnFocusChanged?.Invoke(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(1f - elapsed / duration);
            OnFocusProgressChanged?.Invoke(normalized);

            yield return null;
        }

        focusRoutine = null;
        Restore();
    }

    private IEnumerator CoPreviewSlow(float duration, float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        BeginPreviewSlow(enemySpeedMultiplier, projectileSpeedMultiplier);

        float endTime = Time.unscaledTime + duration;
        while (Time.unscaledTime < endTime && !IsActive)
            yield return null;

        previewRoutine = null;
        EndPreviewSlow();
    }

    private static void Restore()
    {
        IsActive = false;
        EnemySpeedMultiplier = 1f;
        ProjectileSpeedMultiplier = 1f;

        OnFocusProgressChanged?.Invoke(0f);
        OnFocusChanged?.Invoke(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Restore();
        }
    }
}
