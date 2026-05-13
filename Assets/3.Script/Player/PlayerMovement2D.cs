using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : Singleton<PlayerMovement2D>
{
    [SerializeField, KoreanLabel("입력 리더")] private PlayerInputReader input;
    [SerializeField, KoreanLabel("리지드바디")] private Rigidbody2D rb;
    [SerializeField, KoreanLabel("체력")] private Health health;
    [SerializeField, KoreanLabel("바닥 체크 위치")] private Transform groundCheck;
    [SerializeField, KoreanLabel("지형 레이어")] private LayerMask groundMask;

    [Header("이동")]
    [SerializeField, KoreanLabel("이동 속도")] private float moveSpeed = 6.5f;
    [SerializeField, KoreanLabel("점프 힘")] private float jumpPower = 8.5f;
    [SerializeField, KoreanLabel("최대 점프 횟수")] private int maxJumpCount = 2;
    [SerializeField, KoreanLabel("코요테 타임")] private float coyoteTime = 0.1f;
    [SerializeField, KoreanLabel("점프 입력 버퍼")] private float jumpBufferTime = 0.1f;
    [SerializeField, KoreanLabel("바닥 체크 크기")] private Vector2 groundCheckSize = new Vector2(0.58f, 0.08f);
    [SerializeField, KoreanLabel("바닥 체크 거리")] private float groundCheckDistance = 0.12f;
    [SerializeField, KoreanLabel("바닥 판정 최소 노멀 Y")] private float minGroundNormalY = 0.65f;
    [SerializeField, KoreanLabel("바닥 접촉 유지 시간")] private float groundContactGraceTime = 0.08f;
    [SerializeField, KoreanLabel("벽 체크 크기")] private Vector2 wallCheckSize = new Vector2(0.68f, 1.05f);
    [SerializeField, KoreanLabel("벽 체크 거리")] private float wallCheckDistance = 0.08f;
    [SerializeField, KoreanLabel("벽 판정 최소 노멀 X")] private float minWallNormalX = 0.65f;

    [Header("대시")]
    [SerializeField, KoreanLabel("대시 힘")] private float dashPower = 13f;
    [SerializeField, KoreanLabel("대시 지속 시간")] private float dashDuration = 0.13f;
    [SerializeField, KoreanLabel("대시 쿨타임")] private float dashCooldown = 0.3f;

    [Header("원웨이 플랫폼")]
    [SerializeField, KoreanLabel("플랫폼 내려오기 충돌 무시 시간")] private float platformDropDuration = 0.28f;
    [SerializeField, KoreanLabel("플랫폼 내려오기 속도")] private float platformDropVelocity = -3.5f;

    [Header("피격")]
    [SerializeField, KoreanLabel("피격 경직 시간")] private float hitStunDuration = 0.18f;
    [SerializeField, KoreanLabel("피격 무적 시간")] private float damageInvincibleDuration = 0.65f;

    [Header("충돌 보정")]
    [SerializeField, KoreanLabel("적 충돌 무시 갱신 반경")] private float enemyCollisionIgnoreRadius = 12f;
    [SerializeField, KoreanLabel("적 충돌 무시 갱신 주기")] private float enemyCollisionIgnoreInterval = 0.12f;

    private float facing = 1f;
    private float lastGroundedTime;
    private float lastGroundContactTime = -999f;
    private float lastJumpPressedTime;
    private float nextDashTime;
    private float controlLockedUntilTime;
    private float moveSpeedMultiplier = 1f;
    private Coroutine speedBuffRoutine;
    private readonly List<MoveSpeedBuffEntry> moveSpeedBuffs = new();
    private Coroutine attackStepRoutine;
    private int jumpCount;
    private bool isDashing;
    private bool isAttackStepping;
    private bool attackStepPassesEnemies;
    private Collider2D currentOneWayPlatform;
    private Collider2D[] ownColliders;
    private PhysicsMaterial2D noFrictionMaterial;
    private readonly List<ColliderPair> ignoredDashCollisions = new();
    private readonly Collider2D[] enemyIgnoreBuffer = new Collider2D[96];
    private float nextEnemyCollisionIgnoreTime;

    public float Facing => facing;
    public bool IsDashing => isDashing;
    public bool IsGrounded { get; private set; }
    public bool IsInputLocked => IsControlLocked();
    public Health Health => health;
    public Collider2D[] Colliders => ownColliders;
    public SpriteRenderer VisualRenderer { get; private set; }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (health == null) health = GetComponent<Health>();

        ownColliders = GetComponentsInChildren<Collider2D>();
        VisualRenderer = GetComponentInChildren<SpriteRenderer>();
        ApplyNoFrictionMaterial();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;

    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        UpdateGrounded();
        CacheInputTime();

        if (IsControlLocked())
            return;

        if (input != null && input.JumpPressed && input.Move.y < -0.5f && TryDropThroughPlatform())
            return;

        if (input != null && input.DashPressed)
            TryDash();

        TryJump();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            return;
        }

        if (isDashing || input == null) return;

        if (isAttackStepping)
        {
            RefreshEnemyCollisionIgnores();
            return;
        }

        if (IsControlLocked())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RefreshEnemyCollisionIgnores();
            return;
        }

        RefreshEnemyCollisionIgnores();

        float moveX = input.Move.x;
        if (IsBlockedHorizontally(moveX))
            moveX = 0f;

        rb.linearVelocity = new Vector2(moveX * moveSpeed * moveSpeedMultiplier, rb.linearVelocity.y);
    }

    // 공격 모션 중 플레이어 입력을 잠그고, 공격 연출이 끊기지 않도록 현재 수평 속도를 정리한다.
    public void LockMovementFor(float duration)
    {
        if (duration <= 0f)
            return;

        controlLockedUntilTime = Mathf.Max(controlLockedUntilTime, Time.time + duration);
        if (!isDashing && !isAttackStepping && rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void LockMovementForRealtime(float duration)
    {
        LockMovementFor(duration);
    }

    // 평타 1타/2타에서 짧게 앞으로 밀어주는 연출용 이동이다. 일반 이동 잠금과 별도로 FixedUpdate 덮어쓰기를 피한다.
    public void ApplyAttackStep(float speed, float duration, bool passThroughEnemies = false)
    {
        if (speed <= 0f || duration <= 0f || rb == null || isDashing)
            return;

        if (attackStepRoutine != null)
        {
            StopCoroutine(attackStepRoutine);
            EndAttackStepPassThrough();
        }

        attackStepRoutine = StartCoroutine(CoAttackStep(speed, duration, passThroughEnemies));
    }

    public void StopAttackStep()
    {
        if (attackStepRoutine != null)
        {
            StopCoroutine(attackStepRoutine);
            attackStepRoutine = null;
        }

        isAttackStepping = false;
        EndAttackStepPassThrough();

        if (!isDashing && rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // 버프 아이템에서 호출한다. 여러 속도 버프를 독립 엔트리로 쌓고, 살아있는 버프 중 가장 높은 배율을 적용한다.
    public void SetTemporaryMoveSpeedMultiplier(float multiplier, float duration)
    {
        if (duration <= 0f)
            return;

        moveSpeedBuffs.Add(new MoveSpeedBuffEntry(Mathf.Max(0.1f, multiplier), Time.time + duration));
        RefreshMoveSpeedMultiplier();

        if (speedBuffRoutine != null)
            return;

        speedBuffRoutine = StartCoroutine(CoMoveSpeedBuff());
    }

    private IEnumerator CoMoveSpeedBuff()
    {
        while (moveSpeedBuffs.Count > 0)
        {
            RefreshMoveSpeedMultiplier();
            yield return null;
        }

        moveSpeedMultiplier = 1f;
        speedBuffRoutine = null;
    }

    private void RefreshMoveSpeedMultiplier()
    {
        float multiplier = 1f;
        for (int i = moveSpeedBuffs.Count - 1; i >= 0; i--)
        {
            if (Time.time >= moveSpeedBuffs[i].EndTime)
            {
                moveSpeedBuffs.RemoveAt(i);
                continue;
            }

            multiplier = Mathf.Max(multiplier, moveSpeedBuffs[i].Multiplier);
        }

        moveSpeedMultiplier = multiplier;
    }

    private IEnumerator CoAttackStep(float speed, float duration, bool passThroughEnemies)
    {
        isAttackStepping = true;
        attackStepPassesEnemies = passThroughEnemies;
        if (attackStepPassesEnemies)
            BeginDashEnemyPassThrough();

        float endTime = Time.time + duration;

        while (Time.time < endTime && !isDashing)
        {
            rb.linearVelocity = new Vector2(facing * speed, rb.linearVelocity.y);
            yield return new WaitForFixedUpdate();
        }

        if (!isDashing && rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isAttackStepping = false;
        EndAttackStepPassThrough();
        attackStepRoutine = null;
    }

    // 점프 입력 버퍼와 코요테 타임을 처리하고, 지상 점프와 공중 점프를 분리한다.
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
        SoundManager.Instance?.PlayJump();
    }

    private void TryDash()
    {
        if (Time.time < nextDashTime || isDashing || IsControlLocked())
            return;

        StartCoroutine(CoDash());
    }

    private IEnumerator CoDash()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;
        health?.SetInvincibleFor(dashDuration + 0.05f);
        BeginDashEnemyPassThrough();
        SoundManager.Instance?.PlayDash();

        float originGravity = rb.gravityScale;
        float dashDir = input != null && Mathf.Abs(input.Move.x) > 0.01f ? Mathf.Sign(input.Move.x) : facing;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashPower, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originGravity;
        EndDashEnemyPassThrough();
        isDashing = false;
    }

    // 박스캐스트와 겹침 검사를 함께 사용해 One-Way 플랫폼 위에서도 안정적으로 접지 상태를 유지한다.
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
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(checkPos, groundCheckSize, 0f);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null || overlap.isTrigger || IsOwnCollider(overlap) || IsEnemyCollider(overlap))
                continue;

            bool isOneWay = IsOneWayPlatform(overlap);
            if (!isOneWay)
            {
                int targetMask = 1 << overlap.gameObject.layer;
                if ((groundMask.value & targetMask) == 0)
                    continue;
            }
            else if (rb != null && rb.linearVelocity.y > 0.05f)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsEnemyCollider(collision.collider))
        {
            IgnoreCollisionWithEnemy(collision.collider);
            return;
        }

        if (!IsGroundLayer(collision.collider))
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y < minGroundNormalY)
                continue;

            lastGroundContactTime = Time.time;
            IsGrounded = true;
            if (IsOneWayPlatform(collision.collider))
                currentOneWayPlatform = collision.collider;
            return;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == currentOneWayPlatform)
            currentOneWayPlatform = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsEnemyCollider(collision.collider))
            IgnoreCollisionWithEnemy(collision.collider);
    }

    // 수평 이동 방향을 실제로 막는 벽만 차단한다. 뒤쪽 벽이나 코너 겹침 때문에 이동이 멈추지 않게 한다.
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
            if (hit.collider == null || hit.collider.isTrigger || IsOwnCollider(hit.collider) || IsEnemyCollider(hit.collider) || IsOneWayPlatform(hit.collider))
                continue;

            if (Vector2.Dot(hit.normal, direction) > -minWallNormalX)
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
        if (target == null || target.isTrigger || IsOwnCollider(target) || IsEnemyCollider(target))
            return false;

        if (IsOneWayPlatform(target))
            return true;

        int targetMask = 1 << target.gameObject.layer;
        return (groundMask.value & targetMask) != 0;
    }

    // 아래 입력과 점프를 같이 눌렀을 때 현재 밟은 One-Way 플랫폼을 잠깐 통과한다.
    private bool TryDropThroughPlatform()
    {
        if (!IsGrounded || currentOneWayPlatform == null || ownColliders == null)
            return false;

        StartCoroutine(CoDropThroughPlatform(currentOneWayPlatform));
        lastJumpPressedTime = -999f;
        return true;
    }

    private IEnumerator CoDropThroughPlatform(Collider2D platform)
    {
        if (platform == null)
            yield break;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] != null && !ownColliders[i].isTrigger)
                Physics2D.IgnoreCollision(ownColliders[i], platform, true);
        }

        currentOneWayPlatform = null;
        IsGrounded = false;
        lastGroundedTime = -999f;
        lastGroundContactTime = -999f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, platformDropVelocity);

        yield return new WaitForSeconds(platformDropDuration);

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] != null && platform != null && !ownColliders[i].isTrigger)
                Physics2D.IgnoreCollision(ownColliders[i], platform, false);
        }
    }

    // 마찰 때문에 벽이나 블록 모서리에 붙는 느낌을 줄이기 위해 캐릭터 몸체 콜라이더에 무마찰 재질을 적용한다.
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
        if (IsControlLocked()) return;
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

                bool wasIgnored = Physics2D.GetIgnoreCollision(own, target);
                Physics2D.IgnoreCollision(own, target, true);
                ignoredDashCollisions.Add(new ColliderPair(own, target, wasIgnored));
            }
        }
    }

    private void EndDashEnemyPassThrough()
    {
        for (int i = 0; i < ignoredDashCollisions.Count; i++)
        {
            ColliderPair pair = ignoredDashCollisions[i];
            if (pair.A != null && pair.B != null)
                Physics2D.IgnoreCollision(pair.A, pair.B, IsEnemyCollider(pair.B) || pair.WasIgnored);
        }

        ignoredDashCollisions.Clear();
    }

    // 플레이어와 적은 길막 물리가 없어야 한다. 스폰/풀/대시 타이밍에 충돌 상태가 풀려도 여기서 다시 고정한다.
    private void RefreshEnemyCollisionIgnores()
    {
        if (Time.time < nextEnemyCollisionIgnoreTime || enemyCollisionIgnoreRadius <= 0f)
            return;

        nextEnemyCollisionIgnoreTime = Time.time + Mathf.Max(0.02f, enemyCollisionIgnoreInterval);
        Vector2 center = rb != null ? rb.position : (Vector2)transform.position;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.OverlapCircle(center, enemyCollisionIgnoreRadius, filter, enemyIgnoreBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D enemyCollider = enemyIgnoreBuffer[i];
            enemyIgnoreBuffer[i] = null;

            if (enemyCollider == null || !IsEnemyCollider(enemyCollider))
                continue;

            IgnoreCollisionWithEnemy(enemyCollider);
        }
    }

    private void IgnoreCollisionWithEnemy(Collider2D enemyCollider)
    {
        if (enemyCollider == null || enemyCollider.isTrigger || ownColliders == null)
            return;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider2D own = ownColliders[i];
            if (own == null || own.isTrigger)
                continue;

            Physics2D.IgnoreCollision(own, enemyCollider, true);
        }
    }

    private void EndAttackStepPassThrough()
    {
        if (!attackStepPassesEnemies || isDashing)
            return;

        EndDashEnemyPassThrough();
        attackStepPassesEnemies = false;
    }

    private static bool IsEnemyCollider(Collider2D target)
    {
        Health targetHealth = target.GetComponentInParent<Health>();
        return targetHealth != null && targetHealth.Team == Team.Enemy;
    }

    // 피격 직후에는 입력 경직과 무적 시간을 적용해 몸박/공격 중복 데미지를 막는다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        if (damageInvincibleDuration > 0f)
            damaged.SetInvincibleFor(damageInvincibleDuration);

        if (hitStunDuration <= 0f)
            return;

        controlLockedUntilTime = Mathf.Max(controlLockedUntilTime, Time.time + hitStunDuration);
        if (!isDashing && rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private bool IsControlLocked()
    {
        return Time.time < controlLockedUntilTime;
    }

    private readonly struct ColliderPair
    {
        public readonly Collider2D A;
        public readonly Collider2D B;
        public readonly bool WasIgnored;

        public ColliderPair(Collider2D a, Collider2D b, bool wasIgnored)
        {
            A = a;
            B = b;
            WasIgnored = wasIgnored;
        }
    }

    private readonly struct MoveSpeedBuffEntry
    {
        public readonly float Multiplier;
        public readonly float EndTime;

        public MoveSpeedBuffEntry(float multiplier, float endTime)
        {
            Multiplier = multiplier;
            EndTime = endTime;
        }
    }
}
