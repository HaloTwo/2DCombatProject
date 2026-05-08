using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.08f;
    [SerializeField] private float defaultStrength = 0.08f;

    private Vector3 originLocalPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        originLocalPosition = transform.localPosition;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(CoShake(duration, strength));
    }

    private IEnumerator CoShake(float duration, float strength)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * strength;
            transform.localPosition = originLocalPosition + (Vector3)offset;
            yield return null;
        }

        transform.localPosition = originLocalPosition;
        shakeRoutine = null;
    }
}
