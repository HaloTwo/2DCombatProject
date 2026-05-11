using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasyEnemyRespawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> enemyPrefabs = new();
    [SerializeField] private List<Transform> spawnPoints = new();
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private bool spawnOnStart = true;

    private readonly List<Health> aliveEnemies = new();

    private void Start()
    {
        if (spawnOnStart)
            StartCoroutine(CoSpawnLoop());
    }

    private IEnumerator CoSpawnLoop()
    {
        while (enabled)
        {
            CleanupDeadReferences();

            if (aliveEnemies.Count < maxAlive)
                SpawnOne();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        if (enemyPrefabs.Count == 0)
            return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (prefab == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject enemy = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(prefab, spawnPosition, Quaternion.identity)
            : Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (!enemy.TryGetComponent(out Health health))
            return;

        health.ResetHealth();
        EnemyWorldHealthBar[] healthBars = enemy.GetComponentsInChildren<EnemyWorldHealthBar>(true);
        for (int i = 0; i < healthBars.Length; i++)
            healthBars[i].ForceShow();

        health.OnDead -= HandleEnemyDead;
        health.OnDead += HandleEnemyDead;
        aliveEnemies.Add(health);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count == 0)
            return transform.position;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        return point != null ? point.position : transform.position;
    }

    private void HandleEnemyDead(Health enemy)
    {
        enemy.OnDead -= HandleEnemyDead;
        aliveEnemies.Remove(enemy);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null)
                aliveEnemies[i].OnDead -= HandleEnemyDead;
        }

        aliveEnemies.Clear();
    }

    private void CleanupDeadReferences()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null || aliveEnemies[i].IsDead || !aliveEnemies[i].gameObject.activeInHierarchy)
                aliveEnemies.RemoveAt(i);
        }
    }
}
