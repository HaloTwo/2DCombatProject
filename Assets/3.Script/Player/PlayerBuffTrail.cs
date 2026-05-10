using System.Collections;
using UnityEngine;

public class PlayerBuffTrail : MonoBehaviour
{
    [SerializeField, KoreanLabel("잔상 기준 렌더러")] private SpriteRenderer sourceRenderer;
    [SerializeField, KoreanLabel("잔상 색상")] private Color speedTrailColor = new Color(0.35f, 1f, 0.65f, 0.62f);
    [SerializeField, KoreanLabel("공격력 버프 틴트")] private Color powerTintColor = new Color(1f, 0.72f, 0.68f, 1f);
    [SerializeField, KoreanLabel("공격력 잔상 색상")] private Color powerTrailColor = new Color(1f, 0.12f, 0.08f, 0.56f);
    [SerializeField, KoreanLabel("잔상 생성 간격")] private float spawnInterval = 0.055f;
    [SerializeField, KoreanLabel("잔상 사라지는 시간")] private float fadeTime = 0.22f;

    private Coroutine trailRoutine;
    private Coroutine powerRoutine;

    private void Awake()
    {
        if (sourceRenderer == null)
        {
            PlayerMovement2D movement = GetComponent<PlayerMovement2D>();
            sourceRenderer = movement != null ? movement.VisualRenderer : GetComponentInChildren<SpriteRenderer>();
        }
    }

    // 이동속도 버프가 켜져 있는 동안 대시처럼 플레이어 뒤에 잔상을 남긴다.
    public void PlaySpeedTrail(float duration)
    {
        if (duration <= 0f)
            return;

        if (sourceRenderer == null)
        {
            PlayerMovement2D movement = GetComponent<PlayerMovement2D>();
            sourceRenderer = movement != null ? movement.VisualRenderer : GetComponentInChildren<SpriteRenderer>();
        }

        if (sourceRenderer == null)
            return;

        if (trailRoutine != null)
            StopCoroutine(trailRoutine);

        trailRoutine = StartCoroutine(CoSpeedTrail(duration));
    }

    // 공격력 버프 동안 본체는 연하게 붉게 틴트하고, 뒤쪽에는 붉은 전투 잔상만 남긴다.
    public void PlayPowerAura(float duration)
    {
        if (duration <= 0f)
            return;

        if (sourceRenderer == null)
        {
            PlayerMovement2D movement = GetComponent<PlayerMovement2D>();
            sourceRenderer = movement != null ? movement.VisualRenderer : GetComponentInChildren<SpriteRenderer>();
        }

        if (sourceRenderer == null)
            return;

        if (powerRoutine != null)
            StopCoroutine(powerRoutine);

        powerRoutine = StartCoroutine(CoPowerAura(duration));
    }

    private IEnumerator CoSpeedTrail(float duration)
    {
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            SpawnGhost(speedTrailColor);
            yield return new WaitForSeconds(spawnInterval);
        }

        trailRoutine = null;
    }

    private IEnumerator CoPowerAura(float duration)
    {
        Color cachedOrigin = sourceRenderer.color;
        sourceRenderer.color = Color.Lerp(cachedOrigin, powerTintColor, 0.45f);
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            SpawnGhost(powerTrailColor);
            yield return new WaitForSeconds(spawnInterval * 1.4f);
        }

        if (sourceRenderer != null)
            sourceRenderer.color = cachedOrigin;

        powerRoutine = null;
    }

    private void SpawnGhost(Color color)
    {
        GameObject ghost = new GameObject("BuffTrail");
        ghost.transform.position = sourceRenderer.transform.position;
        ghost.transform.rotation = sourceRenderer.transform.rotation;
        ghost.transform.localScale = sourceRenderer.transform.lossyScale;

        SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceRenderer.sprite;
        renderer.flipX = sourceRenderer.flipX;
        renderer.flipY = sourceRenderer.flipY;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        renderer.color = color;

        StartCoroutine(CoFadeGhost(renderer));
    }

    private IEnumerator CoFadeGhost(SpriteRenderer ghostRenderer)
    {
        if (ghostRenderer == null)
            yield break;

        Color startColor = ghostRenderer.color;
        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeTime);
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            ghostRenderer.color = color;
            yield return null;
        }

        Destroy(ghostRenderer.gameObject);
    }
}
