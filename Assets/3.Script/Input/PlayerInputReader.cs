using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private float doubleTapDashWindow = 0.25f;
    [SerializeField] private Health health;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private InputAction skillOneAction;
    private InputAction skillTwoAction;
    private InputAction blockAction;
    private InputAction interactAction;
    private InputAction focusAction;

    private float lastLeftTapTime = -999f;
    private float lastRightTapTime = -999f;
    private float previousMoveX;

    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool SkillOnePressed { get; private set; }
    public bool SkillTwoPressed { get; private set; }
    public bool BlockPressed { get; private set; }
    public bool BlockHeld { get; private set; }
    public bool BlockReleased { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool FocusPressed { get; private set; }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        CreateActions();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        attackAction.Enable();
        skillOneAction.Enable();
        skillTwoAction.Enable();
        blockAction.Enable();
        interactAction.Enable();
        focusAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
        attackAction.Disable();
        skillOneAction.Disable();
        skillTwoAction.Disable();
        blockAction.Disable();
        interactAction.Disable();
        focusAction.Disable();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            ClearInput();
            return;
        }

        Move = moveAction.ReadValue<Vector2>();

        JumpPressed = jumpAction.WasPressedThisFrame();
        DashPressed = dashAction.WasPressedThisFrame() || IsDoubleTapDash(Move.x);
        AttackPressed = attackAction.WasPressedThisFrame();
        SkillOnePressed = skillOneAction.WasPressedThisFrame();
        SkillTwoPressed = skillTwoAction.WasPressedThisFrame();
        BlockPressed = blockAction.WasPressedThisFrame();
        BlockHeld = blockAction.IsPressed();
        BlockReleased = blockAction.WasReleasedThisFrame();
        InteractPressed = interactAction.WasPressedThisFrame();
        FocusPressed = focusAction.WasPressedThisFrame();
    }

    // 플레이어 사망 후에는 이동/공격/스킬/가드 입력이 다음 시스템으로 전달되지 않게 한 곳에서 비운다.
    private void ClearInput()
    {
        Move = Vector2.zero;
        JumpPressed = false;
        DashPressed = false;
        AttackPressed = false;
        SkillOnePressed = false;
        SkillTwoPressed = false;
        BlockPressed = false;
        BlockHeld = false;
        BlockReleased = false;
        InteractPressed = false;
        FocusPressed = false;
    }

    // New Input System Action을 코드에서 구성해 키보드와 게임패드를 같은 입력 흐름으로 처리한다.
    private void CreateActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/z");
        dashAction.AddBinding("<Gamepad>/buttonEast");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/c");
        attackAction.AddBinding("<Gamepad>/buttonWest");

        skillOneAction = new InputAction("SkillOne", InputActionType.Button);
        skillOneAction.AddBinding("<Keyboard>/a");
        skillOneAction.AddBinding("<Gamepad>/leftShoulder");

        skillTwoAction = new InputAction("SkillTwo", InputActionType.Button);
        skillTwoAction.AddBinding("<Keyboard>/s");
        skillTwoAction.AddBinding("<Gamepad>/rightShoulder");

        blockAction = new InputAction("Block", InputActionType.Button);
        blockAction.AddBinding("<Keyboard>/x");
        blockAction.AddBinding("<Gamepad>/leftTrigger");

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/upArrow");
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonNorth");

        focusAction = new InputAction("Focus", InputActionType.Button);
        focusAction.AddBinding("<Keyboard>/v");
        focusAction.AddBinding("<Gamepad>/rightStickPress");
    }

    // 방향 입력을 짧게 두 번 넣으면 키보드/패드 모두 회피 입력으로 처리한다.
    private bool IsDoubleTapDash(float moveX)
    {
        bool leftTapped = previousMoveX >= -0.5f && moveX < -0.5f;
        bool rightTapped = previousMoveX <= 0.5f && moveX > 0.5f;
        bool dash = false;

        if (leftTapped)
        {
            dash = Time.time - lastLeftTapTime <= doubleTapDashWindow;
            lastLeftTapTime = Time.time;
        }

        if (rightTapped)
        {
            dash = dash || Time.time - lastRightTapTime <= doubleTapDashWindow;
            lastRightTapTime = Time.time;
        }

        previousMoveX = moveX;
        return dash;
    }
}
