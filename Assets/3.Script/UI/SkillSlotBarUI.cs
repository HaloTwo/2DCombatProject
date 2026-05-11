using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotBarUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillController skillController;
    [SerializeField] private SkillSlotUI slotOne;
    [SerializeField] private SkillSlotUI slotTwo;

    [Header("스킬 선택 패널")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private RectTransform selectionContent;
    [SerializeField] private Button selectionButtonPrefab;
    [SerializeField] private Text selectionTitleText;

    private readonly List<Button> createdButtons = new();
    private Canvas selectionCanvas;
    private int selectedSlotIndex;

    private void Start()
    {
        EnsureSelectionPanel();
        CloseSelectionPanel();
        Refresh();
    }

    private void Update()
    {
        if (skillController == null)
            return;

        slotOne?.SetCooldown(skillController.GetCooldownRatio(0), skillController.GetCooldownRemaining(0));
        slotTwo?.SetCooldown(skillController.GetCooldownRatio(1), skillController.GetCooldownRemaining(1));
    }

    public void Bind(PlayerSkillController controller)
    {
        skillController = controller;
        Refresh();
    }

    public void RequestSwap(SkillSlotUI from, SkillSlotUI to)
    {
        if (skillController == null || from == null || to == null || from == to)
            return;

        // 드래그 교체는 스킬 데이터와 쿨다운 상태를 같이 교환한다.
        skillController.SwapSkillSlots();
        Refresh();
        slotOne?.PlaySwapFeedback();
        slotTwo?.PlaySwapFeedback();
    }

    public void SelectSlot(int slotIndex)
    {
        selectedSlotIndex = Mathf.Clamp(slotIndex, 0, 1);
        OpenSelectionPanel(selectedSlotIndex);
    }

    public void OpenSelectionPanel(int slotIndex)
    {
        EnsureSelectionPanel();
        if (selectionPanel == null || skillController == null)
            return;

        selectedSlotIndex = Mathf.Clamp(slotIndex, 0, 1);
        if (selectionTitleText != null)
            selectionTitleText.text = selectedSlotIndex == 0 ? "A 슬롯 스킬 선택" : "S 슬롯 스킬 선택";

        RebuildSkillButtons();
        selectionPanel.transform.SetAsLastSibling();
        selectionPanel.SetActive(true);
    }

    public void CloseSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    public void Refresh()
    {
        if (slotOne != null)
        {
            slotOne.Bind(this, 0);
            slotOne.SetLabel("A", skillController != null ? skillController.SkillOne : null);
        }

        if (slotTwo != null)
        {
            slotTwo.Bind(this, 1);
            slotTwo.SetLabel("S", skillController != null ? skillController.SkillTwo : null);
        }
    }

    private void RebuildSkillButtons()
    {
        for (int i = 0; i < createdButtons.Count; i++)
        {
            if (createdButtons[i] != null)
                Destroy(createdButtons[i].gameObject);
        }
        createdButtons.Clear();

        SkillData[] skills = skillController.AvailableSkills;
        if (skills == null || selectionContent == null)
            return;

        int createdCount = 0;
        for (int i = 0; i < skills.Length; i++)
        {
            SkillData skill = skills[i];
            if (skill == null)
                continue;

            Button button = CreateSkillButton(skill);
            createdButtons.Add(button);
            createdCount++;
        }

        if (createdCount == 0)
        {
            Button emptyButton = CreateDefaultButton(selectionContent);
            emptyButton.interactable = false;
            Text label = emptyButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "장착 가능한 스킬 없음";
            createdButtons.Add(emptyButton);
        }
    }

    private Button CreateSkillButton(SkillData skill)
    {
        Button button = selectionButtonPrefab != null
            ? Instantiate(selectionButtonPrefab, selectionContent)
            : CreateDefaultButton(selectionContent);

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = skill.DisplayName;

        Transform iconTransform = button.transform.Find("Icon");
        Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (icon != null)
        {
            icon.sprite = skill.icon;
            icon.enabled = skill.icon != null;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => EquipSelectedSkill(skill));
        return button;
    }

    private void EquipSelectedSkill(SkillData skill)
    {
        if (skillController == null || skill == null)
            return;

        skillController.SetSkillSlot(selectedSlotIndex, skill);
        Refresh();

        if (selectedSlotIndex == 0)
            slotOne?.PlaySwapFeedback();
        else
            slotTwo?.PlaySwapFeedback();

        CloseSelectionPanel();
    }

    private void EnsureSelectionPanel()
    {
        if (selectionPanel != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform panelParent = canvas != null ? canvas.transform : transform;

        selectionPanel = new GameObject("SkillSelectionPanel");
        selectionPanel.transform.SetParent(panelParent, false);

        RectTransform panelRect = selectionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(105f, 150f);
        panelRect.sizeDelta = new Vector2(240f, 210f);

        Image panelImage = selectionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.04f, 0.06f, 0.9f);

        // 런타임 생성 패널이 기존 HUD 이미지 뒤에 깔리지 않도록 별도 Canvas로 정렬 순서를 올린다.
        selectionCanvas = selectionPanel.AddComponent<Canvas>();
        selectionCanvas.overrideSorting = true;
        selectionCanvas.sortingOrder = 50;
        selectionPanel.AddComponent<GraphicRaycaster>();

        VerticalLayoutGroup layout = selectionPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = selectionPanel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        selectionTitleText = CreateText("Title", selectionPanel.transform, "스킬 선택", 16, FontStyle.Bold);
        selectionContent = new GameObject("SkillList").AddComponent<RectTransform>();
        selectionContent.SetParent(selectionPanel.transform, false);

        LayoutElement contentLayoutElement = selectionContent.gameObject.AddComponent<LayoutElement>();
        contentLayoutElement.preferredHeight = 150f;

        VerticalLayoutGroup contentLayout = selectionContent.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
    }

    private Button CreateDefaultButton(Transform parent)
    {
        GameObject go = new GameObject("SkillButton");
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.11f, 0.13f, 0.17f, 0.95f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 32f);

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.minHeight = 32f;
        layoutElement.preferredHeight = 32f;

        Text label = CreateText("Label", go.transform, "Skill", 14, FontStyle.Normal);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        label.alignment = TextAnchor.MiddleLeft;
        return button;
    }

    private Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        Text label = go.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }
}
