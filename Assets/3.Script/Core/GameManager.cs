using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState state = GameState.Ready;

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
        Debug.Log("[GameManager] Game Over");
    }
}
