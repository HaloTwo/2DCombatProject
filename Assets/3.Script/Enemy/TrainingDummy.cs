using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D rb;

    private Vector3 spawnPosition;
    private bool hasLockedPosition;

    private void Reset()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDead += HandleDead;
    }

    private void Start()
    {
        LockCurrentPosition();
    }

    private void LateUpdate()
    {
        if (!hasLockedPosition)
            LockCurrentPosition();

        transform.position = spawnPosition;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDead -= HandleDead;
    }

    // 배치가 끝난 현재 위치를 기준점으로 잠근다. 프리팹 인스턴스가 원점으로 되돌아가지 않게 한다.
    private void LockCurrentPosition()
    {
        spawnPosition = transform.position;
        hasLockedPosition = true;
    }

    // 허수아비는 과제 초반 타격감 확인용이므로 죽어도 바로 체력을 복구해 계속 때릴 수 있게 둔다.
    private void HandleDead(Health dead)
    {
        health.ResetHealth();
    }
}
