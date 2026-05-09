using System.Collections;
using UnityEngine;

public class PlayerDashEffect : MonoBehaviour
{
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private Color ghostColor = new Color(0.55f, 0.85f, 1f, 0.48f);
    [SerializeField] private float ghostInterval = 0.035f;
    [SerializeField] private float ghostLifeTime = 0.22f;
    [SerializeField] private float dashAlpha = 0.62f;

    private bool wasDashing;
    private float nextGhostTime;
    private Color originColor = Color.white;

    private void Reset()
    {
        movement = GetComponent<PlayerMovement2D>();
        sourceRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (sourceRenderer == null) sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        if (sourceRenderer != null) originColor = sourceRenderer.color;
    }

    private void Update()
    {
        if (movement == null || sourceRenderer == null)
            return;

        if (movement.IsDashing)
        {
            if (!wasDashing)
                BeginDashEffect();

            if (Time.time >= nextGhostTime)
                SpawnGhost();
        }
        else if (wasDashing)
        {
            EndDashEffect();
        }

        wasDashing = movement.IsDashing;
    }

    // 대시 시작 순간 본체를 살짝 투명하게 만들어 속도감을 준다.
    private void BeginDashEffect()
    {
        originColor = sourceRenderer.color;
        sourceRenderer.color = new Color(originColor.r, originColor.g, originColor.b, dashAlpha);
        nextGhostTime = 0f;
    }

    // 대시가 끝나면 본체 색을 원래대로 되돌린다.
    private void EndDashEffect()
    {
        sourceRenderer.color = originColor;
    }

    // 현재 스프라이트를 복사한 잔상을 생성하고 짧게 페이드아웃한다.
    private void SpawnGhost()
    {
        nextGhostTime = Time.time + ghostInterval;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = sourceRenderer.transform.position;
        ghost.transform.rotation = sourceRenderer.transform.rotation;
        ghost.transform.localScale = sourceRenderer.transform.lossyScale;

        SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceRenderer.sprite;
        renderer.flipX = sourceRenderer.flipX;
        renderer.flipY = sourceRenderer.flipY;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        renderer.color = ghostColor;

        StartCoroutine(CoFadeGhost(renderer, ghost));
    }

    private IEnumerator CoFadeGhost(SpriteRenderer renderer, GameObject ghost)
    {
        float elapsed = 0f;
        Color startColor = renderer.color;

        while (elapsed < ghostLifeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostLifeTime);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghost);
    }
}
