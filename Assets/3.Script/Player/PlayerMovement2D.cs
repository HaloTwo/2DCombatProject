using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpPower = 14f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBufferTime = 0.08f;

    [Header("Dash")]
    [SerializeField] private float dashPower = 18f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.35f;

    private float facing = 1f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private float nextDashTime;
    private bool isDashing;

    public float Facing => facing;
    public bool IsDashing => isDashing;
    public bool IsGrounded { get; private set; }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (input == null) input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        UpdateGrounded();
        CacheInputTime();

        if (input != null && input.DashPressed)
            TryDash();

        TryJump();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isDashing || input == null) return;

        rb.linearVelocity = new Vector2(input.Move.x * moveSpeed, rb.linearVelocity.y);
    }

    // 점프 입력 버퍼와 코요테 타임을 함께 처리해 플랫폼 액션 조작감을 보정한다.
    private void TryJump()
    {
        bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canUseCoyote = Time.time - lastGroundedTime <= coyoteTime;

        if (!hasBufferedJump || !canUseCoyote)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        lastJumpPressedTime = -999f;
        lastGroundedTime = -999f;
    }

    private void TryDash()
    {
        if (Time.time < nextDashTime || isDashing)
            return;

        StartCoroutine(CoDash());
    }

    private IEnumerator CoDash()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        float originGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facing * dashPower, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originGravity;
        isDashing = false;
    }

    private void UpdateGrounded()
    {
        Vector2 checkPos = groundCheck != null ? groundCheck.position : transform.position;
        IsGrounded = Physics2D.OverlapCircle(checkPos, 0.12f, groundMask);

        if (IsGrounded)
            lastGroundedTime = Time.time;
    }

    private void CacheInputTime()
    {
        if (input != null && input.JumpPressed)
            lastJumpPressedTime = Time.time;
    }

    private void UpdateFacing()
    {
        if (input == null) return;
        if (Mathf.Abs(input.Move.x) < 0.01f) return;

        facing = Mathf.Sign(input.Move.x);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        transform.localScale = scale;
    }
}
