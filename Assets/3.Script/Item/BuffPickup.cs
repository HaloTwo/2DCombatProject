using System.Collections;
using UnityEngine;

public class BuffPickup : MonoBehaviour
{
    [SerializeField, KoreanLabel("버프 타입")] private BuffPickupType buffType = BuffPickupType.Heal;
    [SerializeField, KoreanLabel("회복량")] private float healAmount = 18f;
    [SerializeField, KoreanLabel("포커스 충전량")] private float focusChargeAmount = 25f;
    [SerializeField, KoreanLabel("이동 속도 배율")] private float speedMultiplier = 1.2f;
    [SerializeField, KoreanLabel("무적 시간")] private float invincibleDuration = 2f;
    [SerializeField, KoreanLabel("버프 지속 시간")] private float buffDuration = 5f;
    [SerializeField, KoreanLabel("획득 시 비활성화")] private bool deactivateOnPickup = true;

    private bool consumed;

    private void OnEnable()
    {
        consumed = false;
    }

    // 플레이어가 아이템을 먹는 순간 효과만 적용하고, 실제 연출/사운드는 프리팹 쪽에서 붙일 수 있게 둔다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !other.TryGetComponent(out PlayerMovement2D movement))
            return;

        consumed = true;
        Apply(other.gameObject, movement);

        if (deactivateOnPickup)
            gameObject.SetActive(false);
    }

    private void Apply(GameObject player, PlayerMovement2D movement)
    {
        switch (buffType)
        {
            case BuffPickupType.Heal:
                player.GetComponent<Health>()?.Heal(healAmount);
                break;

            case BuffPickupType.FocusCharge:
                HUDView.Instance?.AddFocusGauge(focusChargeAmount);
                break;

            case BuffPickupType.SpeedBoost:
                movement.SetTemporaryMoveSpeedMultiplier(speedMultiplier, buffDuration);
                break;

            case BuffPickupType.Invincible:
                player.GetComponent<Health>()?.SetInvincibleFor(invincibleDuration);
                break;
        }
    }
}
