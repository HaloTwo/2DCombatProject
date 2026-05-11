using UnityEngine;

public class FocusBackgroundDimmer : Singleton<FocusBackgroundDimmer>
{
    [SerializeField, KoreanLabel("어둡게 만들 루트들")] private Transform[] targetRoots;
    [SerializeField, KoreanLabel("어두워진 색상 배율")] private Color dimMultiplier = new Color(0.18f, 0.18f, 0.22f, 1f);

    private SpriteRenderer[] renderers;
    private Color[] originColors;
    private bool dimmed;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        CacheRenderers();
    }

    // 포커스 상태에서는 Map/BG, Map/FG 같은 환경 스프라이트만 어둡게 만든다.
    // 플레이어, 적, 투사체, 이펙트 렌더러는 대상 루트에 넣지 않으면 영향을 받지 않는다.
    public void Show()
    {
        CacheRenderers();

        if (dimmed)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color origin = originColors[i];
            renderers[i].color = new Color(
                origin.r * dimMultiplier.r,
                origin.g * dimMultiplier.g,
                origin.b * dimMultiplier.b,
                origin.a * dimMultiplier.a
            );
        }

        dimmed = true;
    }

    public void Hide()
    {
        if (!dimmed || renderers == null || originColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originColors[i];
        }

        dimmed = false;
    }

    private void CacheRenderers()
    {
        if (renderers != null && renderers.Length > 0)
            return;

        if (targetRoots == null || targetRoots.Length == 0)
        {
            renderers = System.Array.Empty<SpriteRenderer>();
            originColors = System.Array.Empty<Color>();
            return;
        }

        int count = 0;
        for (int i = 0; i < targetRoots.Length; i++)
        {
            if (targetRoots[i] == null)
                continue;

            count += targetRoots[i].GetComponentsInChildren<SpriteRenderer>(true).Length;
        }

        renderers = new SpriteRenderer[count];
        originColors = new Color[count];

        int index = 0;
        for (int i = 0; i < targetRoots.Length; i++)
        {
            if (targetRoots[i] == null)
                continue;

            SpriteRenderer[] found = targetRoots[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < found.Length; j++)
            {
                renderers[index] = found[j];
                originColors[index] = found[j].color;
                index++;
            }
        }
    }

    protected override void OnDestroy()
    {
        Hide();
        base.OnDestroy();
    }
}
