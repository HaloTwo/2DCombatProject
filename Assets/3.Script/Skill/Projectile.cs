using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IPoolable, IParryReactable
{
    [SerializeField, KoreanLabel("속도")] private float speed = 12f;
    [SerializeField, KoreanLabel("수명")] private float lifeTime = 2f;
    [SerializeField, KoreanLabel("검기 수평 유지")] private bool keepHorizontalSlash = true;
    [SerializeField, KoreanLabel("지형 충돌 시 제거")] private bool releaseOnTerrainHit = true;
    [SerializeField, KoreanLabel("포물선 중력")] private float arcGravity = 22f;
    [SerializeField, KoreanLabel("포물선 최소 비행 시간")] private float minArcFlightTime = 0.55f;
    [SerializeField, KoreanLabel("포물선 최대 비행 시간")] private float maxArcFlightTime = 1.1f;
    [SerializeField, KoreanLabel("플레이어 투사체 최대 거리")] private float playerMaxDistance = 7f;
    [SerializeField, KoreanLabel("소멸 이펙트")] private GameObject impactEffectPrefab;

    private Team ownerTeam;
    private AttackData attackData;
    private Vector2 direction;
    private Vector2 arcVelocity;
    private Vector2 spawnPosition;
    private float despawnTime;
    private Collider2D[] ownColliders;
    private bool useArcMotion;

    private void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    public void Fire(Team team, Vector2 fireDirection, AttackData data)
    {
        ownerTeam = team;
        attackData = data;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.right;
        arcVelocity = Vector2.zero;
        useArcMotion = false;
        spawnPosition = transform.position;
        despawnTime = Time.time + lifeTime;
        ApplyFacing();

        if (keepHorizontalSlash)
            transform.rotation = Quaternion.identity;
    }

    // 원거리 몬스터가 던지는 투사체용 발사 함수다. 목표 위치를 향해 포물선 궤도를 계산한다.
    public void FireArc(Team team, Vector2 targetPosition, AttackData data)
    {
        Vector2 start = transform.position;
        Vector2 toTarget = targetPosition - start;
        float horizontalDistance = Mathf.Abs(toTarget.x);
        float flightTime = Mathf.Clamp(horizontalDistance / Mathf.Max(0.1f, speed), minArcFlightTime, maxArcFlightTime);

        ownerTeam = team;
        attackData = data;
        direction = toTarget.x >= 0f ? Vector2.right : Vector2.left;
        arcVelocity = new Vector2(toTarget.x / flightTime, (toTarget.y + 0.5f * arcGravity * flightTime * flightTime) / flightTime);
        useArcMotion = true;
        spawnPosition = transform.position;
        despawnTime = Time.time + Mathf.Max(lifeTime, flightTime + 0.4f);
        ApplyFacing();
    }

    private void Update()
    {
        Vector2 currentPosition = transform.position;
        float speedMultiplier = GetProjectileSpeedMultiplier();
        Vector2 nextPosition;

        if (useArcMotion)
        {
            arcVelocity += Vector2.down * arcGravity * speedMultiplier * Time.deltaTime;
            nextPosition = currentPosition + arcVelocity * speedMultiplier * Time.deltaTime;
        }
        else
        {
            nextPosition = currentPosition + direction * speed * speedMultiplier * Time.deltaTime;
        }

        if (releaseOnTerrainHit && TryHitTerrainBetween(currentPosition, nextPosition))
            return;

        transform.position = nextPosition;

        if (ownerTeam == Team.Player && playerMaxDistance > 0f && Vector2.Distance(spawnPosition, nextPosition) >= playerMaxDistance)
        {
            ReleaseWithImpact(nextPosition);
            return;
        }

        if (Time.time >= despawnTime)
            Release();
    }

    private float GetProjectileSpeedMultiplier()
    {
        if (ownerTeam == Team.Player)
            return 1f;

        return FocusModeController.ProjectileSpeedMultiplier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (attackData == null) return;
        if (!other.TryGetComponent(out Hurtbox hurtbox))
        {
            if (releaseOnTerrainHit && IsTerrainBlocker(other))
                ReleaseWithImpact(transform.position);

            return;
        }

        float damage = ownerTeam == Team.Player ? PlayerDamageBuff.ModifyPlayerDamage(attackData.damage) : attackData.damage;
        DamageInfo info = new DamageInfo(ownerTeam, damage, other.ClosestPoint(transform.position), attackData.knockback, attackData.hitStopTime);
        if (hurtbox.ApplyDamage(info, this))
        {
            if (ownerTeam == Team.Player)
                SoundManager.Instance?.PlayRandomBladeHit();

            ReleaseWithImpact(other.ClosestPoint(transform.position));
        }
    }

    public void OnParried(Vector2 parryPoint, Vector2 parryDirection)
    {
        ownerTeam = Team.Player;
        direction = -direction;
        arcVelocity = Vector2.zero;
        useArcMotion = false;
        transform.position = parryPoint + direction * 0.25f;
        spawnPosition = transform.position;
        despawnTime = Time.time + lifeTime;
        ApplyFacing();
    }

    public void ForceRelease()
    {
        ReleaseWithImpact(transform.position);
    }

    public void OnSpawned()
    {
        if (ownColliders == null || ownColliders.Length == 0)
            ownColliders = GetComponentsInChildren<Collider2D>();

        spawnPosition = transform.position;
        despawnTime = Time.time + lifeTime;
    }

    public void OnDespawned()
    {
        attackData = null;
        arcVelocity = Vector2.zero;
        useArcMotion = false;
    }

    private void ReleaseWithImpact(Vector3 position)
    {
        SpawnImpactEffect(position);
        Release();
    }

    private void Release()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectPrefab == null)
            return;

        GameObject effect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(impactEffectPrefab, position, Quaternion.identity)
            : Instantiate(impactEffectPrefab, position, Quaternion.identity);

        if (effect != null && effect.TryGetComponent(out TimedAutoRelease autoRelease))
            autoRelease.Play();
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x == 0f ? 1f : direction.x);
        transform.localScale = scale;
    }

    // 빠른 투사체가 프레임 사이에 벽을 통과하지 않도록 이동 경로를 직접 검사한다.
    private bool TryHitTerrainBetween(Vector2 from, Vector2 to)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (!IsTerrainBlocker(hitCollider))
                continue;

            transform.position = hits[i].point;
            ReleaseWithImpact(hits[i].point);
            return true;
        }

        return false;
    }

    private bool IsTerrainBlocker(Collider2D target)
    {
        if (target == null || target.isTrigger || IsOwnCollider(target))
            return false;

        if (target.GetComponentInParent<Health>() != null)
            return false;

        return true;
    }

    private bool IsOwnCollider(Collider2D target)
    {
        if (ownColliders == null)
            return false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == target)
                return true;
        }

        return false;
    }
}
