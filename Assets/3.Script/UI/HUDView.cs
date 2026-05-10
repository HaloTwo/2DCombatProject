using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [Header("체력")]
    [SerializeField, KoreanLabel("플레이어 체력")] private Health playerHealth;
    [SerializeField, KoreanLabel("체력 슬라이더")] private Slider hpSlider;
    [SerializeField, KoreanLabel("체력 텍스트")] private Text hpText;

    [Header("포커스 게이지")]
    [SerializeField, KoreanLabel("입력 리더")] private PlayerInputReader input;
    [SerializeField, KoreanLabel("포커스 슬라이더")] private Slider focusSlider;
    [SerializeField, KoreanLabel("포커스 Fill 이미지")] private Image focusFillImage;
    [SerializeField, KoreanLabel("준비 완료 연출 오브젝트")] private GameObject focusReadyEffect;
    [SerializeField, KoreanLabel("포커스 텍스트")] private Text focusText;
    [SerializeField, KoreanLabel("최대 게이지")] private float maxFocusGauge = 100f;
    [SerializeField, KoreanLabel("몹 처치 충전량")] private float focusChargePerEnemyKill = 22f;
    [SerializeField, KoreanLabel("포커스 지속 시간")] private float focusDuration = 4f;
    [SerializeField, KoreanLabel("포커스 중 적 속도 배율")] private float focusEnemySpeedMultiplier = 0.35f;
    [SerializeField, KoreanLabel("포커스 중 투사체 속도 배율")] private float focusProjectileSpeedMultiplier = 0.45f;

    [Header("포커스 UI 색상")]
    [SerializeField, KoreanLabel("기본 게이지 색상")] private Color focusNormalColor = new Color(0.85f, 0.15f, 0.65f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 색상 A")] private Color focusFullBlinkColorA = new Color(1f, 0.25f, 0.85f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 색상 B")] private Color focusFullBlinkColorB = new Color(1f, 0.75f, 1f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 속도")] private float focusFullBlinkSpeed = 31.4f;

    private float currentFocusGauge;

    private bool IsFocusFull => currentFocusGauge >= maxFocusGauge;

    private void Reset()
    {
        playerHealth = FindFirstObjectByType<PlayerMovement2D>()?.GetComponent<Health>();
        input = FindFirstObjectByType<PlayerInputReader>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged += HandlePlayerDamaged;

        Health.OnAnyDead += HandleAnyDead;
        FocusModeController.OnFocusChanged += HandleFocusChanged;
        FocusModeController.OnFocusProgressChanged += HandleFocusProgressChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= HandlePlayerDamaged;

        Health.OnAnyDead -= HandleAnyDead;
        FocusModeController.OnFocusChanged -= HandleFocusChanged;
        FocusModeController.OnFocusProgressChanged -= HandleFocusProgressChanged;
    }

    private void Start()
    {
        if (input == null)
            input = FindFirstObjectByType<PlayerInputReader>();

        Refresh();
    }

    private void Update()
    {
        if (input != null && input.FocusPressed)
            TryActivateFocus();

        if (IsFocusFull && !FocusModeController.IsActive)
            RefreshFocus();
    }

    private void HandlePlayerDamaged(Health health, DamageInfo info)
    {
        Refresh();

        if (health.IsDead)
            GameManager.Instance?.GameOver();
    }

    public void Refresh()
    {
        RefreshHp();
        RefreshFocus();
    }

    private void RefreshHp()
    {
        if (playerHealth == null || hpSlider == null)
            return;

        hpSlider.value = playerHealth.MaxHp <= 0f ? 0f : playerHealth.CurrentHp / playerHealth.MaxHp;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHp)} / {Mathf.CeilToInt(playerHealth.MaxHp)}";
    }

    private void RefreshFocus()
    {
        float normalized = GetFocusNormalized();

        if (focusSlider != null)
            focusSlider.value = normalized;

        RefreshFocusFill(normalized);
        RefreshFocusReadyEffect();
        RefreshFocusText(normalized);
    }

    private float GetFocusNormalized()
    {
        if (maxFocusGauge <= 0f)
            return 0f;

        return Mathf.Clamp01(currentFocusGauge / maxFocusGauge);
    }

    private void RefreshFocusFill(float normalized)
    {
        if (focusFillImage == null)
            return;

        focusFillImage.fillAmount = normalized;
        focusFillImage.color = GetFocusFillColor();
    }

    private Color GetFocusFillColor()
    {
        if (!IsFocusFull || FocusModeController.IsActive)
            return focusNormalColor;

        float blink01 = (Mathf.Sin(Time.unscaledTime * focusFullBlinkSpeed) + 1f) * 0.5f;
        return Color.Lerp(focusFullBlinkColorA, focusFullBlinkColorB, blink01);
    }

    private void RefreshFocusReadyEffect()
    {
        if (focusReadyEffect == null)
            return;

        focusReadyEffect.SetActive(IsFocusFull && !FocusModeController.IsActive);
    }

    private void RefreshFocusText(float normalized)
    {
        if (focusText == null)
            return;

        focusText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
    }

    public void AddFocusGauge(float amount)
    {
        if (amount <= 0f || FocusModeController.IsActive)
            return;

        currentFocusGauge = Mathf.Clamp(currentFocusGauge + amount, 0f, maxFocusGauge);
        RefreshFocus();
    }

    private void TryActivateFocus()
    {
        if (!IsFocusFull)
            return;

        if (FocusModeController.IsActive)
            return;

        currentFocusGauge = maxFocusGauge;
        RefreshFocus();

        FocusModeController.Activate(
            focusDuration,
            focusEnemySpeedMultiplier,
            focusProjectileSpeedMultiplier
        );
    }

    private void HandleAnyDead(Health dead)
    {
        if (dead != null && dead.Team == Team.Enemy)
            AddFocusGauge(focusChargePerEnemyKill);
    }

    private void HandleFocusChanged(bool active)
    {
        if (!active)
            currentFocusGauge = 0f;

        RefreshFocus();
    }

    private void HandleFocusProgressChanged(float normalized)
    {
        if (!FocusModeController.IsActive)
            return;

        currentFocusGauge = maxFocusGauge * Mathf.Clamp01(normalized);
        RefreshFocus();
    }
}