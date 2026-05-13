using UnityEngine;
using UnityEngine.UI;

public class BuffStatusSlotUI : MonoBehaviour
{
    [SerializeField, KoreanLabel("배경 이미지")] private Image backgroundImage;
    [SerializeField, KoreanLabel("아이콘 이미지")] private Image iconImage;
    [SerializeField, KoreanLabel("쿨타임 Fill 이미지")] private Image fillImage;
    [SerializeField, KoreanLabel("남은 시간 텍스트")] private Text timeText;

    private RectTransform rectTransform;

    public BuffItemType Type { get; private set; }
    public float Duration { get; private set; }
    public float EndTime { get; private set; }

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            return rectTransform;
        }
    }

    public void Initialize(BuffItemType type, float duration, float slotSize, Sprite circleSprite, Color iconColor)
    {
        Type = type;
        Duration = Mathf.Max(0.1f, duration);
        EndTime = Time.time + Duration;

        EnsureView(slotSize, circleSprite);

        gameObject.SetActive(true);
        transform.localScale = Vector3.one;

        if (backgroundImage != null)
            backgroundImage.color = new Color(0f, 0f, 0f, 0.72f);

        if (iconImage != null)
            iconImage.color = iconColor;

        UpdateView();
    }

    // 같은 버프를 다시 획득했을 때 기존 슬롯의 남은 시간을 연장한다.
    public void ExtendDuration(float additionalDuration)
    {
        if (additionalDuration <= 0f)
            return;

        float remaining = Mathf.Max(0f, EndTime - Time.time);

        Duration = Mathf.Max(0.1f, remaining + additionalDuration);
        EndTime = Time.time + Duration;

        gameObject.SetActive(true);
        transform.localScale = Vector3.one;

        UpdateView();
    }

    public bool UpdateView()
    {
        float remaining = EndTime - Time.time;

        if (remaining <= 0f)
        {
            ClearView();
            return false;
        }

        float ratio = Duration <= 0f ? 0f : Mathf.Clamp01(remaining / Duration);

        if (fillImage != null)
        {
            fillImage.enabled = ratio > 0.001f;
            fillImage.fillAmount = ratio;
        }

        if (timeText != null)
            timeText.text = remaining.ToString("0.0");

        return true;
    }

    public void ClearView()
    {
        Duration = 0f;
        EndTime = 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.enabled = false;
        }

        if (timeText != null)
            timeText.text = string.Empty;
    }

    private void EnsureView(float slotSize, Sprite circleSprite)
    {
        RectTransform.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement layout = GetComponent<LayoutElement>();

        if (layout == null)
            layout = gameObject.AddComponent<LayoutElement>();

        layout.minWidth = slotSize;
        layout.minHeight = slotSize;
        layout.preferredWidth = slotSize;
        layout.preferredHeight = slotSize;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        backgroundImage.sprite = circleSprite;
        backgroundImage.raycastTarget = false;

        if (iconImage == null)
            iconImage = CreateImage("Icon", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), circleSprite);

        if (fillImage == null)
        {
            fillImage = CreateImage("CooldownFill", Vector2.zero, Vector2.one, circleSprite);
            fillImage.color = new Color(0f, 0f, 0f, 0.58f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
        }

        if (timeText == null)
        {
            GameObject textObject = new GameObject("TimeText");
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            Stretch(textRect, Vector2.zero, Vector2.one);

            timeText = textObject.AddComponent<Text>();
            timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.fontSize = 14;
            timeText.fontStyle = FontStyle.Bold;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.color = Color.white;
            timeText.raycastTarget = false;
        }
    }

    private Image CreateImage(string objectName, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(transform, false);

        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        Stretch(imageRect, anchorMin, anchorMax);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        return image;
    }

    private static void Stretch(RectTransform target, Vector2 anchorMin, Vector2 anchorMax)
    {
        target.anchorMin = anchorMin;
        target.anchorMax = anchorMax;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}