using UnityEngine;

public enum BuffItemType
{
    MoveSpeed,
    AttackPower,
    FocusGauge,
    Invincible
}

public class BuffItem : MonoBehaviour
{
    [SerializeField, KoreanLabel("버프 종류")] private BuffItemType buffType = BuffItemType.MoveSpeed;
    [SerializeField, KoreanLabel("지속 시간")] private float duration = 5f;
    [SerializeField, KoreanLabel("이동 속도 배율")] private float moveSpeedMultiplier = 1.35f;
    [SerializeField, KoreanLabel("공격력 배율")] private float attackPowerMultiplier = 1.4f;
    [SerializeField, KoreanLabel("포커스 충전량")] private float focusGaugeAmount = 25f;
    [SerializeField, KoreanLabel("획득 이펙트")] private GameObject pickupEffectPrefab;
    [SerializeField, KoreanLabel("획득 후 비활성화")] private bool deactivateOnPickup = true;

    private const float DefaultMoveSpeedMultiplier = 1.35f;
    private const float DefaultAttackPowerMultiplier = 1.4f;
    private const float DefaultFocusGaugeAmount = 25f;
    private const float DefaultInvincibleDuration = 3f;

    private bool consumed;

    private void OnEnable()
    {
        consumed = false;
    }

    // 플레이어가 버프를 먹는 순간 enum 종류에 맞는 효과를 적용한다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !other.TryGetComponent(out PlayerMovement2D movement))
            return;

        consumed = true;
        Apply(other.gameObject, movement);
        SpawnPickupEffect(transform.position);

        if (deactivateOnPickup)
            ReleaseOrDestroy();
    }

    private void Apply(GameObject player, PlayerMovement2D movement)
    {
        float buffDuration = GetEffectiveDuration();

        switch (buffType)
        {
            case BuffItemType.MoveSpeed:
                movement.SetTemporaryMoveSpeedMultiplier(GetEffectiveMoveSpeedMultiplier(), buffDuration);
                PlayerBuffTrail speedTrail = GetOrAddBuffTrail(player);
                speedTrail?.PlaySpeedTrail(buffDuration);
                BuffStatusView.Show(buffType, buffDuration);
                break;

            case BuffItemType.AttackPower:
                PlayerDamageBuff damageBuff = player.GetComponent<PlayerDamageBuff>();
                if (damageBuff == null)
                    damageBuff = player.AddComponent<PlayerDamageBuff>();

                damageBuff.SetTemporaryDamageMultiplier(GetEffectiveAttackPowerMultiplier(), buffDuration);
                PlayerBuffTrail powerTrail = GetOrAddBuffTrail(player);
                powerTrail?.PlayPowerAura(buffDuration);
                BuffStatusView.Show(buffType, buffDuration);
                break;

            case BuffItemType.FocusGauge:
                HUDView.Instance?.AddFocusGauge(GetEffectiveFocusGaugeAmount());
                BuffStatusView.Show(buffType, 1.2f);
                break;

            case BuffItemType.Invincible:
                player.GetComponent<Health>()?.SetInvincibleFor(buffDuration);
                BuffStatusView.Show(buffType, buffDuration);
                break;
        }
    }

    private float GetEffectiveDuration()
    {
        if (duration > 0f)
            return duration;

        return buffType == BuffItemType.Invincible ? DefaultInvincibleDuration : 5f;
    }

    private float GetEffectiveMoveSpeedMultiplier()
    {
        return moveSpeedMultiplier > 1f ? moveSpeedMultiplier : DefaultMoveSpeedMultiplier;
    }

    private float GetEffectiveAttackPowerMultiplier()
    {
        return attackPowerMultiplier > 1f ? attackPowerMultiplier : DefaultAttackPowerMultiplier;
    }

    private float GetEffectiveFocusGaugeAmount()
    {
        return focusGaugeAmount > 0f ? focusGaugeAmount : DefaultFocusGaugeAmount;
    }

    private static PlayerBuffTrail GetOrAddBuffTrail(GameObject player)
    {
        PlayerBuffTrail trail = player.GetComponent<PlayerBuffTrail>();
        if (trail == null)
            trail = player.AddComponent<PlayerBuffTrail>();

        return trail;
    }

    private void SpawnPickupEffect(Vector3 position)
    {
        if (pickupEffectPrefab == null)
            return;

        GameObject effect = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(pickupEffectPrefab, position, Quaternion.identity)
            : Instantiate(pickupEffectPrefab, position, Quaternion.identity);

        if (effect != null && effect.TryGetComponent(out TimedAutoRelease autoRelease))
            autoRelease.Play();
    }

    private void ReleaseOrDestroy()
    {
        if (ObjectPool.Instance != null && GetComponent<PooledObjectTag>() != null)
            ObjectPool.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
}
