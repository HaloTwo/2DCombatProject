using UnityEngine;

public class EnemyWorldHealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Transform fillRoot;
    [SerializeField] private Transform backRoot;
    [SerializeField] private GameObject barRoot;

    private Vector3 baseFillScale;
    private Vector3 baseFillPosition;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer backRenderer;
    private bool hasBaseFillTransform;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (barRoot == null) barRoot = gameObject;
        if (backRoot == null) backRoot = transform.Find("Back");
        if (fillRoot == null) fillRoot = transform.Find("FillAnchor/Fill");
        CacheFillTransform();

        Refresh();
        Show();
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnDead += HandleDead;
        Refresh();
        Show();
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
        ApplyFillByBack(rate);
    }

    // 프리팹에서 잡아둔 Fill의 크기와 위치를 기준으로 보존한다.
    // 체력 변화 시에는 X 스케일과 X 위치만 조정해서 왼쪽부터 줄어드는 것처럼 보이게 한다.
    private void ApplyFill(float rate)
    {
        if (!hasBaseFillTransform)
            CacheFillTransform();

        Vector3 scale = baseFillScale;
        scale.x = baseFillScale.x * rate;
        fillRoot.localScale = scale;

        fillRoot.localPosition = baseFillPosition;
        AlignFillLeftToBackLeft();
    }

    private void CacheFillTransform()
    {
        if (fillRoot == null)
            return;

        if (fillRenderer == null) fillRenderer = fillRoot.GetComponent<SpriteRenderer>();
        if (backRenderer == null && backRoot != null) backRenderer = backRoot.GetComponent<SpriteRenderer>();

        baseFillScale = fillRoot.localScale;
        baseFillPosition = fillRoot.localPosition;
        hasBaseFillTransform = true;
    }

    private void AlignFillLeftToBackLeft()
    {
        if (fillRenderer == null || backRenderer == null)
            return;

        float deltaX = backRenderer.bounds.min.x - fillRenderer.bounds.min.x;
        fillRoot.position += new Vector3(deltaX, 0f, 0f);
    }

    // Back과 같은 높이/폭을 기준으로 Fill을 왼쪽부터 채운다.
    // 프리팹에 저장된 Fill 크기가 틀어져 있어도 여기서 매번 Back 기준으로 강제 보정한다.
    private void ApplyFillByBack(float rate)
    {
        if (fillRoot == null || backRoot == null)
            return;

        Vector3 backScale = backRoot.localScale;
        Vector3 backPosition = backRoot.localPosition;
        Transform fillAnchor = fillRoot.parent;

        if (fillAnchor != null)
            fillAnchor.localPosition = new Vector3(backPosition.x - backScale.x * 0.5f, backPosition.y, fillAnchor.localPosition.z);

        fillRoot.localScale = new Vector3(backScale.x * rate, backScale.y, backScale.z);
        fillRoot.localPosition = new Vector3(backScale.x * rate * 0.5f, 0f, fillRoot.localPosition.z);
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
