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

    private bool broken;

    private void Reset()
    {
        health = GetComponent<Health>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        broken = false;
        health?.ResetHealth();
        if (health != null)
        {
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

    // 웨이브/스테이지가 바뀔 때 박스만 다시 살린다. 드랍/이펙트는 건드리지 않고 원래 오브젝트만 초기화한다.
    public void Respawn()
    {
        broken = false;
        gameObject.SetActive(true);
        if (health == null)
            health = GetComponent<Health>();

        health?.ResetHealth();
    }
}
