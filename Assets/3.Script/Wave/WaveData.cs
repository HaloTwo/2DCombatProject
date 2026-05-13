using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "2D Combat/Wave Data")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject enemyPrefab;
        public int count = 1;
        public float interval = 0.4f;
    }

    public List<SpawnEntry> enemies = new();
    public float nextWaveDelay = 2f;
    [Header("Boss")]
    [KoreanLabel("보스 웨이브")]
    public bool isBossWave;
    [KoreanLabel("보스 이름")]
    public string bossName = "BOSS";
}
