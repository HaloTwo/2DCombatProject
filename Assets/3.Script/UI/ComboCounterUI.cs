using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComboCounterUI : Singleton<ComboCounterUI>
{
    [SerializeField] private Text comboText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private float resetDelay = 1.2f;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float popScale = 1.18f;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -145f);
    [SerializeField] private Vector2 panelSize = new Vector2(330f, 74f);
    [SerializeField] private Color textColor = Color.white;

    private int comboCount;
    private float lastComboTime = -999f;
    private Coroutine popRoutine;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        EnsureReadableStyle();
        Refresh();
    }

    private void OnEnable()
    {
        Hitbox.OnAnyHit += HandleAnyHit;
        Health.OnAnyDead += HandleAnyDead;
    }

    private void OnDisable()
    {
        Hitbox.OnAnyHit -= HandleAnyHit;
        Health.OnAnyDead -= HandleAnyDead;
    }

    private void Update()
    {
        if (comboCount > 0 && Time.time - lastComboTime > resetDelay)
        {
            comboCount = 0;
            Refresh();
        }
    }

    // 플레이어 공격이 실제로 명중했을 때만 콤보를 갱신한다.
    private void HandleAnyHit(Health target, DamageInfo info)
    {
        if (info.AttackerTeam != Team.Player || target == null || target.Team != Team.Enemy)
            return;

        RegisterCombo();
    }

    // 연속 처치도 콤보를 이어가게 해서 제출 영상에서 전투 템포가 더 잘 보이도록 한다.
    private void HandleAnyDead(Health dead)
    {
        if (dead == null || dead.Team != Team.Enemy)
            return;

        if (Time.time - lastComboTime <= 0.05f)
            return;

        RegisterCombo();
    }

    public void RegisterHit(DamageInfo info)
    {
        if (info.AttackerTeam == Team.Player)
            RegisterCombo();
    }

    private void RegisterCombo()
    {
        comboCount++;
        lastComboTime = Time.time;
        Refresh();
        PlayPop();
    }

    private void Refresh()
    {
        if (comboText == null)
            return;

        bool visible = comboCount > 0;
        comboText.gameObject.SetActive(visible);
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (!visible)
            return;

        comboText.text = $"HIT x{comboCount}";
    }

    private void PlayPop()
    {
        if (comboText == null || !isActiveAndEnabled)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(CoPop());
    }

    private IEnumerator CoPop()
    {
        Transform target = comboText.transform;
        float half = Mathf.Max(0.01f, popDuration * 0.5f);
        float time = 0f;

        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(Vector3.one, Vector3.one * popScale, Mathf.Clamp01(time / half));
            yield return null;
        }

        time = 0f;
        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, Mathf.Clamp01(time / half));
            yield return null;
        }

        target.localScale = Vector3.one;
        popRoutine = null;
    }

    private void EnsureReadableStyle()
    {
        if (comboText == null)
            comboText = GetComponentInChildren<Text>(true);

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = panelSize;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
            backgroundImage.raycastTarget = false;
        }

        if (comboText == null)
            return;

        comboText.fontSize = Mathf.Max(comboText.fontSize, 42);
        comboText.fontStyle = FontStyle.Bold;
        comboText.alignment = TextAnchor.MiddleCenter;
        comboText.color = textColor;

        Outline outline = comboText.GetComponent<Outline>();
        if (outline == null)
            outline = comboText.gameObject.AddComponent<Outline>();

        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        Shadow shadow = comboText.GetComponent<Shadow>();
        if (shadow == null)
            shadow = comboText.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }
}
