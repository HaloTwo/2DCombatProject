using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.45f, 0f);

    private void Reset()
    {
        health = GetComponent<Health>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    // 피격 이벤트를 받아 월드 좌표에 데미지 숫자를 띄우고 콤보 UI에 히트 정보를 전달한다.
    private void HandleDamaged(Health damaged, DamageInfo info)
    {
        Vector3 position = (Vector3)info.HitPoint + spawnOffset;
        GameObject go = new GameObject("DamageText");
        go.transform.position = position;

        TextMesh text = go.AddComponent<TextMesh>();
        text.text = Mathf.RoundToInt(info.Damage).ToString();
        text.fontSize = 36;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = textColor;
        text.characterSize = 0.08f;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 80;

        go.AddComponent<WorldDamageText>();
        ComboCounterUI.Instance?.RegisterHit(info);
    }
}
