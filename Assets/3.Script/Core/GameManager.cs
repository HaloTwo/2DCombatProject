using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState state = GameState.Ready;

    [Header("게임오버 연출")]
    [SerializeField, KoreanLabel("사망 슬로우 배율")] private float gameOverSlowScale = 0.18f;
    [SerializeField, KoreanLabel("사망 슬로우 시간")] private float gameOverSlowDuration = 0.35f;
    [SerializeField, KoreanLabel("게임오버 후 정지")] private bool pauseAfterGameOver = true;

    public GameState State => state;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        state = GameState.Playing;
    }

    public void ClearGame()
    {
        if (state == GameState.Clear) return;
        state = GameState.Clear;
        Debug.Log("[GameManager] Game Clear");
    }

    public void GameOver()
    {
        if (state == GameState.GameOver) return;
        state = GameState.GameOver;
        StartCoroutine(CoGameOverSlowMotion());
        Debug.Log("[GameManager] Game Over");
    }

    // 플레이어 사망 순간만 짧게 느려지게 해서 게임오버 전환에 무게감을 준다.
    private IEnumerator CoGameOverSlowMotion()
    {
        Time.timeScale = Mathf.Clamp(gameOverSlowScale, 0.01f, 1f);
        yield return new WaitForSecondsRealtime(gameOverSlowDuration);
        Time.timeScale = pauseAfterGameOver ? 0f : 1f;
    }
}
