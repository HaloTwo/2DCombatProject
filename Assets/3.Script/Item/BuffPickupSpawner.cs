using System.Collections;
using UnityEngine;

public class BuffPickupSpawner : MonoBehaviour
{
    [SerializeField, KoreanLabel("버프 프리팹 목록")] private GameObject[] buffPrefabs;
    [SerializeField, KoreanLabel("스폰 위치 목록")] private Transform[] spawnPoints;
    [SerializeField, KoreanLabel("최소 스폰 간격")] private float minSpawnInterval = 7f;
    [SerializeField, KoreanLabel("최대 스폰 간격")] private float maxSpawnInterval = 13f;
    [SerializeField, KoreanLabel("동시에 유지할 최대 개수")] private int maxAliveCount = 2;

    private int aliveCount;

    private void OnEnable()
    {
        StartCoroutine(CoSpawnLoop());
    }

    // 일정 간격으로 맵 곳곳에 버프 아이템을 뿌린다. 프리팹/위치는 씬에서 직접 구성한다.
    private IEnumerator CoSpawnLoop()
    {
        while (enabled)
        {
            float delay = Random.Range(minSpawnInterval, Mathf.Max(minSpawnInterval, maxSpawnInterval));
            yield return new WaitForSeconds(delay);

            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        if (aliveCount >= maxAliveCount || buffPrefabs == null || buffPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        GameObject prefab = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (prefab == null || point == null)
            return;

        GameObject go = Instantiate(prefab, point.position, Quaternion.identity);
        aliveCount++;
        StartCoroutine(CoWatchPickup(go));
    }

    private IEnumerator CoWatchPickup(GameObject pickup)
    {
        while (pickup != null && pickup.activeInHierarchy)
            yield return null;

        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}
