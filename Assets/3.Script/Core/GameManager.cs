using UnityEngine;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState state = GameState.Ready;

    [Header("게임오버 연출")]
    [SerializeField, KoreanLabel("사망 슬로우 배율")] private float gameOverSlowScale = 0.18f;
    [SerializeField, KoreanLabel("사망 슬로우 시간")] private float gameOverSlowDuration = 1.8f;
    [SerializeField, KoreanLabel("최소 사망 연출 시간")] private float minGameOverPresentationTime = 1.5f;
    [SerializeField, KoreanLabel("게임오버 후 정지")] private bool pauseAfterGameOver = true;

    public GameState State => state;
    public bool CanShowGameOverMenu { get; private set; }

    public void StartGame()
    {
        Time.timeScale = 1f;
        CanShowGameOverMenu = false;
        state = GameState.Playing;
    }

    public void ClearGame()
    {
        if (state == GameState.Clear) return;
        state = GameState.Clear;
        WaveAnnounceUI.ShowGameClearGlobal();
        Debug.Log("[GameManager] Game Clear");
    }

    public void GameOver()
    {
        if (state == GameState.GameOver) return;
        state = GameState.GameOver;
        CanShowGameOverMenu = false;
        StartCoroutine(CoGameOverSlowMotion());
        Debug.Log("[GameManager] Game Over");
    }

    // 플레이어 사망 모션이 먼저 보이도록 슬로우모션을 재생한 뒤, 재시작 메뉴 표시를 허용한다.
    private IEnumerator CoGameOverSlowMotion()
    {
        Time.timeScale = Mathf.Clamp(gameOverSlowScale, 0.01f, 1f);
        yield return new WaitForSecondsRealtime(Mathf.Max(gameOverSlowDuration, minGameOverPresentationTime));
        CanShowGameOverMenu = true;
        Time.timeScale = pauseAfterGameOver ? 0f : 1f;
    }
}
