using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private string loadingSceneName = "LoadingScene";

    private InputAction submitAction;

    private void Awake()
    {
        submitAction = new InputAction("Submit", InputActionType.Button);
        submitAction.AddBinding("<Keyboard>/enter");
        submitAction.AddBinding("<Keyboard>/space");
        submitAction.AddBinding("<Gamepad>/start");
        submitAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        submitAction.Enable();
    }

    private void OnDisable()
    {
        submitAction.Disable();
    }

    private void Update()
    {
        if (submitAction.WasPressedThisFrame())
            StartGame();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(loadingSceneName);
    }
}
