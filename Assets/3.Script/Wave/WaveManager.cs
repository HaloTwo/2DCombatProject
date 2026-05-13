using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WaveManager : Singleton<WaveManager>
{
    [SerializeField] private List<WaveData> waves = new();
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new();
    [SerializeField] private float waveIntroTime = 1f;
    [SerializeField] private float waveClearHoldTime = 1f;
    [SerializeField] private int countdownStart = 5;
    [SerializeField, KoreanLabel("첫 웨이브 카운트다운")] private int firstWaveCountdownStart = 15;

    private readonly List<Health> aliveEnemies = new();
    private int currentWaveIndex = -1;
    private int currentWaveTotal;
    private int currentWaveKilled;
    private bool isRunning;

    public static event Action<int> OnWaveCleared;
    public static event Action OnAllWavesCleared;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (Instance != this)
            return;

        StartWaves();
    }

    public void StartWaves()
    {
        if (isRunning) return;

        ClearAliveEnemySubscriptions();
        currentWaveIndex = -1;
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

            RespawnBreakableObjects();
            currentWaveTotal = CountWaveEnemies(wave);
            currentWaveKilled = 0;

            if (wave.isBossWave)
                WaveAnnounceUI.ShowBossWaveStartGlobal(currentWaveIndex, currentWaveTotal, wave.bossName);
            else
                WaveAnnounceUI.ShowWaveStartGlobal(currentWaveIndex, currentWaveTotal);

            yield return new WaitForSecondsRealtime(waveIntroTime);

            int currentCountdownStart = GetCountdownStart();

            for (int count = currentCountdownStart; count > 0; count--)
            {
                WaveAnnounceUI.ShowCountdownGlobal(count, currentWaveIndex);
                yield return new WaitForSecondsRealtime(1f);
            }

            WaveAnnounceUI.ShowWaveProgressGlobal(currentWaveIndex, currentWaveKilled, currentWaveTotal);

            yield return StartCoroutine(CoSpawnWave(wave));
            yield return new WaitUntil(() => aliveEnemies.Count == 0);

            OnWaveCleared?.Invoke(currentWaveIndex);
            WaveAnnounceUI.ShowWaveClearGlobal(currentWaveIndex);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, waveClearHoldTime));
        }

        isRunning = false;
        OnAllWavesCleared?.Invoke();
        WaveAnnounceUI.ShowGameClearGlobal();
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
        health.OnDead -= HandleEnemyDead;
        health.OnDead += HandleEnemyDead;
        aliveEnemies.Add(health);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count == 0)
            return transform.position;

        int index = UnityEngine.Random.Range(0, spawnPoints.Count);
        return spawnPoints[index] != null ? spawnPoints[index].Position : transform.position;
    }

    private void HandleEnemyDead(Health enemy)
    {
        enemy.OnDead -= HandleEnemyDead;
        aliveEnemies.Remove(enemy);

        currentWaveKilled = Mathf.Clamp(currentWaveKilled + 1, 0, currentWaveTotal);
        WaveAnnounceUI.ShowWaveProgressGlobal(currentWaveIndex, currentWaveKilled, currentWaveTotal);
    }

    private void OnDisable()
    {
        StopAndClearWaves(false);
    }

    // 씬 재시작/타이틀 이동처럼 전투 흐름을 강제로 끊어야 할 때 호출한다.
    public void StopAndClearWaves(bool despawnAliveEnemies = true)
    {
        StopAllCoroutines();
        isRunning = false;
        currentWaveIndex = -1;

        if (despawnAliveEnemies)
            DespawnAliveEnemies();

        ClearAliveEnemySubscriptions();
    }

    private void DespawnAliveEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            Health enemy = aliveEnemies[i];
            if (enemy == null)
                continue;

            GameObject enemyObject = enemy.gameObject;

            if (ObjectPool.Instance != null && enemyObject.GetComponent<PooledObjectTag>() != null)
                ObjectPool.Instance.Release(enemyObject);
            else
                enemyObject.SetActive(false);
        }
    }

    private void ClearAliveEnemySubscriptions()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null)
                aliveEnemies[i].OnDead -= HandleEnemyDead;
        }

        aliveEnemies.Clear();
    }

    private int CountWaveEnemies(WaveData wave)
    {
        int total = 0;

        if (wave == null)
            return total;

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            WaveData.SpawnEntry entry = wave.enemies[i];

            if (entry == null || entry.enemyPrefab == null)
                continue;

            total += Mathf.Max(0, entry.count);
        }

        return total;
    }

    // 첫 웨이브는 플레이어가 조작과 UI를 확인할 시간을 주기 위해 더 길게 대기한다.
    private int GetCountdownStart()
    {
        return currentWaveIndex == 0 ? firstWaveCountdownStart : countdownStart;
    }

    // 새 웨이브가 시작될 때 씬에 배치된 박스류 BreakableObject만 다시 켠다.
    private void RespawnBreakableObjects()
    {
        BreakableObject[] breakables = FindObjectsByType<BreakableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < breakables.Length; i++)
            breakables[i]?.Respawn();
    }
}