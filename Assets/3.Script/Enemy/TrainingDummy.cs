using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    [SerializeField] private Health health;

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
        if (health != null)
            health.OnDead += HandleDead;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDead -= HandleDead;
    }

    // 허수아비는 과제 초반 타격감 확인용이므로 죽어도 바로 체력을 회복해 계속 때릴 수 있게 둔다.
    private void HandleDead(Health dead)
    {
        health.ResetHealth();
    }
}
