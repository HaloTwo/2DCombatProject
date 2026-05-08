using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private KeyCode skillOneKey = KeyCode.K;
    [SerializeField] private KeyCode skillTwoKey = KeyCode.L;

    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool SkillOnePressed { get; private set; }
    public bool SkillTwoPressed { get; private set; }

    private void Update()
    {
        Move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        JumpPressed = UnityEngine.Input.GetKeyDown(jumpKey);
        DashPressed = UnityEngine.Input.GetKeyDown(dashKey);
        AttackPressed = UnityEngine.Input.GetKeyDown(attackKey);
        SkillOnePressed = UnityEngine.Input.GetKeyDown(skillOneKey);
        SkillTwoPressed = UnityEngine.Input.GetKeyDown(skillTwoKey);
    }
}
