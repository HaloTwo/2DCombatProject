using System.Collections;
using UnityEngine;

public class FocusGaugeOrb : MonoBehaviour, IPoolable
{
    private const float DefaultSize = 0.14f;

    private static Sprite generatedOrbSprite;

    [SerializeField, KoreanLabel("스프라이트 렌더러")] private SpriteRenderer spriteRenderer;
    [SerializeField, KoreanLabel("도착 판정 거리")] private float arriveDistance = 0.08f;

    private HUDView hudView;
    private Transform target;
    private Renderer targetRenderer;
    private Vector3 targetOffset;
    private float chargeAmount;
    private float flyDuration;
    private float arcHeight;
    private float startDelay;
    private Coroutine flyRoutine;

    private void Reset()
    {
        BindRenderer();
    }

    private void Awake()
    {
        BindRenderer();
    }

    public void Play(
        Vector3 startPosition,
        Transform target,
        Renderer targetRenderer,
        Vector3 targetOffset,
        HUDView hudView,
        float chargeAmount,
        Color color,
        float flyDuration,
        float arcHeight,
        float startDelay)
    {
        BindRenderer();
        transform.position = startPosition;
        transform.localScale = Vector3.one;

        if (spriteRenderer != null)
            spriteRenderer.color = color;

        Initialize(target, targetRenderer, targetOffset, hudView, chargeAmount, flyDuration, arcHeight, startDelay);
    }

    public void OnSpawned()
    {
        BindRenderer();
    }

    public void OnDespawned()
    {
        if (flyRoutine != null)
        {
            StopCoroutine(flyRoutine);
            flyRoutine = null;
        }

        hudView = null;
        target = null;
        targetRenderer = null;
        targetOffset = Vector3.zero;
        chargeAmount = 0f;
    }

    private static Sprite GetGeneratedOrbSprite()
    {
        if (generatedOrbSprite != null)
            return generatedOrbSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance01 = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance01);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        generatedOrbSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size / DefaultSize);
        return generatedOrbSprite;
    }

    private void Initialize(Transform target, Renderer targetRenderer, Vector3 targetOffset, HUDView hudView, float chargeAmount, float flyDuration, float arcHeight, float startDelay)
    {
        this.target = target;
        this.targetRenderer = targetRenderer;
        this.targetOffset = targetOffset;
        this.hudView = hudView;
        this.chargeAmount = chargeAmount;
        this.flyDuration = Mathf.Max(0.05f, flyDuration);
        this.arcHeight = arcHeight;
        this.startDelay = Mathf.Max(0f, startDelay);

        if (flyRoutine != null)
            StopCoroutine(flyRoutine);

        flyRoutine = StartCoroutine(CoFly());
    }

    // 죽은 적 위치에서 출발해 플레이어 중심으로 빨려 들어간 뒤 실제 게이지를 올린다.
    private IEnumerator CoFly()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < flyDuration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 end = GetTargetPosition();
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
            transform.position = Vector3.Lerp(start, end, eased) + arc;
            transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.55f, t);

            if (Vector3.Distance(transform.position, end) <= arriveDistance)
                break;

            yield return null;
        }

        hudView?.AddFocusGauge(chargeAmount);
        flyRoutine = null;
        Release();
    }

    private Vector3 GetTargetPosition()
    {
        if (targetRenderer != null)
            return targetRenderer.bounds.center;

        return target != null ? target.TransformPoint(targetOffset) : transform.position;
    }

    private void BindRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedOrbSprite();

        spriteRenderer.sortingOrder = 95;
    }

    private void Release()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}
