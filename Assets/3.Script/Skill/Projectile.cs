using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IPoolable, IParryReactable
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private bool keepHorizontalSlash = true;

    private Team ownerTeam;
    private AttackData attackData;
    private Vector2 direction;
    private float despawnTime;

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
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= despawnTime)
            Release();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (attackData == null) return;
        if (!other.TryGetComponent(out Hurtbox hurtbox)) return;

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
}
