using System.Collections.Generic;
using UnityEngine;

public class SpritePool : Singleton<SpritePool>
{
    private readonly Dictionary<string, Sprite> cache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(SpritePool));
        DontDestroyOnLoad(go);
        go.AddComponent<SpritePool>();
    }

    // Resources/Sprite/{name} 경로에서 스프라이트를 한 번만 로드하고 이후에는 캐시로 재사용한다.
    public Sprite Get(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (cache.TryGetValue(name, out Sprite sprite))
            return sprite;

        sprite = Resources.Load<Sprite>($"Sprite/{name}");
        if (sprite == null)
        {
            Debug.LogWarning($"[SpritePool] Resources/Sprite/{name} 스프라이트를 찾지 못했습니다.");
            return null;
        }

        cache.Add(name, sprite);
        return sprite;
    }
}
