using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Text keyText;
    [SerializeField] private Text skillText;
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Image highlightImage;
    [SerializeField] private float feedbackDuration = 0.16f;
    [SerializeField] private float readyFlashScale = 1.12f;

    private SkillSlotBarUI owner;
    private int slotIndex;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originPosition;
    private Coroutine feedbackRoutine;
    private bool wasCoolingDown;
    private static Sprite circleSprite;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (background == null)
            background = GetComponent<Image>();

        //SetupView();
    }

    public void Bind(SkillSlotBarUI newOwner, int newSlotIndex)
    {
        owner = newOwner;
        slotIndex = newSlotIndex;
    }

    public void SetLabel(string key, SkillData skill)
    {
        if (keyText != null)
            keyText.text = key;

        if (skillText != null)
            skillText.text = skill != null ? skill.DisplayName : "Empty";

        if (iconImage != null)
        {
            iconImage.sprite = skill != null ? skill.icon : null;
            iconImage.enabled = skill != null && skill.icon != null;
        }

        if (background != null)
        {
            background.sprite = GetCircleSprite();
            background.color = slotIndex == 0
                ? new Color(0.1f, 0.35f, 0.75f, 0.9f)
                : new Color(0.55f, 0.2f, 0.75f, 0.9f);
        }
    }

    public void SetCooldown(float ratio, float remainingSeconds)
    {
        bool hasCooldown = ratio > 0.001f;

        if (cooldownOverlay != null)
            cooldownOverlay.enabled = hasCooldown;

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = ratio;
            cooldownFill.enabled = hasCooldown;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = hasCooldown;
            cooldownText.text = hasCooldown ? Mathf.Max(1, Mathf.CeilToInt(remainingSeconds)).ToString() : string.Empty;
        }

        if (wasCoolingDown && !hasCooldown)
            PlayReadyFeedback();

        wasCoolingDown = hasCooldown;
    }

    public void PlaySwapFeedback()
    {
        PlayFeedback(new Color(1f, 0.85f, 0.25f, 0.8f), 1.08f, feedbackDuration);
    }

    public void PlayReadyFeedback()
    {
        PlayFeedback(new Color(1f, 1f, 1f, 0.9f), readyFlashScale, feedbackDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.SelectSlot(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 중에는 현재 슬롯이 Drop 이벤트를 가로막지 않도록 Raycast를 잠깐 끈다.
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

    // UI 오브젝트는 씬에서 미리 만들어 참조한다.
    // 여기서는 런타임에 새 UI를 생성하지 않고, 필요한 표시 속성만 세팅한다.
    private void SetupView()
    {
        Sprite circle = GetCircleSprite();

        if (background != null)
        {
            background.sprite = circle;
            background.raycastTarget = true;
        }

        if (iconImage != null)
        {
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.enabled = false;
            cooldownOverlay.color = new Color(0f, 0f, 0f, 0.45f);
            cooldownOverlay.raycastTarget = false;
        }

        if (cooldownFill != null)
        {
            cooldownFill.sprite = circle;
            cooldownFill.color = new Color(0f, 0f, 0f, 0.68f);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = true;
            cooldownFill.fillAmount = 0f;
            cooldownFill.enabled = false;
            cooldownFill.raycastTarget = false;
        }

        if (cooldownText != null)
        {
            cooldownText.fontStyle = FontStyle.Bold;
            cooldownText.alignment = TextAnchor.MiddleCenter;
            cooldownText.color = Color.white;
            cooldownText.raycastTarget = false;
            cooldownText.enabled = false;
        }

        if (highlightImage != null)
        {
            highlightImage.enabled = true;
            highlightImage.color = new Color(1f, 1f, 1f, 0f);
            highlightImage.raycastTarget = false;
        }
    }

    private void PlayFeedback(Color color, float scale, float duration)
    {
        if (!isActiveAndEnabled)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(CoFeedback(color, scale, duration));
    }

    private IEnumerator CoFeedback(Color color, float scale, float duration)
    {
        Vector3 originScale = Vector3.one;

        if (highlightImage != null)
            highlightImage.color = color;

        float half = Mathf.Max(0.01f, duration * 0.5f);
        float time = 0f;

        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / half);
            transform.localScale = Vector3.Lerp(originScale, Vector3.one * scale, t);
            yield return null;
        }

        time = 0f;

        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / half);
            transform.localScale = Vector3.Lerp(Vector3.one * scale, originScale, t);

            if (highlightImage != null)
            {
                Color faded = color;
                faded.a = Mathf.Lerp(color.a, 0f, t);
                highlightImage.color = faded;
            }

            yield return null;
        }

        transform.localScale = originScale;

        if (highlightImage != null)
            highlightImage.color = new Color(color.r, color.g, color.b, 0f);

        feedbackRoutine = null;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 2) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}