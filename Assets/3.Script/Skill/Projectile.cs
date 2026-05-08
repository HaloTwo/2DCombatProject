using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;

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
        if (hurtbox.ApplyDamage(info))
            Release();
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
