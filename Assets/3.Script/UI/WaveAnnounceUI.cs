using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveAnnounceUI : Singleton<WaveAnnounceUI>
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private Text subText;
    [SerializeField] private Text progressText;

    [Header("Style")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -46f);
    [SerializeField] private Vector2 panelSize = new Vector2(620f, 150f);
    [SerializeField] private Color mainColor = Color.white;
    [SerializeField] private Color accentColor = new Color(0.2f, 0.9f, 1f, 1f);
    [SerializeField] private Color clearColor = new Color(1f, 0.22f, 0.68f, 1f);
    [SerializeField] private int mainFontSize = 56;
    [SerializeField] private int subFontSize = 24;
    [SerializeField] private int progressFontSize = 26;
    [SerializeField] private float popDuration = 0.14f;
    [SerializeField] private float popScale = 1.12f;

    private Coroutine popRoutine;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        EnsureRefs();
        SetVisible(false);
    }

    public void ShowWaveStart(int waveIndex, int totalEnemies)
    {
        EnsureRefs();
        SetVisible(true);
        SetTexts($"WAVE {waveIndex + 1}", "READY", $"0 / {Mathf.Max(0, totalEnemies)}", mainColor);
        PlayPop();
    }

    public void ShowBossWaveStart(int waveIndex, int totalEnemies, string bossName)
    {
        EnsureRefs();
        SetVisible(true);
        string label = string.IsNullOrWhiteSpace(bossName) ? "BOSS" : bossName;
        SetTexts("BOSS WAVE", label, $"0 / {Mathf.Max(0, totalEnemies)}", clearColor);
        PlayPop();
        CameraShake.ShakeDefault();
    }

    public void ShowCountdown(int count, int waveIndex)
    {
        EnsureRefs();
        SetVisible(true);
        SetTexts(count.ToString(), $"WAVE {waveIndex + 1}", string.Empty, accentColor);
        PlayPop();
    }

    public void ShowWaveProgress(int waveIndex, int killed, int total)
    {
        EnsureRefs();
        SetVisible(true);
        SetTexts($"WAVE {waveIndex + 1}", "ENEMIES", $"{Mathf.Max(0, killed)} / {Mathf.Max(0, total)}", accentColor);
    }

    public void ShowWaveClear(int waveIndex)
    {
        EnsureRefs();
        SetVisible(true);
        SetTexts("WAVE CLEAR!", $"WAVE {waveIndex + 1}", string.Empty, clearColor);
        PlayPop();
    }

    public void ShowGameClear()
    {
        EnsureRefs();
        SetVisible(true);
        SetTexts("CLEAR!", "MISSION COMPLETE", string.Empty, clearColor);
        PlayPop();
    }

    public static void ShowWaveStartGlobal(int waveIndex, int totalEnemies)
    {
        EnsureInstance()?.ShowWaveStart(waveIndex, totalEnemies);
    }

    public static void ShowBossWaveStartGlobal(int waveIndex, int totalEnemies, string bossName)
    {
        EnsureInstance()?.ShowBossWaveStart(waveIndex, totalEnemies, bossName);
    }

    public static void ShowCountdownGlobal(int count, int waveIndex)
    {
        EnsureInstance()?.ShowCountdown(count, waveIndex);
    }

    public static void ShowWaveProgressGlobal(int waveIndex, int killed, int total)
    {
        EnsureInstance()?.ShowWaveProgress(waveIndex, killed, total);
    }

    public static void ShowWaveClearGlobal(int waveIndex)
    {
        EnsureInstance()?.ShowWaveClear(waveIndex);
    }

    public static void ShowGameClearGlobal()
    {
        EnsureInstance()?.ShowGameClear();
    }

    private static WaveAnnounceUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;
        
        return FindFirstObjectByType<WaveAnnounceUI>(FindObjectsInactive.Include);
    }

    private void EnsureRefs()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = panelSize;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        Image existingBackground = GetComponent<Image>();
        if (existingBackground != null)
            existingBackground.enabled = false;

        if (messageText == null)
            messageText = CreateText("Message", new Vector2(0f, -42f), new Vector2(panelSize.x - 30f, 64f), mainFontSize, mainColor);

        if (subText == null)
            subText = CreateText("SubText", new Vector2(0f, -91f), new Vector2(panelSize.x - 30f, 34f), subFontSize, Color.white);

        if (progressText == null)
            progressText = CreateText("Progress", new Vector2(0f, -122f), new Vector2(panelSize.x - 30f, 34f), progressFontSize, accentColor);
    }

    private Text CreateText(string objectName, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void SetTexts(string main, string sub, string progress, Color mainTextColor)
    {
        if (messageText != null)
        {
            messageText.text = main;
            messageText.color = mainTextColor;
            messageText.fontSize = mainFontSize;
        }

        if (subText != null)
        {
            subText.text = sub;
            subText.color = Color.white;
            subText.gameObject.SetActive(!string.IsNullOrEmpty(sub));
        }

        if (progressText != null)
        {
            progressText.text = progress;
            progressText.color = accentColor;
            progressText.gameObject.SetActive(!string.IsNullOrEmpty(progress));
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        gameObject.SetActive(true);
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void PlayPop()
    {
        if (!isActiveAndEnabled)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(CoPop());
    }

    private IEnumerator CoPop()
    {
        float time = 0f;
        while (time < popDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / popDuration);
            transform.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        popRoutine = null;
    }
}
