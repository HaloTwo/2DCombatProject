using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class WorldDamageText : MonoBehaviour, IPoolable
{
    [SerializeField] private float lifeTime = 0.65f;
    [SerializeField] private Vector3 velocity = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private int sortingOrder = 80;
    [SerializeField] private int fontSize = 36;
    [SerializeField] private float characterSize = 0.08f;

    private TextMesh text;
    private Color originColor;
    private float elapsed;
    private bool isPlaying;

    private void Awake()
    {
        CacheText();
        SetupTextMesh();
    }

    public void Play(int damage, Color color)
    {
        CacheText();
        SetupTextMesh();

        elapsed = 0f;
        isPlaying = true;
        originColor = color;

        if (text != null)
        {
            text.text = damage.ToString();
            text.color = originColor;
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (text != null)
        {
            float alpha = Mathf.Clamp01(1f - elapsed / lifeTime);
            text.color = new Color(originColor.r, originColor.g, originColor.b, alpha);
        }

        if (elapsed >= lifeTime)
            Release();
    }

    public void OnSpawned()
    {
        elapsed = 0f;
        isPlaying = false;
        CacheText();
        SetupTextMesh();
    }

    public void OnDespawned()
    {
        elapsed = 0f;
        isPlaying = false;

        if (text != null)
        {
            text.text = string.Empty;
            text.color = new Color(originColor.r, originColor.g, originColor.b, 0f);
        }
    }

    private void Release()
    {
        isPlaying = false;

        if (ObjectPool.Instance != null && GetComponent<PooledObjectTag>() != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private void CacheText()
    {
        if (text == null)
            text = GetComponent<TextMesh>();
    }

    private void SetupTextMesh()
    {
        if (text == null)
            return;

        text.fontSize = fontSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer != null)
            renderer.sortingOrder = sortingOrder;
    }
}