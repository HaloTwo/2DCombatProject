using System.Collections.Generic;
using UnityEngine;

public class ListRendererCache : MonoBehaviour
{
    [SerializeField] private List<SpriteRenderer> renderers = new();

    private readonly Dictionary<SpriteRenderer, Color> originColors = new();

    private void Reset()
    {
        Collect();
    }

    private void Awake()
    {
        if (renderers.Count == 0)
            Collect();

        CacheColors();
    }

    public void Collect()
    {
        renderers.Clear();
        GetComponentsInChildren(true, renderers);
    }

    public void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = color;
        }
    }

    public void Restore()
    {
        foreach (var pair in originColors)
        {
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }
    }

    private void CacheColors()
    {
        originColors.Clear();
        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr != null)
                originColors[sr] = sr.color;
        }
    }
}
