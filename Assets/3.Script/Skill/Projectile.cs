using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IPoolable, IParryReactable
{
    [SerializeField, KoreanLabel("속도")] private float speed = 12f;
    [SerializeField, KoreanLabel("수명")] private float lifeTime = 2f;
    [SerializeField, KoreanLabel("검기 수평 유지")] private bool keepHorizontalSlash = true;
    [SerializeField, KoreanLabel("지형 충돌 시 제거")] private bool releaseOnTerrainHit = true;

    private Team ownerTeam;
    private AttackData attackData;
    private Vector2 direction;
    private float despawnTime;
    private Collider2D[] ownColliders;

    private void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    public void Fire(Team team, Vector2 fireDirection, AttackData data)
    {
        ownerTeam = team;
        attackData = data;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.right;
        despawnTime = Time.time + lifeTime;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x == 0f ? 1f : direction.x);
        transform.localScale = scale;

        if (keepHorizontalSlash)
            transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        Vector2 currentPosition = transform.position;

        //플레이어 투사체면 안느려짐
        float speedMultiplier = GetProjectileSpeedMultiplier();

        Vector2 nextPosition = currentPosition + direction * speed * speedMultiplier * Time.deltaTime;

        if (releaseOnTerrainHit && TryHitTerrainBetween(currentPosition, nextPosition))
            return;

        transform.position = nextPosition;

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
                Release();

            return;
        }

        DamageInfo info = new DamageInfo(ownerTeam, attackData.damage, other.ClosestPoint(transform.position), attackData.knockback, attackData.hitStopTime);
        if (hurtbox.ApplyDamage(info, this))
            Release();
    }

    public void OnParried(Vector2 parryPoint, Vector2 parryDirection)
    {
        ownerTeam = Team.Player;
        direction = -direction;
        transform.position = parryPoint + direction * 0.25f;
        despawnTime = Time.time + lifeTime;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x == 0f ? 1f : direction.x);
        transform.localScale = scale;
    }

    public void OnSpawned()
    {
        if (ownColliders == null || ownColliders.Length == 0)
            ownColliders = GetComponentsInChildren<Collider2D>();

        despawnTime = Time.time + lifeTime;
    }

    public void OnDespawned()
    {
        attackData = null;
    }

    private void Release()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    // 빠르게 이동하는 투사체가 프레임 사이에 벽을 뚫지 않도록 이동 경로를 직접 검사한다.
    private bool TryHitTerrainBetween(Vector2 from, Vector2 to)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (!IsTerrainBlocker(hitCollider))
                continue;

            transform.position = hits[i].point;
            Release();
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
