using UnityEngine;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState state = GameState.Ready;
    [SerializeField, KoreanLabel("게임 BGM")] private BGMType gameBgm = BGMType.MainBGM;
    [SerializeField, KoreanLabel("시작 시 BGM 재생")] private bool playBgmOnStart = true;

    [Header("게임오버 연출")]
    [SerializeField, KoreanLabel("사망 슬로우 배율")] private float gameOverSlowScale = 0.18f;
    [SerializeField, KoreanLabel("사망 슬로우 시간")] private float gameOverSlowDuration = 1.8f;
    [SerializeField, KoreanLabel("최소 사망 연출 시간")] private float minGameOverPresentationTime = 1.5f;
    [SerializeField, KoreanLabel("게임오버 후 정지")] private bool pauseAfterGameOver = true;

    public GameState State => state;
    public bool CanShowGameOverMenu { get; private set; }
    public bool IsPaused => state == GameState.Paused;

    private void Start()
    {
        if (state == GameState.Playing)
            PlayGameBGM();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        CanShowGameOverMenu = false;
        state = GameState.Playing;
        PlayGameBGM();
    }

    public void ClearGame()
    {
        if (state == GameState.Clear) return;
        state = GameState.Clear;
        WaveAnnounceUI.ShowGameClearGlobal();
        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game Clear");
    }

    public void PauseGame()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (state != GameState.Paused)
            return;

        state = GameState.Playing;
        Time.timeScale = 1f;
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

    // 로딩씬이 끝나고 GameScene에 들어왔을 때 메인 전투 BGM을 한 번만 재생한다.
    private void PlayGameBGM()
    {
        if (!playBgmOnStart)
            return;

        SoundManager.Instance?.PlayBGM(gameBgm);
    }
}
