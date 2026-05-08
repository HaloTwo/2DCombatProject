using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private List<WaveData> waves = new();
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new();

    private readonly List<Health> aliveEnemies = new();
    private int currentWaveIndex = -1;
    private bool isRunning;

    private void Start()
    {
        StartWaves();
    }

    public void StartWaves()
    {
        if (isRunning) return;

        isRunning = true;
        StartCoroutine(CoRunWaves());
    }

    // WaveData를 순서대로 실행하고, 현재 웨이브의 모든 적이 죽으면 다음 웨이브로 넘어간다.
    private IEnumerator CoRunWaves()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waves.Count; currentWaveIndex++)
        {
            WaveData wave = waves[currentWaveIndex];
            if (wave == null) continue;

            yield return StartCoroutine(CoSpawnWave(wave));
            yield return new WaitUntil(() => aliveEnemies.Count == 0);
            yield return new WaitForSeconds(wave.nextWaveDelay);
        }

        isRunning = false;
        GameManager.Instance?.ClearGame();
    }

    private IEnumerator CoSpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemies.Count; i++)
        {
            WaveData.SpawnEntry entry = wave.enemies[i];
            if (entry == null || entry.enemyPrefab == null) continue;

            for (int count = 0; count < entry.count; count++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(entry.interval);
            }
        }
    }

    // 적을 생성하고 Health.OnDead를 구독해 웨이브 생존 카운트를 추적한다.
    private void SpawnEnemy(GameObject prefab)
    {
        Vector3 pos = GetSpawnPosition();
        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);

        if (!go.TryGetComponent(out Health health))
            return;

        health.ResetHealth();
        health.OnDead += HandleEnemyDead;
        aliveEnemies.Add(health);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count == 0)
            return transform.position;

        int index = Random.Range(0, spawnPoints.Count);
        return spawnPoints[index] != null ? spawnPoints[index].Position : transform.position;
    }

    private void HandleEnemyDead(Health enemy)
    {
        enemy.OnDead -= HandleEnemyDead;
        aliveEnemies.Remove(enemy);
    }
}
