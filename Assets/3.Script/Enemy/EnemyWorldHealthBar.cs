using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class EnemyWorldHealthBar : MonoBehaviour
{
    [Header("체력바 UI")]
    [SerializeField, KoreanLabel("대상 체력")] private Health health;
    [SerializeField, KoreanLabel("체력바 루트")] private GameObject barRoot;
    [SerializeField, KoreanLabel("월드 캔버스")] private Canvas canvas;
    [SerializeField, KoreanLabel("체력 Fill 이미지")] private Image fillImage;
    [SerializeField, KoreanLabel("체력 Back 이미지")] private Image backImage;
    [SerializeField, KoreanLabel("사망 시 숨김")] private bool hideOnDead = true;

    [Header("회전 보정")]
    [SerializeField, KoreanLabel("월드 회전 고정")] private bool keepWorldRotation = true;
    [SerializeField, KoreanLabel("부모 반전 보정")] private bool counterParentFlip = true;

    private Graphic[] graphics;
    private Vector3 baseLocalScale;
    private bool hasBaseLocalScale;

    private void Awake()
    {
        CacheBaseTransform();
        BindReferences();
        Refresh();
        Show();
    }

    private void OnValidate()
    {
        CacheBaseTransform();
        BindReferences();
        Refresh();
        StabilizeCanvasTransform();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            Refresh();
            return;
        }

        if (health == null)
            return;

        health.OnDamaged += HandleDamaged;
        health.OnChanged += HandleChanged;
        health.OnDead += HandleDead;
        Refresh();
        Show();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (health == null)
            return;

        Hide();
        health.OnDamaged -= HandleDamaged;
        health.OnChanged -= HandleChanged;
        health.OnDead -= HandleDead;
    }

    private void LateUpdate()
    {
        StabilizeCanvasTransform();

        if (Application.isPlaying)
            Refresh();
    }

    // 체력이 변할 때 월드 공간 체력바를 갱신하고 잠깐 표시한다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        Refresh();
        Show();
    }

    // 풀링으로 체력이 리셋되거나 값이 바뀔 때 체력바를 즉시 다시 채운다.
    private void HandleChanged(Health changed)
    {
        Refresh();
        Show();
    }

    private void HandleDead(Health dead)
    {
        Refresh();
        Hide();
    }

    // 스폰 시스템이 비활성화된 체력바를 다시 살릴 때 호출한다.
    public void ForceShow()
    {
        if (barRoot != null && !barRoot.activeSelf)
            barRoot.SetActive(true);

        BindReferences();
        Refresh();
        Show();
    }

    private void Refresh()
    {
        if (fillImage == null)
            return;

        float rate = !Application.isPlaying || health == null || health.MaxHp <= 0f ? 1f : Mathf.Clamp01(health.CurrentHp / health.MaxHp);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = rate;
    }

    private void BindReferences()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (barRoot == null) barRoot = gameObject;
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (backImage == null)
        {
            Transform back = FindChildRecursive(transform, "Back");
            if (back != null) backImage = back.GetComponent<Image>();
        }
        if (fillImage == null)
        {
            Transform fill = FindChildRecursive(transform, "Fill");
            if (fill != null) fillImage = fill.GetComponent<Image>();
        }
        if (graphics == null || graphics.Length == 0) graphics = GetComponentsInChildren<Graphic>(true);
    }

    // 비주얼 하위에 체력바가 있어도 부모 회전/좌우 반전을 따라가지 않게 월드 표시 방향을 고정한다.
    private void StabilizeCanvasTransform()
    {
        if (!hasBaseLocalScale)
            CacheBaseTransform();

        if (keepWorldRotation)
            transform.rotation = Quaternion.identity;

        if (!counterParentFlip || transform.parent == null)
            return;

        Vector3 parentScale = transform.parent.lossyScale;
        Vector3 nextScale = baseLocalScale;
        nextScale.x = Mathf.Abs(baseLocalScale.x) * SignOrOne(parentScale.x);
        nextScale.y = Mathf.Abs(baseLocalScale.y) * SignOrOne(parentScale.y);
        transform.localScale = nextScale;
    }

    private void CacheBaseTransform()
    {
        if (hasBaseLocalScale)
            return;

        baseLocalScale = transform.localScale;
        hasBaseLocalScale = true;
    }

    private static float SignOrOne(float value)
    {
        return value < 0f ? -1f : 1f;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void Show()
    {
        if (barRoot != null && !barRoot.activeSelf)
            barRoot.SetActive(true);

        if (canvas != null)
            canvas.enabled = true;

        if (graphics == null || graphics.Length == 0)
            BindReferences();

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].enabled = true;
        }
    }

    private void Hide()
    {
        if (!hideOnDead)
            return;

        if (canvas != null)
            canvas.enabled = false;

        if (graphics == null || graphics.Length == 0)
            BindReferences();

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].enabled = false;
        }
    }
}
