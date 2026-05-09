using System.Collections;
using UnityEngine;

public class TimedAutoRelease : MonoBehaviour, IPoolable
{
    [SerializeField] private float lifeTime = 0.35f;

    private Coroutine routine;

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(CoRelease());
    }

    public void OnSpawned()
    {
        Play();
    }

    public void OnDespawned()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = null;
    }

    private IEnumerator CoRelease()
    {
        yield return new WaitForSeconds(lifeTime);

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
