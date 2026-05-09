using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text hpText;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged += HandlePlayerDamaged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= HandlePlayerDamaged;
    }

    private void HandlePlayerDamaged(Health health, DamageInfo info)
    {
        Refresh();

        if (health.IsDead)
            GameManager.Instance?.GameOver();
    }

    public void Refresh()
    {
        if (playerHealth == null || hpSlider == null) return;
        hpSlider.value = playerHealth.MaxHp <= 0f ? 0f : playerHealth.CurrentHp / playerHealth.MaxHp;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHp)} / {Mathf.CeilToInt(playerHealth.MaxHp)}";
    }
}
