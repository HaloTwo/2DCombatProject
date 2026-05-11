using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : Singleton<HUDView>
{
    [Header("포커스 발동 연출")]
    [SerializeField, KoreanLabel("플레이어 발동 연출")] private PlayerFocusBurst playerFocusBurst;
    [SerializeField, KoreanLabel("배경 암전 연출")] private FocusBackgroundDimmer focusBackgroundDimmer;

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

    [Header("포커스 획득 연출")]
    [SerializeField, KoreanLabel("오브 프리팹")] private GameObject focusOrbPrefab;
    [SerializeField, KoreanLabel("오브 색상")] private Color focusOrbColor = new Color(1f, 0.18f, 0.85f, 1f);
    [SerializeField, KoreanLabel("처치당 오브 개수")] private int focusOrbCount = 5;
    [SerializeField, KoreanLabel("오브 이동 시간")] private float focusOrbFlyDuration = 0.55f;
    [SerializeField, KoreanLabel("오브 포물선 높이")] private float focusOrbArcHeight = 0.45f;
    [SerializeField, KoreanLabel("오브 순차 출발 간격")] private float focusOrbStagger = 0.035f;
    [SerializeField, KoreanLabel("오브 시작 원형 반경")] private float focusOrbSpawnRadius = 0.38f;
    [SerializeField, KoreanLabel("오브 시작 반경 흔들림")] private float focusOrbSpawnRadiusJitter = 0.16f;
    [SerializeField, KoreanLabel("오브 도착 위치 보정")] private Vector3 focusOrbTargetOffset = new Vector3(0f, 0.65f, 0f);

    [Header("포커스 UI 색상")]
    [SerializeField, KoreanLabel("기본 게이지 색상")] private Color focusNormalColor = new Color(0.85f, 0.15f, 0.65f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 색상 A")] private Color focusFullBlinkColorA = new Color(1f, 0.25f, 0.85f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 색상 B")] private Color focusFullBlinkColorB = new Color(1f, 0.75f, 1f, 1f);
    [SerializeField, KoreanLabel("가득 참 깜빡임 속도")] private float focusFullBlinkSpeed = 31.4f;

    private float currentFocusGauge;
    private SpriteRenderer playerVisualRenderer;
    private bool subscribedPlayerHealth;
    private Coroutine focusIntroRoutine;
    private bool isFocusIntroPlaying;

    private bool IsFocusFull => currentFocusGauge >= maxFocusGauge;
    protected override void Awake()
    {
        base.Awake();
    }

    private void Reset()
    {
        playerHealth = FindFirstObjectByType<PlayerMovement2D>()?.GetComponent<Health>();
        input = FindFirstObjectByType<PlayerInputReader>();
    }

    private void OnEnable()
    {
        SubscribePlayerHealth();

        Health.OnAnyDead += HandleAnyDead;
        FocusModeController.OnFocusChanged += HandleFocusChanged;
        FocusModeController.OnFocusProgressChanged += HandleFocusProgressChanged;

        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribePlayerHealth();
        StopFocusIntro();

        Health.OnAnyDead -= HandleAnyDead;
        FocusModeController.OnFocusChanged -= HandleFocusChanged;
        FocusModeController.OnFocusProgressChanged -= HandleFocusProgressChanged;
    }

    private void Start()
    {
        PlayerMovement2D player = PlayerMovement2D.Instance;
        if (playerHealth == null && player != null)
            playerHealth = player.Health;

        if (playerFocusBurst == null && player != null)
            playerFocusBurst = player.GetComponent<PlayerFocusBurst>();

        if (input == null)
            input = player != null ? player.GetComponent<PlayerInputReader>() : null;

        SubscribePlayerHealth();
        Refresh();
    }

    private void SubscribePlayerHealth()
    {
        if (subscribedPlayerHealth || playerHealth == null)
            return;

        playerHealth.OnDamaged += HandlePlayerDamaged;
        subscribedPlayerHealth = true;
    }

    private void UnsubscribePlayerHealth()
    {
        if (!subscribedPlayerHealth || playerHealth == null)
            return;

        playerHealth.OnDamaged -= HandlePlayerDamaged;
        subscribedPlayerHealth = false;
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

        if (FocusModeController.IsActive || isFocusIntroPlaying)
            return;

        currentFocusGauge = maxFocusGauge;
        RefreshFocus();

        focusIntroRoutine = StartCoroutine(CoFocusIntro());
    }

    private IEnumerator CoFocusIntro()
    {
        isFocusIntroPlaying = true;
        PlayerFocusBurst burst = GetPlayerFocusBurst();
        float introLifeTime = burst != null ? burst.IntroLifeTime : 1.25f;
        burst?.BeginIntro(GetFocusBackgroundDimmer(), focusEnemySpeedMultiplier, focusProjectileSpeedMultiplier);

        yield return new WaitForSecondsRealtime(introLifeTime);

        FocusModeController.Activate(
            focusDuration,
            focusEnemySpeedMultiplier,
            focusProjectileSpeedMultiplier
        );

        isFocusIntroPlaying = false;
        focusIntroRoutine = null;
    }

    // 포커스 발동 순간 플레이어 중심에서 이펙트를 보여주고, 지정 시간 뒤 풀로 반환한다.
    /*
    private void PlayFocusStartEffect()
    {
        if (focusStartEffectPrefab == null)
            return;

        ReleaseFocusStartEffect();

        Vector3 position = GetPlayerEffectCenter();
        activeFocusStartEffect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(focusStartEffectPrefab, position, Quaternion.identity)
            : Instantiate(focusStartEffectPrefab, position, Quaternion.identity);

        if (activeFocusStartEffect == null)
            return;

        focusStartEffectRoutine = StartCoroutine(CoReleaseFocusStartEffect(activeFocusStartEffect));
    }

    private Vector3 GetPlayerEffectCenter()
    {
        SpriteRenderer visual = GetPlayerVisualRenderer();
        if (visual != null)
            return visual.bounds.center + focusStartEffectOffset;

        return playerHealth != null
            ? playerHealth.transform.TransformPoint(focusStartEffectOffset)
            : transform.position;
    }

    // 포커스 발동 파티클이 터지는 순간 주변 적은 밀어내고, 접근 중인 적 투사체는 지운다.
    private void ApplyFocusBurstAroundPlayer()
    {
        if (focusStartKnockbackRadius <= 0f || playerHealth == null)
            return;

        Vector2 center = GetPlayerEffectCenter();
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false
        };
        int count = Physics2D.OverlapCircle(center, focusStartKnockbackRadius, filter, focusKnockbackHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = focusKnockbackHits[i];
            focusKnockbackHits[i] = null;

            if (hit == null)
                continue;

            ClearProjectileInFocusBurst(hit);
            if (hit.isTrigger)
                continue;

            Health enemyHealth = hit.GetComponentInParent<Health>();
            if (enemyHealth == null || enemyHealth.Team != Team.Enemy || enemyHealth.IsDead)
                continue;

            Vector2 direction = ((Vector2)enemyHealth.transform.position - center).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = playerHealth.transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

            if (enemyHealth.TryGetComponent(out EnemyBrainBase enemyBrain))
                enemyBrain.ApplyFocusBurstKnockback(direction, focusStartKnockbackForce, focusStartKnockbackUpForce, focusStartKnockbackStunTime);
            else if (enemyHealth.TryGetComponent(out Rigidbody2D enemyRb))
                enemyRb.linearVelocity = new Vector2(direction.x * focusStartKnockbackForce, focusStartKnockbackUpForce);
        }
    }

    private void ClearProjectileInFocusBurst(Collider2D hit)
    {
        Projectile projectile = hit.GetComponentInParent<Projectile>();
        if (projectile != null)
            projectile.ForceRelease();
    }

    private IEnumerator CoReleaseFocusStartEffect(GameObject effect)
    {
        yield return new WaitForSecondsRealtime(focusStartEffectLifeTime);

        if (effect == activeFocusStartEffect)
            ReleaseFocusStartEffect();
    }

    private void ReleaseFocusStartEffect()
    {
        if (focusStartEffectRoutine != null)
        {
            StopCoroutine(focusStartEffectRoutine);
            focusStartEffectRoutine = null;
        }

        if (activeFocusStartEffect == null)
            return;

        GameObject effect = activeFocusStartEffect;
        activeFocusStartEffect = null;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(effect);
        else
            Destroy(effect);
    }

    */
    private FocusBackgroundDimmer GetFocusBackgroundDimmer()
    {
        if (focusBackgroundDimmer != null)
            return focusBackgroundDimmer;

        focusBackgroundDimmer = FocusBackgroundDimmer.Instance;
        return focusBackgroundDimmer;
    }

    private PlayerFocusBurst GetPlayerFocusBurst()
    {
        if (playerFocusBurst != null)
            return playerFocusBurst;

        PlayerMovement2D player = PlayerMovement2D.Instance;
        playerFocusBurst = player != null ? player.GetComponent<PlayerFocusBurst>() : null;
        return playerFocusBurst;
    }

    private void StopFocusIntro()
    {
        if (focusIntroRoutine != null)
        {
            StopCoroutine(focusIntroRoutine);
            focusIntroRoutine = null;
        }

        isFocusIntroPlaying = false;
        FocusModeController.EndPreviewSlow();
        GetPlayerFocusBurst()?.CancelIntro(GetFocusBackgroundDimmer());
        GetFocusBackgroundDimmer()?.Hide();
    }

    private void HandleAnyDead(Health dead)
    {
        if (dead == null || dead.Team != Team.Enemy)
            return;

        SpawnFocusOrbs(dead.transform.position);
    }

    private void SpawnFocusOrbs(Vector3 enemyPosition)
    {
        if (FocusModeController.IsActive)
            return;

        Transform target = playerHealth != null ? playerHealth.transform : null;
        SpriteRenderer targetRenderer = GetPlayerVisualRenderer();
        if (target == null || focusOrbCount <= 0)
        {
            AddFocusGauge(focusChargePerEnemyKill);
            return;
        }

        float chargePerOrb = focusChargePerEnemyKill / focusOrbCount;
        for (int i = 0; i < focusOrbCount; i++)
        {
            Vector3 spawnPosition = GetFocusOrbSpawnPosition(enemyPosition, i);
            SpawnFocusOrb(spawnPosition, target, targetRenderer, chargePerOrb, i * focusOrbStagger);
        }
    }

    private Vector3 GetFocusOrbSpawnPosition(Vector3 center, int index)
    {
        float count = Mathf.Max(1, focusOrbCount);
        float baseAngle = (360f / count) * index;
        float angle = (baseAngle + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
        float radius = Mathf.Max(0f, focusOrbSpawnRadius + Random.Range(-focusOrbSpawnRadiusJitter, focusOrbSpawnRadiusJitter));
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return center + new Vector3(offset.x, offset.y, 0f);
    }

    private void SpawnFocusOrb(Vector3 spawnPosition, Transform target, SpriteRenderer targetRenderer, float chargePerOrb, float delay)
    {
        GameObject go = null;
        if (focusOrbPrefab != null && ObjectPool.Instance != null)
            go = ObjectPool.Instance.Get(focusOrbPrefab, spawnPosition, Quaternion.identity);
        else if (focusOrbPrefab != null)
            go = Instantiate(focusOrbPrefab, spawnPosition, Quaternion.identity);

        if (go == null || !go.TryGetComponent(out FocusGaugeOrb orb))
        {
            AddFocusGauge(chargePerOrb);
            return;
        }

        orb.Play(
            spawnPosition,
            target,
            targetRenderer,
            focusOrbTargetOffset,
            this,
            chargePerOrb,
            focusOrbColor,
            focusOrbFlyDuration,
            focusOrbArcHeight,
            delay
        );
    }

    // 플레이어 루트 피벗은 발밑 기준이라, 오브는 실제 캐릭터 스프라이트의 월드 중심으로 흡수시킨다.
    private SpriteRenderer GetPlayerVisualRenderer()
    {
        if (playerVisualRenderer != null)
            return playerVisualRenderer;

        if (playerHealth == null)
            return null;

        PlayerMovement2D player = PlayerMovement2D.Instance;
        playerVisualRenderer = player != null ? player.VisualRenderer : playerHealth.GetComponentInChildren<SpriteRenderer>();
        return playerVisualRenderer;
    }

    private void HandleFocusChanged(bool active)
    {
        if (active)
        {
            GetFocusBackgroundDimmer()?.Show();
        }
        else
        {
            currentFocusGauge = 0f;
            GetFocusBackgroundDimmer()?.Hide();
        }

        RefreshFocus();
    }

    private void HandleFocusProgressChanged(float normalized)
    {
        if (!FocusModeController.IsActive)
            return;

        currentFocusGauge = maxFocusGauge * Mathf.Clamp01(normalized);
        RefreshFocus();
    }

    /*
    private void OnDrawGizmosSelected()
    {
        if (focusStartKnockbackRadius <= 0f)
            return;

        Vector3 center = playerHealth != null ? GetPlayerEffectCenter() : transform.position;
        Gizmos.color = new Color(1f, 0.15f, 0.85f, 0.28f);
        Gizmos.DrawSphere(center, focusStartKnockbackRadius);
        Gizmos.color = new Color(1f, 0.15f, 0.85f, 0.9f);
        Gizmos.DrawWireSphere(center, focusStartKnockbackRadius);
    }
    */
}
