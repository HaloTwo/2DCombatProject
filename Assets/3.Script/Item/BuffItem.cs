using UnityEngine;

public enum BuffItemType
{
    MoveSpeed,
    AttackPower,
    FocusGauge
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

    private bool consumed;

    private void OnEnable()
    {
        consumed = false;
    }

    // 플레이어가 닿으면 즉시 적용되는 단순 버프 아이템이다. 인벤토리 없이 영상에서 바로 효과가 보이게 한다.
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
        switch (buffType)
        {
            case BuffItemType.MoveSpeed:
                movement.SetTemporaryMoveSpeedMultiplier(moveSpeedMultiplier, duration);
                PlayerBuffTrail trail = player.GetComponent<PlayerBuffTrail>();
                if (trail == null)
                    trail = player.AddComponent<PlayerBuffTrail>();

                trail.PlaySpeedTrail(duration);
                BuffStatusView.Show(buffType, duration);
                break;

            case BuffItemType.AttackPower:
                PlayerDamageBuff damageBuff = player.GetComponent<PlayerDamageBuff>();
                if (damageBuff == null)
                    damageBuff = player.AddComponent<PlayerDamageBuff>();

                damageBuff.SetTemporaryDamageMultiplier(attackPowerMultiplier, duration);
                PlayerBuffTrail powerTrail = player.GetComponent<PlayerBuffTrail>();
                if (powerTrail == null)
                    powerTrail = player.AddComponent<PlayerBuffTrail>();

                powerTrail.PlayPowerAura(duration);
                BuffStatusView.Show(buffType, duration);
                break;

            case BuffItemType.FocusGauge:
                HUDView.Instance?.AddFocusGauge(focusGaugeAmount);
                BuffStatusView.Show(buffType, 1.2f);
                break;
        }
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
