using System.IO;
using UnityEditor;
using UnityEngine;

public static class BossContentBuilder
{
    private const string AppliedMarkerPath = "Assets/9.SO/.boss_content_applied";
    private const string NormalOriginPath = "Assets/7.Prefab/Enemy/EnemyOrigin/NormalEnemy.prefab";
    private const string BossOriginPath = "Assets/7.Prefab/Enemy/EnemyOrigin/BossEnemy.prefab";
    private const string SkeletonPath = "Assets/2.Model/Monster/SkeletonEnemy.prefab";
    private const string BossSkeletonPath = "Assets/2.Model/Monster/BossSkeletonEnemy.prefab";
    private const string BossAttackPath = "Assets/9.SO/Attack/EliteBossAttack.asset";
    private const string FinalWavePath = "Assets/9.SO/Wave/Wave_5.asset";

    [InitializeOnLoadMethod]
    private static void ApplyOnceAfterCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(AppliedMarkerPath))
                return;

            Apply();
            File.WriteAllText(AppliedMarkerPath, "Elite boss content applied.");
            AssetDatabase.ImportAsset(AppliedMarkerPath);
        };
    }

    [MenuItem("Tools/2DCombat/Create Elite Boss Content")]
    public static void Apply()
    {
        CreateBossOrigin();
        GameObject bossPrefab = CreateBossSkeleton();
        ApplyFinalWave(bossPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BossContentBuilder] Elite boss content applied.");
    }

    // 기존 근접 적 원본 구조를 재사용해서 보스 원본을 만든다.
    // Health/Hitbox/Animator/AI 흐름은 그대로 두고 보스용 수치와 2페이즈 연출만 얹는다.
    private static void CreateBossOrigin()
    {
        GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NormalOriginPath);
        AttackData bossAttack = AssetDatabase.LoadAssetAtPath<AttackData>(BossAttackPath);
        if (normalPrefab == null || bossAttack == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(normalPrefab);
        instance.name = "BossEnemy";
        instance.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        Health health = instance.GetComponent<Health>();
        SetSerialized(health, "maxHp", 280f);
        SetSerialized(health, "invincibleTime", 0.08f);

        MeleeChargerEnemy oldBrain = instance.GetComponent<MeleeChargerEnemy>();
        if (oldBrain != null)
            Object.DestroyImmediate(oldBrain, true);

        EliteMeleeBossEnemy bossBrain = instance.AddComponent<EliteMeleeBossEnemy>();
        Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
        Animator animator = instance.GetComponentInChildren<Animator>(true);
        Hitbox attackHitbox = instance.GetComponentInChildren<Hitbox>(true);

        SerializedObject brainSo = new SerializedObject(bossBrain);
        SetProperty(brainSo, "rb", rb);
        SetProperty(brainSo, "health", health);
        SetProperty(brainSo, "animator", animator);
        SetProperty(brainSo, "detectRange", 7f);
        SetProperty(brainSo, "attackRange", 0.75f);
        SetProperty(brainSo, "moveSpeed", 1.35f);
        SetProperty(brainSo, "patrolTurnCooldown", 1.2f);
        SetProperty(brainSo, "attackHitbox", attackHitbox);
        SetProperty(brainSo, "attackData", bossAttack);
        SetProperty(brainSo, "attackCooldown", 0.9f);
        SetProperty(brainSo, "attackStateDuration", 0.65f);
        SetProperty(brainSo, "phaseTwoHpRatio", 0.5f);
        SetProperty(brainSo, "phaseTwoSpeedMultiplier", 1.3f);
        brainSo.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(instance, BossOriginPath);
    }

    // 실제 게임에서 스폰되는 보스는 Skeleton 비주얼을 쓰고, 원본은 BossEnemy를 따른다.
    private static GameObject CreateBossSkeleton()
    {
        GameObject bossOrigin = AssetDatabase.LoadAssetAtPath<GameObject>(BossOriginPath);
        GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        if (bossOrigin == null || skeletonPrefab == null)
            return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(bossOrigin);
        instance.name = "BossSkeletonEnemy";
        instance.transform.localScale = new Vector3(1.45f, 1.45f, 1f);

        Health health = instance.GetComponent<Health>();
        SetSerialized(health, "maxHp", 340f);

        SpriteRenderer bossRenderer = instance.GetComponentInChildren<SpriteRenderer>(true);
        SpriteRenderer skeletonRenderer = skeletonPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (bossRenderer != null && skeletonRenderer != null)
            bossRenderer.sprite = skeletonRenderer.sprite;

        Animator bossAnimator = instance.GetComponentInChildren<Animator>(true);
        Animator skeletonAnimator = skeletonPrefab.GetComponentInChildren<Animator>(true);
        if (bossAnimator != null && skeletonAnimator != null)
            bossAnimator.runtimeAnimatorController = skeletonAnimator.runtimeAnimatorController;

        BoxCollider2D attackCollider = FindChild<BoxCollider2D>(instance, "AttackHitbox");
        if (attackCollider != null)
        {
            attackCollider.offset = new Vector2(0.12f, 0.02f);
            attackCollider.size = new Vector2(1.1f, 0.75f);
        }

        return SavePrefab(instance, BossSkeletonPath);
    }

    // 마지막 웨이브는 잡몹 수를 줄이고 보스 1마리를 섞어 최종전처럼 보이게 만든다.
    private static void ApplyFinalWave(GameObject bossPrefab)
    {
        WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(FinalWavePath);
        GameObject goblin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2.Model/Monster/GoblinEnemy.prefab");
        GameObject flying = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2.Model/Monster/FlyingEyeEnemy.prefab");
        if (wave == null || bossPrefab == null)
            return;

        wave.enemies.Clear();
        AddWaveEntry(wave, goblin, 2, 0.35f);
        AddWaveEntry(wave, flying, 2, 0.45f);
        AddWaveEntry(wave, bossPrefab, 1, 0.25f);
        wave.nextWaveDelay = 1.2f;
        wave.isBossWave = true;
        wave.bossName = "ELITE SKELETON";
        EditorUtility.SetDirty(wave);
    }

    private static void AddWaveEntry(WaveData wave, GameObject prefab, int count, float interval)
    {
        if (wave == null || prefab == null)
            return;

        wave.enemies.Add(new WaveData.SpawnEntry
        {
            enemyPrefab = prefab,
            count = count,
            interval = interval
        });
    }

    private static T FindChild<T>(GameObject root, string name) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == name)
                return components[i];
        }

        return null;
    }

    private static GameObject SavePrefab(GameObject instance, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void SetSerialized(Object target, string propertyName, float value)
    {
        if (target == null)
            return;

        SerializedObject so = new SerializedObject(target);
        SetProperty(so, propertyName, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetProperty(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }
}
