using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    [System.Serializable]
    public class PoolEntry
    {
        public GameObject prefab;
        public int prewarmCount = 8;
    }

    private class Pool
    {
        public GameObject prefab;
        public Transform root;
        public readonly Queue<GameObject> inactive = new();
    }

    [SerializeField] private List<PoolEntry> initialPools = new();

    private readonly Dictionary<GameObject, Pool> pools = new();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        for (int i = 0; i < initialPools.Count; i++)
        {
            PoolEntry entry = initialPools[i];
            if (entry == null || entry.prefab == null) continue;
            Register(entry.prefab, entry.prewarmCount);
        }
    }

    // 투사체와 이펙트처럼 반복 생성되는 오브젝트를 미리 만들어 GC 비용을 줄인다.
    public void Register(GameObject prefab, int prewarmCount)
    {
        Pool pool = GetOrCreatePool(prefab);

        for (int i = 0; i < prewarmCount; i++)
        {
            GameObject go = CreateNew(pool);
            go.SetActive(false);
            pool.inactive.Enqueue(go);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        Pool pool = GetOrCreatePool(prefab);
        GameObject go = pool.inactive.Count > 0 ? pool.inactive.Dequeue() : CreateNew(pool);

        PooledObjectTag tag = go.GetComponent<PooledObjectTag>();
        tag.OriginPrefab = prefab;

        go.transform.SetParent(null);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        go.GetComponent<IPoolable>()?.OnSpawned();
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null) return;

        PooledObjectTag tag = go.GetComponent<PooledObjectTag>();
        if (tag == null || tag.OriginPrefab == null || !pools.TryGetValue(tag.OriginPrefab, out Pool pool))
        {
            Destroy(go);
            return;
        }

        go.GetComponent<IPoolable>()?.OnDespawned();
        go.SetActive(false);
        go.transform.SetParent(pool.root);
        pool.inactive.Enqueue(go);
    }

    private Pool GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out Pool pool))
            return pool;

        GameObject root = new GameObject($"Pool_{prefab.name}");
        root.transform.SetParent(transform);

        pool = new Pool
        {
            prefab = prefab,
            root = root.transform
        };

        pools.Add(prefab, pool);
        return pool;
    }

    private GameObject CreateNew(Pool pool)
    {
        GameObject go = Instantiate(pool.prefab, pool.root);
        PooledObjectTag tag = go.GetComponent<PooledObjectTag>();
        if (tag == null) tag = go.AddComponent<PooledObjectTag>();
        tag.OriginPrefab = pool.prefab;
        return go;
    }
}
