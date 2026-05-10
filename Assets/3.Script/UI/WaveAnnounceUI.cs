using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveAnnounceUI : MonoBehaviour
{
    [SerializeField, KoreanLabel("루트 캔버스 그룹")] private CanvasGroup canvasGroup;
    [SerializeField, KoreanLabel("메시지 텍스트")] private Text messageText;
    [SerializeField, KoreanLabel("등장 시간")] private float fadeInTime = 0.12f;
    [SerializeField, KoreanLabel("유지 시간")] private float holdTime = 0.55f;
    [SerializeField, KoreanLabel("퇴장 시간")] private float fadeOutTime = 0.25f;
    [SerializeField, KoreanLabel("등장 스케일")] private float popScale = 1.16f;

    private Coroutine routine;

    public static WaveAnnounceUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<Text>();

        SetVisible(0f, Vector3.one);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowWaveClear(int waveIndex)
    {
        Play($"WAVE {waveIndex + 1} CLEAR", false);
    }

    public void ShowGameClear()
    {
        Play("CLEAR", true);
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

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("WaveAnnounceCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject root = new GameObject("WaveAnnounceUI");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(520f, 90f);

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        Text text = root.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;

        WaveAnnounceUI ui = root.AddComponent<WaveAnnounceUI>();
        ui.canvasGroup = group;
        ui.messageText = text;
        return ui;
    }

    private void Play(string message, bool large)
    {
        if (!isActiveAndEnabled)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(CoPlay(message, large));
    }

    private IEnumerator CoPlay(string message, bool large)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.fontSize = large ? 56 : 34;
        }

        float time = 0f;
        while (time < fadeInTime)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / fadeInTime);
            SetVisible(t, Vector3.Lerp(Vector3.one * popScale, Vector3.one, t));
            yield return null;
        }

        SetVisible(1f, Vector3.one);
        yield return new WaitForSecondsRealtime(large ? holdTime * 2f : holdTime);

        time = 0f;
        while (time < fadeOutTime)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / fadeOutTime);
            SetVisible(1f - t, Vector3.one);
            yield return null;
        }

        SetVisible(0f, Vector3.one);
        routine = null;
    }

    private void SetVisible(float alpha, Vector3 scale)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = false;
        }

        transform.localScale = scale;
    }
}
