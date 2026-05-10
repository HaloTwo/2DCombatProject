using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    readonly string loadingSceneName = "LoadingScene";

    public void StartGame()
    {
        SceneManager.LoadScene(loadingSceneName);
    }
}
