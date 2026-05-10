using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffStatusView : MonoBehaviour
{
    private sealed class Entry
    {
        public BuffItemType Type;
        public RectTransform Root;
        public Image Fill;
        public Text TimeText;
        public float StartTime;
        public float Duration;
        public float EndTime;
    }

    [SerializeField, KoreanLabel("버프 목록 루트")] private RectTransform listRoot;
    [SerializeField, KoreanLabel("슬롯 크기")] private float slotSize = 44f;

    private readonly List<Entry> entries = new();
    private static BuffStatusView instance;
    private static Sprite circleSprite;

    private void Awake()
    {
        instance = this;
        EnsureListRoot();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            float remaining = entry.EndTime - Time.time;

            if (remaining <= 0f)
            {
                if (entry.Root != null)
                    Destroy(entry.Root.gameObject);

                entries.RemoveAt(i);
                continue;
            }

            float ratio = entry.Duration <= 0f ? 0f : Mathf.Clamp01(remaining / entry.Duration);
            if (entry.Fill != null)
                entry.Fill.fillAmount = ratio;

            if (entry.TimeText != null)
                entry.TimeText.text = remaining.ToString("0.0");
        }
    }

    public static void Show(BuffItemType type, float duration)
    {
        EnsureInstance()?.ShowBuff(type, duration);
    }

    private void ShowBuff(BuffItemType type, float duration)
    {
        EnsureListRoot();

        Entry entry = FindEntry(type);
        if (entry == null)
        {
            entry = CreateEntry(type);
            entries.Add(entry);
        }

        entry.StartTime = Time.time;
        entry.Duration = Mathf.Max(0.1f, duration);
        entry.EndTime = entry.StartTime + entry.Duration;
        StartCoroutine(CoPop(entry.Root));
    }

    private Entry FindEntry(BuffItemType type)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Type == type)
                return entries[i];
        }

        return null;
    }

    private Entry CreateEntry(BuffItemType type)
    {
        GameObject go = new GameObject($"{type}BuffSlot");
        go.transform.SetParent(listRoot, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        Image background = go.AddComponent<Image>();
        background.sprite = GetCircleSprite();
        background.color = new Color(0f, 0f, 0f, 0.72f);

        Image icon = CreateImage("Icon", rect, GetBuffColor(type), new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        icon.sprite = GetCircleSprite();

        Image fill = CreateImage("CooldownFill", rect, new Color(0f, 0f, 0f, 0.58f), Vector2.zero, Vector2.one);
        fill.sprite = GetCircleSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 1f;

        Text timeText = new GameObject("TimeText").AddComponent<Text>();
        timeText.transform.SetParent(rect, false);
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeText.fontSize = 14;
        timeText.fontStyle = FontStyle.Bold;
        timeText.alignment = TextAnchor.MiddleCenter;
        timeText.color = Color.white;
        timeText.raycastTarget = false;

        RectTransform textRect = timeText.GetComponent<RectTransform>();
        Stretch(textRect, Vector2.zero, Vector2.one);

        return new Entry { Type = type, Root = rect, Fill = fill, TimeText = timeText };
    }

    private void EnsureListRoot()
    {
        if (listRoot != null)
            return;

        listRoot = GetComponent<RectTransform>();
        if (listRoot == null)
            listRoot = gameObject.AddComponent<RectTransform>();

        HorizontalLayoutGroup layout = gameObject.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
        }
    }

    private IEnumerator CoPop(Transform target)
    {
        if (target == null)
            yield break;

        target.localScale = Vector3.one * 1.16f;
        yield return new WaitForSecondsRealtime(0.08f);
        target.localScale = Vector3.one;
    }

    private static BuffStatusView EnsureInstance()
    {
        if (instance != null)
            return instance;

        Transform parent = FindCombatHud();
        if (parent == null)
            return null;

        BuffStatusView existing = parent.GetComponentInChildren<BuffStatusView>(true);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root = new GameObject("BuffStatusView");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(286f, 74f);
        rect.sizeDelta = new Vector2(160f, 48f);

        instance = root.AddComponent<BuffStatusView>();
        return instance;
    }

    private static Transform FindCombatHud()
    {
        GameObject combatHud = GameObject.Find("CombatHUD");
        if (combatHud != null)
            return combatHud.transform;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return image;
    }

    private static void Stretch(RectTransform target, Vector2 anchorMin, Vector2 anchorMax)
    {
        target.anchorMin = anchorMin;
        target.anchorMax = anchorMax;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private static Color GetBuffColor(BuffItemType type)
    {
        return type switch
        {
            BuffItemType.MoveSpeed => new Color(0.22f, 1f, 0.46f, 0.95f),
            BuffItemType.AttackPower => new Color(1f, 0.18f, 0.18f, 0.95f),
            BuffItemType.FocusGauge => new Color(1f, 0.18f, 0.85f, 0.95f),
            _ => Color.white
        };
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 2) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
