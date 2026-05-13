using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultView : MonoBehaviour
{
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string startSceneName = "StartScene";

    private void Start()
    {
        EnsurePanelMenus();
        HideAll();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (GameManager.Instance.State == GameState.Clear)
            ShowClear();
        else if (GameManager.Instance.State == GameState.GameOver && GameManager.Instance.CanShowGameOverMenu)
            ShowGameOver();
        else if (GameManager.Instance.State != GameState.Paused && pausePanel != null && pausePanel.activeSelf)
            pausePanel.SetActive(false);
    }

    public void HideAll()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ShowClear()
    {
        if (clearPanel != null) clearPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ShowPause()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Retry()
    {
        CleanupBeforeSceneChange();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToStart()
    {
        CleanupBeforeSceneChange();
        SceneManager.LoadScene(startSceneName);
    }

    public void TogglePause()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.State == GameState.Playing)
        {
            GameManager.Instance.PauseGame();
            ShowPause();
        }
        else if (GameManager.Instance.State == GameState.Paused)
        {
            GameManager.Instance.ResumeGame();
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }

    private void EnsurePanelMenus()
    {
        if (pausePanel == null)
            pausePanel = CreateMenuPanel("PausePanel", "PAUSED", new Color(0.05f, 0.06f, 0.08f, 0.88f));

        EnsureMenuButtons(clearPanel);
        EnsureMenuButtons(gameOverPanel);
        EnsureMenuButtons(pausePanel);
    }

    private void EnsureMenuButtons(GameObject panel)
    {
        if (panel == null)
            return;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        Button retryButton = GetOrCreateButton(panelRect, "RetryButton", "다시하기", new Vector2(-90f, -80f), Retry);
        ConfigureButton(retryButton, "다시하기", new Vector2(-90f, -80f));

        Button startButton = GetOrCreateButton(panelRect, "StartButton", "처음으로", new Vector2(90f, -80f), GoToStart);
        ConfigureButton(startButton, "처음으로", new Vector2(90f, -80f));
    }

    // 재시작/타이틀 이동 전에 현재 씬에서 풀로 꺼낸 몹, 투사체, 포커스 슬로우 상태를 정리한다.
    private void CleanupBeforeSceneChange()
    {
        Time.timeScale = 1f;
        FocusModeController.Stop();
        SoundManager.Instance?.StopBGM();
        WaveManager.Instance?.StopAndClearWaves();

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.ReleaseAllActive();
    }

    private GameObject CreateMenuPanel(string panelName, string title, Color color)
    {
        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObject.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 40f);
        titleRect.sizeDelta = new Vector2(420f, 70f);

        Text text = titleObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = title;
        text.fontSize = 48;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        panel.SetActive(false);
        return panel;
    }

    private Button CreateButton(RectTransform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(150f, 46f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.1f, 0.16f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }

    private Button GetOrCreateButton(RectTransform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        Transform existing = parent.transform.Find(name);
        if (existing == null)
            return CreateButton(parent, name, label, position, onClick);

        Button button = existing.GetComponent<Button>();
        if (button == null)
            button = existing.gameObject.AddComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
        return button;
    }

    // 씬에 미리 배치된 버튼도 결과창 규칙에 맞게 텍스트/위치만 정리한다.
    private void ConfigureButton(Button button, string label, Vector2 position)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150f, 46f);
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;
    }
}
