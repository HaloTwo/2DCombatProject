using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffStatusView : MonoBehaviour
{
    [SerializeField, KoreanLabel("버프 목록 루트")] private RectTransform listRoot;
    [SerializeField, KoreanLabel("버프 슬롯 프리팹")] private BuffStatusSlotUI slotPrefab;
    [SerializeField, KoreanLabel("슬롯 크기")] private float slotSize = 70f;
    [SerializeField, KoreanLabel("슬롯 간격")] private float slotSpacing = 6f;
    [SerializeField, KoreanLabel("초기 풀 크기")] private int prewarmCount = 4;

    private readonly List<BuffStatusSlotUI> activeSlots = new();
    private readonly Queue<BuffStatusSlotUI> pooledSlots = new();
    private static BuffStatusView instance;
    private static Sprite circleSprite;

    private void Awake()
    {
        instance = this;
        EnsureListRoot();
        Prewarm();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            BuffStatusSlotUI slot = activeSlots[i];
            if (slot == null || slot.UpdateView())
                continue;

            activeSlots.RemoveAt(i);
            ReleaseSlot(slot);
        }
    }

    public static void Show(BuffItemType type, float duration)
    {
        EnsureInstance()?.ShowBuff(type, duration);
    }

    private void ShowBuff(BuffItemType type, float duration)
    {
        EnsureListRoot();

        // 같은 버프를 여러 번 먹어도 슬롯을 합치지 않고, 각 슬롯이 자기 지속 시간으로 따로 돈다.
        BuffStatusSlotUI slot = GetSlot();
        slot.transform.SetParent(listRoot, false);
        slot.Initialize(type, duration, slotSize, GetCircleSprite(), GetBuffColor(type));
        activeSlots.Add(slot);

        StartCoroutine(CoPop(slot.transform));
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
    }

    private void Prewarm()
    {
        int count = Mathf.Max(0, prewarmCount);
        for (int i = 0; i < count; i++)
            ReleaseSlot(CreateSlot());
    }

    private BuffStatusSlotUI GetSlot()
    {
        while (pooledSlots.Count > 0)
        {
            BuffStatusSlotUI slot = pooledSlots.Dequeue();
            if (slot != null)
                return slot;
        }

        return CreateSlot();
    }

    private BuffStatusSlotUI CreateSlot()
    {
        BuffStatusSlotUI slot;
        if (slotPrefab != null)
        {
            slot = Instantiate(slotPrefab, listRoot);
        }
        else
        {
            GameObject go = new GameObject("BuffStatusSlot");
            go.transform.SetParent(listRoot, false);
            go.AddComponent<RectTransform>();
            slot = go.AddComponent<BuffStatusSlotUI>();
        }

        slot.Initialize(BuffItemType.FocusGauge, 0.1f, slotSize, GetCircleSprite(), Color.white);
        return slot;
    }

    private void ReleaseSlot(BuffStatusSlotUI slot)
    {
        if (slot == null)
            return;

        slot.ClearView();
        slot.gameObject.SetActive(false);
        slot.transform.SetParent(listRoot, false);
        pooledSlots.Enqueue(slot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
    }

    private void EnsureListRoot()
    {
        if (listRoot == null)
            listRoot = GetComponent<RectTransform>();
        if (listRoot == null)
            listRoot = gameObject.AddComponent<RectTransform>();

        ConfigureLayout(listRoot.gameObject);
    }

    // 버프 슬롯이 여러 개 생겨도 겹치거나 늘어나지 않도록 UGUI 자동 정렬을 고정한다.
    private void ConfigureLayout(GameObject target)
    {
        HorizontalLayoutGroup layout = target.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = target.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = target.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private IEnumerator CoPop(Transform target)
    {
        if (target == null)
            yield break;

        target.localScale = Vector3.one * 1.16f;
        yield return new WaitForSecondsRealtime(0.08f);

        if (target != null)
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
        rect.anchoredPosition = new Vector2(530f, 6f);
        rect.sizeDelta = new Vector2(0f, 90f);

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

    private static Color GetBuffColor(BuffItemType type)
    {
        return type switch
        {
            BuffItemType.MoveSpeed => new Color(0.22f, 1f, 0.46f, 0.95f),
            BuffItemType.AttackPower => new Color(1f, 0.18f, 0.18f, 0.95f),
            BuffItemType.FocusGauge => new Color(1f, 0.18f, 0.85f, 0.95f),
            BuffItemType.Invincible => new Color(0.45f, 0.9f, 1f, 0.95f),
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
