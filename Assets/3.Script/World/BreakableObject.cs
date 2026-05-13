using UnityEngine;

[RequireComponent(typeof(Health))]
public class BreakableObject : MonoBehaviour
{
    [SerializeField, KoreanLabel("체력")] private Health health;
    [SerializeField, KoreanLabel("파괴 이펙트")] private GameObject breakEffectPrefab;
    [SerializeField, KoreanLabel("먼지/파편 이펙트")] private GameObject debrisPrefab;
    [SerializeField, KoreanLabel("드랍 프리팹 목록")] private GameObject[] dropPrefabs;
    [SerializeField, Range(0f, 1f), KoreanLabel("드랍 확률")] private float dropChance = 0.35f;
    [SerializeField, KoreanLabel("약한 카메라 흔들림")] private bool shakeCamera = true;
    [SerializeField, KoreanLabel("파괴 후 비활성화")] private bool deactivateOnBreak = true;

    [Header("리스폰 중복 방지")]
    [SerializeField, KoreanLabel("주변 검사 반경")] private float respawnBlockRadius = 0.65f;
    [SerializeField, KoreanLabel("아이템이 있으면 리스폰 안함")] private bool blockRespawnWhenItemExists = true;
    [SerializeField, KoreanLabel("다른 박스가 있으면 리스폰 안함")] private bool blockRespawnWhenOtherBoxExists = true;

    private bool broken;
    private Vector3 spawnPosition;

    private void Reset()
    {
        health = GetComponent<Health>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        spawnPosition = transform.position;
    }

    private void OnEnable()
    {
        broken = false;

        if (health == null)
            health = GetComponent<Health>();

        health?.ResetHealth();

        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDead -= HandleDead;
            health.OnDamaged += HandleDamaged;
            health.OnDead += HandleDead;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDead -= HandleDead;
        }
    }

    // 박스가 실제로 공격을 받았을 때 전용 타격음을 낸다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        if (!broken)
            SoundManager.Instance?.PlayRandomBoxHit();
    }

    // Health가 0이 되는 순간 월드 오브젝트 반응을 한 곳에서 처리한다.
    private void HandleDead(Health dead)
    {
        if (broken)
            return;

        broken = true;
        Vector3 position = transform.position;

        SpawnPooled(breakEffectPrefab, position);
        SpawnPooled(debrisPrefab, position);
        TryDrop(position);

        if (shakeCamera && CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.06f, 0.045f);

        if (deactivateOnBreak)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }

    private void TryDrop(Vector3 position)
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0 || Random.value > dropChance)
            return;

        GameObject prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
        SpawnPooled(prefab, position + Vector3.up * 0.25f);
    }

    private GameObject SpawnPooled(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(prefab, position, Quaternion.identity)
            : Instantiate(prefab, position, Quaternion.identity);

        if (go != null && go.TryGetComponent(out TimedAutoRelease autoRelease))
            autoRelease.Play();

        return go;
    }

    // 웨이브/스테이지가 바뀔 때 박스만 다시 살린다.
    // 단, 기존 아이템이나 다른 박스가 이미 있으면 겹침 방지를 위해 리스폰하지 않는다.
    public void Respawn()
    {
        if (gameObject.activeSelf)
            return;

        if (IsRespawnBlocked())
            return;

        broken = false;
        transform.position = spawnPosition;
        gameObject.SetActive(true);

        if (health == null)
            health = GetComponent<Health>();

        health?.ResetHealth();
    }

    private bool IsRespawnBlocked()
    {
        if (respawnBlockRadius <= 0f)
            return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPosition, respawnBlockRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            if (!hit.gameObject.activeInHierarchy)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            if (blockRespawnWhenItemExists && hit.GetComponentInParent<BuffItem>() != null)
                return true;

            if (blockRespawnWhenOtherBoxExists)
            {
                BreakableObject otherBox = hit.GetComponentInParent<BreakableObject>();

                if (otherBox != null && otherBox != this && otherBox.gameObject.activeInHierarchy)
                    return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.35f);
        Gizmos.DrawSphere(center, respawnBlockRadius);

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(center, respawnBlockRadius);
    }
#endif
}