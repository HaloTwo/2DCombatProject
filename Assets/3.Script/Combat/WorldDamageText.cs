using UnityEngine;

public class WorldDamageText : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.65f;
    [SerializeField] private Vector3 velocity = new Vector3(0f, 1.45f, 0f);

    private TextMesh text;
    private Color originColor;
    private float elapsed;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        originColor = text != null ? text.color : Color.white;
    }

    // 데미지 숫자가 위로 떠오르면서 사라지게 하는 간단한 월드 텍스트 연출이다.
    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (text != null)
        {
            float alpha = Mathf.Clamp01(1f - elapsed / lifeTime);
            text.color = new Color(originColor.r, originColor.g, originColor.b, alpha);
        }

        if (elapsed >= lifeTime)
            Destroy(gameObject);
    }
}
