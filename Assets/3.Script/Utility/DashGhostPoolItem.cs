using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DashGhostPoolItem : MonoBehaviour, IPoolable
{
    [SerializeField, KoreanLabel("잔상 렌더러")] private SpriteRenderer spriteRenderer;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 대시/스킬 대시가 남기는 잔상 한 장을 초기화한다.
    // 매번 GameObject를 만들지 않고 풀에서 꺼내 재사용하기 위해 원본 렌더러의 현재 프레임만 복사한다.
    public void Play(SpriteRenderer source, Color color, float lifeTime)
    {
        if (source == null || spriteRenderer == null)
            return;

        transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        transform.localScale = source.transform.lossyScale;

        spriteRenderer.sprite = source.sprite;
        spriteRenderer.flipX = source.flipX;
        spriteRenderer.flipY = source.flipY;
        spriteRenderer.sortingLayerID = source.sortingLayerID;
        spriteRenderer.sortingOrder = source.sortingOrder - 1;
        spriteRenderer.color = color;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CoFade(Mathf.Max(0.03f, lifeTime), color));
    }

    public void OnSpawned()
    {
    }

    public void OnDespawned()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = null;
    }

    private IEnumerator CoFade(float lifeTime, Color startColor)
    {
        float elapsed = 0f;
        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / lifeTime);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}
