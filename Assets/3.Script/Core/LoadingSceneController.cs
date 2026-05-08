using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private float minimumLoadingTime = 0.6f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(minimumLoadingTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        while (op != null && !op.isDone)
            yield return null;
    }
}
