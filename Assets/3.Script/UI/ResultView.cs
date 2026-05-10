using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultView : MonoBehaviour
{
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject gameOverPanel;

    private void Start()
    {
        HideAll();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameState.Clear)
            ShowClear();
        else if (GameManager.Instance.State == GameState.GameOver)
            ShowGameOver();
    }

    public void HideAll()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowClear()
    {
        if (clearPanel != null) clearPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
