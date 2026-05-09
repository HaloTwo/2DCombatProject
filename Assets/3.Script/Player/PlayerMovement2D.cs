using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Health health;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float jumpPower = 8.5f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.58f, 0.08f);
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float minGroundNormalY = 0.65f;
    [SerializeField] private float groundContactGraceTime = 0.08f;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.68f, 1.05f);
    [SerializeField] private float wallCheckDistance = 0.08f;
    [SerializeField] private float minWallNormalX = 0.65f;

    [Header("Dash")]
    [SerializeField] private float dashPower = 13f;
    [SerializeField] private float dashDuration = 0.13f;
    [SerializeField] private float dashCooldown = 0.3f;

    private float facing = 1f;
    private float lastGroundedTime;
    private float lastGroundContactTime = -999f;
    private float lastJumpPressedTime;
    private float nextDashTime;
    private int jumpCount;
    private bool isDashing;
    private Collider2D[] ownColliders;
    private PhysicsMaterial2D noFrictionMaterial;
    private readonly List<ColliderPair> ignoredDashCollisions = new();

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
        if (health == null) health = GetComponent<Health>();

        ownColliders = GetComponentsInChildren<Collider2D>();
        ApplyNoFrictionMaterial();
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

        float moveX = input.Move.x;
        if (IsBlockedHorizontally(moveX))
            moveX = 0f;

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
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
        health?.SetInvincibleFor(dashDuration + 0.05f);
        BeginDashEnemyPassThrough();

        float originGravity = rb.gravityScale;
        float dashDir = input != null && Mathf.Abs(input.Move.x) > 0.01f ? Mathf.Sign(input.Move.x) : facing;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashPower, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originGravity;
        EndDashEnemyPassThrough();
        isDashing = false;
    }

    private void UpdateGrounded()
    {
        Vector2 checkPos = groundCheck != null ? groundCheck.position : transform.position;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(checkPos, groundCheckSize, 0f, Vector2.down, groundCheckDistance, groundMask);
        IsGrounded = Time.time - lastGroundContactTime <= groundContactGraceTime || HasGroundOverlap(checkPos);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || IsOwnCollider(hits[i].collider))
                continue;

            if (hits[i].normal.y < minGroundNormalY)
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

    private bool HasGroundOverlap(Vector2 checkPos)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(checkPos, groundCheckSize, 0f, groundMask);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null || overlap.isTrigger || IsOwnCollider(overlap))
                continue;

            if (IsOneWayPlatform(overlap) && rb != null && rb.linearVelocity.y > 0.05f)
                continue;

            return true;
        }

        return false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsGroundLayer(collision.collider))
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y < minGroundNormalY)
                continue;

            lastGroundContactTime = Time.time;
            IsGrounded = true;
            return;
        }
    }

    // 벽/계단 블록 옆면으로 계속 밀어 넣으면 2D 물리 마찰 때문에 공중에서도 붙는 느낌이 난다.
    // 이동 적용 직전에 옆면 접촉 방향 입력만 잘라서 벽 타기처럼 보이는 현상을 막는다.
    private bool IsBlockedHorizontally(float moveX)
    {
        if (Mathf.Abs(moveX) < 0.01f)
            return false;

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 direction = moveX > 0f ? Vector2.right : Vector2.left;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, wallCheckSize, 0f, direction, wallCheckDistance, groundMask);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger || IsOwnCollider(hit.collider) || IsOneWayPlatform(hit.collider))
                continue;

            if (Mathf.Abs(hit.normal.x) < minWallNormalX)
                continue;

            return true;
        }

        return false;
    }

    private bool IsOneWayPlatform(Collider2D target)
    {
        if (target == null || !target.usedByEffector)
            return false;

        return target.GetComponent<PlatformEffector2D>() != null;
    }

    private bool IsGroundLayer(Collider2D target)
    {
        if (target == null || target.isTrigger || IsOwnCollider(target))
            return false;

        int targetMask = 1 << target.gameObject.layer;
        return (groundMask.value & targetMask) != 0;
    }

    // 캐릭터 몸체 콜라이더는 벽과 마찰이 생기면 점프 중 옆면에 달라붙기 쉽다.
    // 트리거 히트박스는 공격 판정용이므로 물리 재질을 건드리지 않는다.
    private void ApplyNoFrictionMaterial()
    {
        if (ownColliders == null)
            return;

        noFrictionMaterial = new PhysicsMaterial2D("Player_NoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == null || ownColliders[i].isTrigger)
                continue;

            ownColliders[i].sharedMaterial = noFrictionMaterial;
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

    private void BeginDashEnemyPassThrough()
    {
        ignoredDashCollisions.Clear();
        if (ownColliders == null)
            return;

        Collider2D[] sceneColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider2D own = ownColliders[i];
            if (own == null || own.isTrigger)
                continue;

            for (int j = 0; j < sceneColliders.Length; j++)
            {
                Collider2D target = sceneColliders[j];
                if (target == null || target.isTrigger || IsOwnCollider(target) || !IsEnemyCollider(target))
                    continue;

                Physics2D.IgnoreCollision(own, target, true);
                ignoredDashCollisions.Add(new ColliderPair(own, target));
            }
        }
    }

    private void EndDashEnemyPassThrough()
    {
        for (int i = 0; i < ignoredDashCollisions.Count; i++)
        {
            ColliderPair pair = ignoredDashCollisions[i];
            if (pair.A != null && pair.B != null)
                Physics2D.IgnoreCollision(pair.A, pair.B, false);
        }

        ignoredDashCollisions.Clear();
    }

    private static bool IsEnemyCollider(Collider2D target)
    {
        Health targetHealth = target.GetComponentInParent<Health>();
        return targetHealth != null && targetHealth.Team == Team.Enemy;
    }

    private readonly struct ColliderPair
    {
        public readonly Collider2D A;
        public readonly Collider2D B;

        public ColliderPair(Collider2D a, Collider2D b)
        {
            A = a;
            B = b;
        }
    }
}
