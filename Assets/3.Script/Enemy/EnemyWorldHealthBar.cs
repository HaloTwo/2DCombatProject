using UnityEngine;

public class EnemyWorldHealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform fillRoot;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private float visibleDuration = 1.4f;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (barRoot == null) barRoot = gameObject;

        Refresh();
        Hide();
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDead += HandleDead;
        Show();
        Refresh();
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        Hide();
        health.OnDamaged -= HandleDamaged;
        health.OnDead -= HandleDead;
    }


    // 체력이 변할 때 월드 공간 체력바를 갱신하고 잠깐 표시한다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        Refresh();
        Show();
    }

    private void HandleDead(Health dead)
    {
        Hide();
    }

    private void Refresh()
    {
        if (health == null || fillRoot == null)
            return;

        float rate = health.MaxHp <= 0f ? 0f : Mathf.Clamp01(health.CurrentHp / health.MaxHp);
        Vector3 scale = fillRoot.localScale;
        scale.x = rate;
        fillRoot.localScale = scale;
    }

    private void Show()
    {
        if (barRoot == null)
            return;
        
        barRoot.SetActive(true);
    }

    private void Hide()
    {
        if (barRoot != null)
            barRoot.SetActive(false);
    }
}
