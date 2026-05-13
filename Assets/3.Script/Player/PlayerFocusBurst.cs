using System.Collections;
using UnityEngine;

public class PlayerFocusBurst : MonoBehaviour
{
    [SerializeField, KoreanLabel("이동 컨트롤러")] private PlayerMovement2D movement;
    [SerializeField, KoreanLabel("체력")] private Health health;
    [SerializeField, KoreanLabel("비주얼 렌더러")] private SpriteRenderer visualRenderer;

    [Header("포커스 발동 연출")]
    [SerializeField, KoreanLabel("발동 이펙트 프리팹")] private GameObject startEffectPrefab;
    [SerializeField, KoreanLabel("발동 이펙트 유지 시간")] private float startEffectLifeTime = 1.25f;
    [SerializeField, KoreanLabel("발동 이펙트 위치 보정")] private Vector3 startEffectOffset = new Vector3(0f, 0.15f, 0f);

    [Header("발동 반경 효과")]
    [SerializeField, KoreanLabel("반경")] private float radius = 2.4f;
    [SerializeField, KoreanLabel("넉백 힘")] private float knockbackForce = 7.5f;
    [SerializeField, KoreanLabel("위쪽 힘")] private float knockbackUpForce = 1.4f;
    [SerializeField, KoreanLabel("경직 시간")] private float stunTime = 0.35f;

    private Coroutine startEffectRoutine;
    private GameObject activeStartEffect;
    private static readonly Collider2D[] hits = new Collider2D[32];

    public float IntroLifeTime => startEffectLifeTime;

    private void Reset()
    {
        BindReferences();
    }

    private void Awake()
    {
        BindReferences();
    }

    public void BeginIntro(FocusBackgroundDimmer backgroundDimmer, float enemySpeedMultiplier, float projectileSpeedMultiplier)
    {
        movement?.LockMovementForRealtime(startEffectLifeTime);
        health?.SetInvincibleFor(startEffectLifeTime + 0.05f);

        FocusModeController.BeginPreviewSlow(enemySpeedMultiplier, projectileSpeedMultiplier);
        backgroundDimmer?.Show();

        SoundManager.Instance?.PlayNamedSFX(SoundManager.SfxFocusMode, SFXType.Skill);
        PlayStartEffect();
        ApplyBurst();
    }

    public void EndIntro()
    {
        ReleaseStartEffect();
    }

    public void CancelIntro(FocusBackgroundDimmer backgroundDimmer)
    {
        FocusModeController.EndPreviewSlow();
        backgroundDimmer?.Hide();
        ReleaseStartEffect();
    }

    public Vector3 GetEffectCenter()
    {
        if (visualRenderer != null)
            return visualRenderer.bounds.center + startEffectOffset;

        return transform.TransformPoint(startEffectOffset);
    }

    private void PlayStartEffect()
    {
        if (startEffectPrefab == null)
            return;

        ReleaseStartEffect();

        Vector3 position = GetEffectCenter();
        activeStartEffect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(startEffectPrefab, position, Quaternion.identity)
            : Instantiate(startEffectPrefab, position, Quaternion.identity);

        if (activeStartEffect != null)
            startEffectRoutine = StartCoroutine(CoReleaseStartEffect(activeStartEffect));
    }

    private IEnumerator CoReleaseStartEffect(GameObject effect)
    {
        yield return new WaitForSecondsRealtime(startEffectLifeTime);

        if (effect == activeStartEffect)
            ReleaseStartEffect();
    }

    private void ReleaseStartEffect()
    {
        if (startEffectRoutine != null)
        {
            StopCoroutine(startEffectRoutine);
            startEffectRoutine = null;
        }

        if (activeStartEffect == null)
            return;

        GameObject effect = activeStartEffect;
        activeStartEffect = null;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(effect);
        else
            Destroy(effect);
    }

    // 포커스 발동 반경은 플레이어 기준 전투 효과라서 UI가 아니라 플레이어 컴포넌트에서 처리한다.
    private void ApplyBurst()
    {
        if (radius <= 0f)
            return;

        Vector2 center = GetEffectCenter();
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        int count = Physics2D.OverlapCircle(center, radius, filter, hits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            hits[i] = null;

            if (hit == null)
                continue;

            ClearProjectile(hit);
            KnockbackEnemy(hit, center);
        }
    }

    private void ClearProjectile(Collider2D hit)
    {
        Projectile projectile = hit.GetComponentInParent<Projectile>();
        if (projectile != null)
            projectile.ForceRelease();
    }

    private void KnockbackEnemy(Collider2D hit, Vector2 center)
    {
        Health enemyHealth = hit.GetComponentInParent<Health>();
        if (enemyHealth == null || enemyHealth.Team != Team.Enemy || enemyHealth.IsDead)
            return;

        Vector2 direction = ((Vector2)enemyHealth.transform.position - center).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        if (enemyHealth.TryGetComponent(out EnemyBrainBase enemyBrain))
            enemyBrain.ApplyFocusBurstKnockback(direction, knockbackForce, knockbackUpForce, stunTime);
        else if (enemyHealth.TryGetComponent(out Rigidbody2D enemyRb))
            enemyRb.linearVelocity = new Vector2(direction.x * knockbackForce, knockbackUpForce);
    }

    private void BindReferences()
    {
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (health == null) health = GetComponent<Health>();
        if (visualRenderer == null) visualRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDrawGizmosSelected()
    {
        BindReferences();

        if (radius <= 0f)
            return;

        Vector3 center = GetEffectCenter();
        Gizmos.color = new Color(1f, 0.15f, 0.85f, 0.24f);
        Gizmos.DrawSphere(center, radius);
        Gizmos.color = new Color(1f, 0.15f, 0.85f, 0.9f);
        Gizmos.DrawWireSphere(center, radius);
    }
}
