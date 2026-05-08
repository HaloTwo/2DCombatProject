using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrototypeSceneBuilder
{
    private const string MarkerPath = "Assets/9.SO/.prototype_generated";
    private const string StartScenePath = "Assets/1.Scene/StartScene.unity";
    private const string LoadingScenePath = "Assets/1.Scene/LoadingScene.unity";
    private const string GameScenePath = "Assets/1.Scene/GameScene.unity";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(MarkerPath)) return;
            BuildPrototype();
            File.WriteAllText(MarkerPath, "generated");
            AssetDatabase.ImportAsset(MarkerPath);
        };
    }

    [MenuItem("2DCombatProject/Build Temporary Prototype")]
    public static void BuildPrototype()
    {
        EnsureFolders();

        AttackData basicAttack = CreateAttackData("BasicAttack", 10f, new Vector2(7f, 2f), 0.055f, 0.11f, 0.22f);
        AttackData dashAttack = CreateAttackData("DashAttack", 14f, new Vector2(10f, 2f), 0.065f, 0.16f, 0.4f);
        AttackData areaAttack = CreateAttackData("AreaAttack", 18f, new Vector2(5f, 4f), 0.08f, 0.2f, 0.8f);
        AttackData enemyMelee = CreateAttackData("EnemyMeleeAttack", 8f, new Vector2(5f, 2f), 0.04f, 0.12f, 0.65f);
        AttackData enemyProjectile = CreateAttackData("EnemyProjectileAttack", 7f, new Vector2(4f, 2f), 0.035f, 0.12f, 0.8f);

        GameObject playerProjectile = CreateProjectilePrefab("PlayerProjectile", Color.cyan);
        GameObject enemyProjectilePrefab = CreateProjectilePrefab("EnemyProjectile", Color.magenta);

        SkillData skillOne = CreateSkillData("Skill_DashAttack", SkillType.DashAttack, dashAttack, 1.2f, 0.14f, 2.5f, 20f, null);
        SkillData skillTwo = CreateSkillData("Skill_AreaAttack", SkillType.AreaAttack, areaAttack, 2.0f, 0.18f, 2.2f, 0f, null);

        GameObject playerPrefab = CreatePlayerPrefab(basicAttack, skillOne, skillTwo);
        GameObject meleeEnemyPrefab = CreateMeleeEnemyPrefab(enemyMelee);
        GameObject rangedEnemyPrefab = CreateRangedEnemyPrefab(enemyProjectile, enemyProjectilePrefab);

        WaveData[] waves = CreateWaveData(meleeEnemyPrefab, rangedEnemyPrefab);

        CreateStartScene();
        CreateLoadingScene();
        CreateGameScene(playerPrefab, meleeEnemyPrefab, rangedEnemyPrefab, playerProjectile, enemyProjectilePrefab, waves);
        SetBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PrototypeSceneBuilder] Temporary prototype generated.");
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/1.Scene",
            "Assets/4.Sprite",
            "Assets/7.Prefab",
            "Assets/7.Prefab/Player",
            "Assets/7.Prefab/Enemy",
            "Assets/7.Prefab/Projectile",
            "Assets/9.SO",
            "Assets/9.SO/Attack",
            "Assets/9.SO/Skill",
            "Assets/9.SO/Wave"
        };

        for (int i = 0; i < folders.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder(folders[i]))
                Directory.CreateDirectory(folders[i]);
        }
    }

    private static AttackData CreateAttackData(string name, float damage, Vector2 knockback, float hitStop, float activeTime, float cooldown)
    {
        string path = $"Assets/9.SO/Attack/{name}.asset";
        AttackData data = AssetDatabase.LoadAssetAtPath<AttackData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<AttackData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.damage = damage;
        data.knockback = knockback;
        data.hitStopTime = hitStop;
        data.activeTime = activeTime;
        data.cooldown = cooldown;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static SkillData CreateSkillData(string name, SkillType type, AttackData attackData, float cooldown, float duration, float range, float force, GameObject projectilePrefab)
    {
        string path = $"Assets/9.SO/Skill/{name}.asset";
        SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SkillData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.skillType = type;
        data.attackData = attackData;
        data.cooldown = cooldown;
        data.duration = duration;
        data.range = range;
        data.force = force;
        data.projectilePrefab = projectilePrefab;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static WaveData[] CreateWaveData(GameObject meleeEnemyPrefab, GameObject rangedEnemyPrefab)
    {
        WaveData[] waves = new WaveData[5];

        for (int i = 0; i < waves.Length; i++)
        {
            string path = $"Assets/9.SO/Wave/Wave_{i + 1}.asset";
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(wave, path);
            }

            wave.enemies.Clear();
            wave.enemies.Add(new WaveData.SpawnEntry { enemyPrefab = meleeEnemyPrefab, count = 1 + i, interval = 0.25f });

            if (i >= 1)
                wave.enemies.Add(new WaveData.SpawnEntry { enemyPrefab = rangedEnemyPrefab, count = i, interval = 0.35f });

            wave.nextWaveDelay = 1.2f;
            EditorUtility.SetDirty(wave);
            waves[i] = wave;
        }

        return waves;
    }

    private static GameObject CreatePlayerPrefab(AttackData basicAttack, SkillData skillOne, SkillData skillTwo)
    {
        GameObject root = new GameObject("Player");
        root.tag = "Player";
        root.layer = LayerMask.NameToLayer("Default");
        root.transform.position = Vector3.zero;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite("PlayerSprite", new Color(0.2f, 0.65f, 1f));
        renderer.sortingOrder = 10;

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3.2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D body = root.AddComponent<BoxCollider2D>();
        body.size = new Vector2(0.75f, 1.2f);

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.Player;
        healthSo.FindProperty("maxHp").floatValue = 100f;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Hurtbox>();
        root.AddComponent<PlayerInputReader>();
        PlayerMovement2D movement = root.AddComponent<PlayerMovement2D>();
        root.AddComponent<ListRendererCache>();
        root.AddComponent<CombatFeedback>();

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.68f, 0f);

        GameObject basicHitbox = CreateHitboxChild(root.transform, "BasicAttackHitbox", new Vector2(1.0f, 0.65f), new Vector3(0.75f, 0f, 0f));
        Hitbox basicHitboxComp = basicHitbox.GetComponent<Hitbox>();

        GameObject skillHitbox = CreateHitboxChild(root.transform, "SkillHitbox", new Vector2(2.2f, 1.1f), new Vector3(1.0f, 0f, 0f));
        Hitbox skillHitboxComp = skillHitbox.GetComponent<Hitbox>();

        PlayerCombat combat = root.AddComponent<PlayerCombat>();
        SerializedObject combatSo = new SerializedObject(combat);
        combatSo.FindProperty("basicAttackHitbox").objectReferenceValue = basicHitboxComp;
        combatSo.FindProperty("basicAttack").objectReferenceValue = basicAttack;
        combatSo.ApplyModifiedPropertiesWithoutUndo();

        PlayerSkillController skills = root.AddComponent<PlayerSkillController>();
        SerializedObject skillSo = new SerializedObject(skills);
        skillSo.FindProperty("movement").objectReferenceValue = movement;
        skillSo.FindProperty("rb").objectReferenceValue = rb;
        skillSo.FindProperty("skillHitbox").objectReferenceValue = skillHitboxComp;
        skillSo.FindProperty("skillOne").objectReferenceValue = skillOne;
        skillSo.FindProperty("skillTwo").objectReferenceValue = skillTwo;
        skillSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject moveSo = new SerializedObject(movement);
        moveSo.FindProperty("rb").objectReferenceValue = rb;
        moveSo.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        moveSo.FindProperty("groundMask").intValue = LayerMask.GetMask("Default");
        moveSo.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/7.Prefab/Player/Player.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateMeleeEnemyPrefab(AttackData attackData)
    {
        GameObject root = CreateEnemyRoot("MeleeEnemy", new Color(1f, 0.35f, 0.2f));

        GameObject hitbox = CreateHitboxChild(root.transform, "EnemyAttackHitbox", new Vector2(0.9f, 0.65f), new Vector3(0.7f, 0f, 0f));
        MeleeChargerEnemy enemy = root.AddComponent<MeleeChargerEnemy>();
        SerializedObject enemySo = new SerializedObject(enemy);
        enemySo.FindProperty("attackHitbox").objectReferenceValue = hitbox.GetComponent<Hitbox>();
        enemySo.FindProperty("attackData").objectReferenceValue = attackData;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/7.Prefab/Enemy/MeleeEnemy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateRangedEnemyPrefab(AttackData projectileAttackData, GameObject projectilePrefab)
    {
        GameObject root = CreateEnemyRoot("RangedEnemy", new Color(0.85f, 0.25f, 1f));

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(root.transform);
        firePoint.transform.localPosition = new Vector3(0.65f, 0.15f, 0f);

        RangedShooterEnemy enemy = root.AddComponent<RangedShooterEnemy>();
        SerializedObject enemySo = new SerializedObject(enemy);
        enemySo.FindProperty("projectileAttackData").objectReferenceValue = projectileAttackData;
        enemySo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        enemySo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/7.Prefab/Enemy/RangedEnemy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateEnemyRoot(string name, Color color)
    {
        GameObject root = new GameObject(name);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite($"{name}Sprite", color);
        renderer.sortingOrder = 9;

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3.2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 1.1f);

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
        healthSo.FindProperty("maxHp").floatValue = 30f;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Hurtbox>();
        root.AddComponent<ListRendererCache>();
        root.AddComponent<CombatFeedback>();
        return root;
    }

    private static GameObject CreateProjectilePrefab(string name, Color color)
    {
        GameObject root = new GameObject(name);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite($"{name}Sprite", color);
        renderer.sortingOrder = 12;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.15f;

        root.AddComponent<Projectile>();

        string path = $"Assets/7.Prefab/Projectile/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateHitboxChild(Transform parent, string name, Vector2 size, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        go.AddComponent<Hitbox>();
        return go;
    }

    private static Sprite CreateSprite(string name, Color color)
    {
        string path = $"Assets/4.Sprite/{name}.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreateStartScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera");
        Camera cam = camera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject menu = new GameObject("StartMenuController");
        StartMenuController controller = menu.AddComponent<StartMenuController>();
        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("loadingSceneName").stringValue = "LoadingScene";
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject canvas = CreateCanvas();
        CreateText(canvas.transform, "TitleText", "2D Combat Prototype", new Vector2(0f, 135f), 48, TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "GuideText", "Enter / Space / Gamepad A or click Start", new Vector2(0f, 55f), 22, TextAnchor.MiddleCenter);
        CreateStartButton(canvas.transform, controller);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, StartScenePath);
    }

    private static void CreateLoadingScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera");
        Camera cam = camera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject loader = new GameObject("LoadingSceneController");
        loader.AddComponent<LoadingSceneController>();

        GameObject canvas = CreateCanvas();
        CreateText(canvas.transform, "LoadingText", "Loading Combat Scene...", Vector2.zero, 38, TextAnchor.MiddleCenter);

        EditorSceneManager.SaveScene(scene, LoadingScenePath);
    }

    private static void CreateGameScene(GameObject playerPrefab, GameObject meleeEnemyPrefab, GameObject rangedEnemyPrefab, GameObject playerProjectile, GameObject enemyProjectile, WaveData[] waves)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camera = new GameObject("Main Camera");
        Camera cam = camera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 1.2f, -10f);
        camera.AddComponent<CameraShake>();

        GameObject managers = new GameObject("@Managers");
        managers.AddComponent<GameManager>().StartGame();
        ObjectPool pool = managers.AddComponent<ObjectPool>();
        SerializedObject poolSo = new SerializedObject(pool);
        SerializedProperty entries = poolSo.FindProperty("initialPools");
        entries.arraySize = 3;
        SetPoolEntry(entries.GetArrayElementAtIndex(0), meleeEnemyPrefab, 6);
        SetPoolEntry(entries.GetArrayElementAtIndex(1), rangedEnemyPrefab, 4);
        SetPoolEntry(entries.GetArrayElementAtIndex(2), enemyProjectile, 10);
        poolSo.ApplyModifiedPropertiesWithoutUndo();

        CreateGround("Ground", new Vector3(0f, -1.25f, 0f), new Vector2(16f, 0.6f), new Color(0.25f, 0.25f, 0.25f));
        CreateGround("LeftWall", new Vector3(-8.2f, 1.5f, 0f), new Vector2(0.4f, 5f), new Color(0.18f, 0.18f, 0.18f));
        CreateGround("RightWall", new Vector3(8.2f, 1.5f, 0f), new Vector2(0.4f, 5f), new Color(0.18f, 0.18f, 0.18f));

        PrefabUtility.InstantiatePrefab(playerPrefab);
        GameObject player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(-4f, 0f, 0f);

        GameObject waveObject = new GameObject("WaveManager");
        WaveManager waveManager = waveObject.AddComponent<WaveManager>();
        SerializedObject waveSo = new SerializedObject(waveManager);
        SerializedProperty waveList = waveSo.FindProperty("waves");
        waveList.arraySize = waves.Length;
        for (int i = 0; i < waves.Length; i++)
            waveList.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];

        SerializedProperty spawnList = waveSo.FindProperty("spawnPoints");
        spawnList.arraySize = 3;
        spawnList.GetArrayElementAtIndex(0).objectReferenceValue = CreateSpawnPoint("SpawnPoint_L", new Vector3(-5.5f, -0.5f, 0f));
        spawnList.GetArrayElementAtIndex(1).objectReferenceValue = CreateSpawnPoint("SpawnPoint_R", new Vector3(5.5f, -0.5f, 0f));
        spawnList.GetArrayElementAtIndex(2).objectReferenceValue = CreateSpawnPoint("SpawnPoint_C", new Vector3(2.0f, -0.5f, 0f));
        waveSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject canvas = CreateCanvas();
        CreateText(canvas.transform, "GuideText", "Move: Arrows / Stick   Jump: Space / A   Dash: Z / B   Attack: C / X   Skill: A,S / LB,RB", new Vector2(0f, 205f), 18, TextAnchor.MiddleCenter);
        if (player != null)
            CreateSkillSlotBar(canvas.transform, player.GetComponent<PlayerSkillController>());

        GameObject clearPanel = CreatePanel(canvas.transform, "ClearPanel", "CLEAR", new Color(0f, 0.45f, 0.25f, 0.85f));
        GameObject gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel", "GAME OVER", new Color(0.45f, 0f, 0f, 0.85f));

        ResultView resultView = canvas.AddComponent<ResultView>();
        SerializedObject resultSo = new SerializedObject(resultView);
        resultSo.FindProperty("clearPanel").objectReferenceValue = clearPanel;
        resultSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        resultSo.ApplyModifiedPropertiesWithoutUndo();
        clearPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    private static void SetPoolEntry(SerializedProperty entry, GameObject prefab, int prewarm)
    {
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("prewarmCount").intValue = prewarm;
    }

    private static EnemySpawnPoint CreateSpawnPoint(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        return go.AddComponent<EnemySpawnPoint>();
    }

    private static void CreateGround(string name, Vector3 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite($"{name}Sprite", color);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateStartButton(Transform parent, StartMenuController controller)
    {
        GameObject buttonObject = new GameObject("StartButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240f, 64f);
        rect.anchoredPosition = new Vector2(0f, -55f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.4f, 0.75f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(controller.StartGame);

        CreateText(buttonObject.transform, "StartButtonText", "START", Vector2.zero, 28, TextAnchor.MiddleCenter);
    }

    private static Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 100f);
        rect.anchoredPosition = anchoredPosition;

        Text uiText = go.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.alignment = anchor;
        uiText.color = Color.white;
        return uiText;
    }

    private static void CreateSkillSlotBar(Transform parent, PlayerSkillController controller)
    {
        GameObject root = new GameObject("SkillSlotBar");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(260f, 86f);
        rect.anchoredPosition = new Vector2(0f, 24f);

        SkillSlotBarUI bar = root.AddComponent<SkillSlotBarUI>();
        SkillSlotUI slotOne = CreateSkillSlot(root.transform, "SkillSlot_A", new Vector2(-70f, 0f));
        SkillSlotUI slotTwo = CreateSkillSlot(root.transform, "SkillSlot_S", new Vector2(70f, 0f));

        SerializedObject barSo = new SerializedObject(bar);
        barSo.FindProperty("skillController").objectReferenceValue = controller;
        barSo.FindProperty("slotOne").objectReferenceValue = slotOne;
        barSo.FindProperty("slotTwo").objectReferenceValue = slotTwo;
        barSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SkillSlotUI CreateSkillSlot(Transform parent, string name, Vector2 anchoredPosition)
    {
        GameObject slot = new GameObject(name);
        slot.transform.SetParent(parent, false);

        RectTransform rect = slot.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(112f, 72f);
        rect.anchoredPosition = anchoredPosition;

        Image background = slot.AddComponent<Image>();
        background.color = new Color(0.1f, 0.35f, 0.75f, 0.9f);
        slot.AddComponent<CanvasGroup>();

        Text keyText = CreateText(slot.transform, "KeyText", "A", new Vector2(-35f, 18f), 20, TextAnchor.MiddleCenter);
        keyText.rectTransform.sizeDelta = new Vector2(36f, 32f);

        Text skillText = CreateText(slot.transform, "SkillText", "Skill", new Vector2(18f, -8f), 19, TextAnchor.MiddleCenter);
        skillText.rectTransform.sizeDelta = new Vector2(76f, 44f);

        SkillSlotUI ui = slot.AddComponent<SkillSlotUI>();
        SerializedObject uiSo = new SerializedObject(ui);
        uiSo.FindProperty("keyText").objectReferenceValue = keyText;
        uiSo.FindProperty("skillText").objectReferenceValue = skillText;
        uiSo.FindProperty("background").objectReferenceValue = background;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        return ui;
    }

    private static GameObject CreatePanel(Transform parent, string name, string label, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 160f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        CreateText(panel.transform, $"{label}Text", label, Vector2.zero, 44, TextAnchor.MiddleCenter);
        return panel;
    }

    private static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(StartScenePath, true),
            new EditorBuildSettingsScene(LoadingScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }
}
