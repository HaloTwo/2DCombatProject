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
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float jumpPower = 8.5f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Dash")]
    [SerializeField] private float dashPower = 13f;
    [SerializeField] private float dashDuration = 0.13f;
    [SerializeField] private float dashCooldown = 0.3f;

    private float facing = 1f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private float nextDashTime;
    private int jumpCount;
    private bool isDashing;
    private Collider2D[] ownColliders;

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

        ownColliders = GetComponentsInChildren<Collider2D>();
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

    // 지상 점프와 공중 2단 점프를 분리해서 점프 입력이 과하게 튀지 않게 처리한다.
    private void TryJump()
    {
        bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        if (!hasBufferedJump)
            return;

        bool canGroundJump = Time.time - lastGroundedTime <= coyoteTime;
        bool canAirJump = !IsGrounded && jumpCount < maxJumpCount;

        if (!canGroundJump && !canAirJump)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        jumpCount = canGroundJump ? 1 : jumpCount + 1;
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
        float dashDir = input != null && Mathf.Abs(input.Move.x) > 0.01f ? Mathf.Sign(input.Move.x) : facing;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashPower, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originGravity;
        isDashing = false;
    }

    private void UpdateGrounded()
    {
        Vector2 checkPos = groundCheck != null ? groundCheck.position : transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.16f, groundMask);
        IsGrounded = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || IsOwnCollider(hits[i]))
                continue;

            IsGrounded = true;
            break;
        }

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            jumpCount = 0;
        }
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

    private bool IsOwnCollider(Collider2D target)
    {
        if (ownColliders == null) return false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == target)
                return true;
        }

        return false;
    }
}
