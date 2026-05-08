using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Text keyText;
    [SerializeField] private Text skillText;
    [SerializeField] private Image background;

    private SkillSlotBarUI owner;
    private int slotIndex;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
            background.color = slotIndex == 0 ? new Color(0.1f, 0.35f, 0.75f, 0.9f) : new Color(0.55f, 0.2f, 0.75f, 0.9f);
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
}
