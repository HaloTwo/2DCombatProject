using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Text keyText;
    [SerializeField] private Text skillText;
    [SerializeField] private Image background;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private Text cooldownText;

    private SkillSlotBarUI owner;
    private int slotIndex;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originPosition;
    private static Sprite circleSprite;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (background == null)
            background = GetComponent<Image>();

        EnsureCircularView();
    }

    public void Bind(SkillSlotBarUI newOwner, int newSlotIndex)
    {
        owner = newOwner;
        slotIndex = newSlotIndex;
    }

    public void SetLabel(string key, string skill)
    {
        if (keyText != null) keyText.text = key;
        if (skillText != null) skillText.text = skill;

        if (background != null)
        {
            background.sprite = GetCircleSprite();
            background.color = slotIndex == 0 ? new Color(0.1f, 0.35f, 0.75f, 0.9f) : new Color(0.55f, 0.2f, 0.75f, 0.9f);
        }
    }

    public void SetCooldown(float ratio, float remainingSeconds)
    {
        EnsureCircularView();

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = ratio;
            cooldownFill.enabled = ratio > 0.001f;
        }

        if (cooldownText != null)
        {
            bool hasCooldown = ratio > 0.001f;
            cooldownText.enabled = hasCooldown;
            cooldownText.text = hasCooldown ? Mathf.CeilToInt(remainingSeconds).ToString() : string.Empty;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 중에는 원래 슬롯이 Drop 이벤트를 막지 않도록 Raycast를 잠시 끈다.
        originPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = originPosition;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    public void OnDrop(PointerEventData eventData)
    {
        SkillSlotUI from = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<SkillSlotUI>() : null;
        owner?.RequestSwap(from, this);
    }

    private void EnsureCircularView()
    {
        if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(72f, 72f);

        Sprite circle = GetCircleSprite();
        if (background != null)
            background.sprite = circle;

        if (cooldownFill == null)
        {
            GameObject fillObject = new GameObject("CooldownFill");
            fillObject.transform.SetParent(transform, false);

            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            cooldownFill = fillObject.AddComponent<Image>();
            cooldownFill.sprite = circle;
            cooldownFill.color = new Color(0f, 0f, 0f, 0.68f);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = true;
            cooldownFill.fillAmount = 0f;
            cooldownFill.raycastTarget = false;
        }

        if (cooldownText == null)
        {
            GameObject textObject = new GameObject("CooldownText");
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            cooldownText = textObject.AddComponent<Text>();
            cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cooldownText.fontSize = 22;
            cooldownText.fontStyle = FontStyle.Bold;
            cooldownText.alignment = TextAnchor.MiddleCenter;
            cooldownText.color = Color.white;
            cooldownText.raycastTarget = false;
            cooldownText.enabled = false;
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 2) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
