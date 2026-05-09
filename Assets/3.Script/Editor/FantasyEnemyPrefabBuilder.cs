using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class FantasyEnemyPrefabBuilder
{
    private const string SpriteRoot = "Assets/Monsters Creatures Fantasy/Sprites";
    private const string GeneratedRoot = "Assets/7.Prefab/Enemy/Fantasy";
    private const string ControllerRoot = "Assets/5.Animation/FantasyEnemies";

    [MenuItem("2DCombatProject/Build Fantasy Enemies")]
    public static void BuildFantasyEnemies()
    {
        EnsureFolder(GeneratedRoot);
        EnsureFolder(ControllerRoot);

        AttackData flyingAttack = CreateAttackData("FlyingEyeAttack", 9f, new Vector2(4f, 2f), 0.04f, 0.12f, 0.9f);
        AttackData goblinAttack = CreateAttackData("GoblinAttack", 10f, new Vector2(5f, 2.5f), 0.045f, 0.13f, 1f);
        AttackData skeletonAttack = CreateAttackData("SkeletonAttack", 16f, new Vector2(7f, 3f), 0.07f, 0.17f, 1.35f);
        AttackData mushroomProjectileAttack = CreateAttackData("MushroomProjectileAttack", 8f, new Vector2(4f, 1.5f), 0.035f, 0.12f, 1.25f);

        GameObject mushroomProjectile = CreateProjectilePrefab("MushroomSporeProjectile", new Color(0.65f, 1f, 0.45f, 1f), 7f, 2.2f);

        CreateFlyingEye(flyingAttack);
        CreateMeleeEnemy("GoblinEnemy", "Goblin", goblinAttack, 38f, 1.55f, 1.05f);
        CreateRangedMushroom(mushroomProjectileAttack, mushroomProjectile);
        CreateMeleeEnemy("SkeletonEnemy", "Skeleton", skeletonAttack, 58f, 1.05f, 1.2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FantasyEnemyPrefabBuilder] Fantasy enemy prefabs generated.");
    }

    private static void CreateFlyingEye(AttackData attackData)
    {
        if (PrefabExists("FlyingEyeEnemy"))
        {
            Debug.Log("[FantasyEnemyPrefabBuilder] FlyingEyeEnemy already exists. Skipped to preserve inspector values.");
            return;
        }

        GameObject root = CreateBaseEnemy("FlyingEyeEnemy", "Flying eye", 24f, 3.6f, 1.05f, 0f);
        Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        root.GetComponent<BoxCollider2D>().isTrigger = true;

        GameObject hitbox = CreateHitboxChild(root.transform, "AttackHitbox", new Vector2(0.9f, 0.7f), new Vector3(0.58f, 0f, 0f));
        FlyingMeleeEnemy brain = root.AddComponent<FlyingMeleeEnemy>();
        ApplyMeleeBrain(brain, rb, root.GetComponent<Health>(), root.GetComponentInChildren<Animator>(), hitbox.GetComponent<Hitbox>(), attackData, 0.9f, 2.25f, 0.48f, 9f);
        SavePrefab(root, "FlyingEyeEnemy");
    }

    private static void CreateMeleeEnemy(string prefabName, string spriteFolder, AttackData attackData, float hp, float speed, float scale)
    {
        if (PrefabExists(prefabName))
        {
            Debug.Log($"[FantasyEnemyPrefabBuilder] {prefabName} already exists. Skipped to preserve inspector values.");
            return;
        }

        GameObject root = CreateBaseEnemy(prefabName, spriteFolder, hp, speed, scale, 3.2f);
        GameObject hitbox = CreateHitboxChild(root.transform, "AttackHitbox", new Vector2(1.05f, 0.8f), new Vector3(0.62f, 0.04f, 0f));

        MeleeChargerEnemy brain = root.AddComponent<MeleeChargerEnemy>();
        ApplyMeleeBrain(brain, root.GetComponent<Rigidbody2D>(), root.GetComponent<Health>(), root.GetComponentInChildren<Animator>(), hitbox.GetComponent<Hitbox>(), attackData, attackData.cooldown, speed, 0.5f, 8f);
        SavePrefab(root, prefabName);
    }

    private static void CreateRangedMushroom(AttackData projectileAttackData, GameObject projectilePrefab)
    {
        if (PrefabExists("MushroomEnemy"))
        {
            Debug.Log("[FantasyEnemyPrefabBuilder] MushroomEnemy already exists. Skipped to preserve inspector values.");
            return;
        }

        GameObject root = CreateBaseEnemy("MushroomEnemy", "Mushroom", 32f, 1.9f, 1.1f, 3.2f);
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(root.transform, false);
        firePoint.transform.localPosition = new Vector3(0.55f, 0.2f, 0f);

        RangedShooterEnemy brain = root.AddComponent<RangedShooterEnemy>();
        SerializedObject so = new SerializedObject(brain);
        so.FindProperty("rb").objectReferenceValue = root.GetComponent<Rigidbody2D>();
        so.FindProperty("health").objectReferenceValue = root.GetComponent<Health>();
        so.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
        so.FindProperty("moveSpeed").floatValue = 1.15f;
        so.FindProperty("detectRange").floatValue = 8f;
        so.FindProperty("projectileAttackData").objectReferenceValue = projectileAttackData;
        so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        so.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        so.FindProperty("keepDistance").floatValue = 4.5f;
        so.FindProperty("fireCooldown").floatValue = 1.35f;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "MushroomEnemy");
    }

    private static GameObject CreateBaseEnemy(string objectName, string spriteFolder, float hp, float speed, float scale, float gravity)
    {
        GameObject root = new GameObject(objectName);
        root.transform.localScale = new Vector3(scale, scale, 1f);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = gravity;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.72f, 0.9f);
        collider.offset = new Vector2(0f, -0.05f);

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
        healthSo.FindProperty("maxHp").floatValue = hp;
        healthSo.FindProperty("invincibleTime").floatValue = 0.1f;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Hurtbox>();
        EnemyContactDamage contactDamage = root.AddComponent<EnemyContactDamage>();
        SerializedObject contactSo = new SerializedObject(contactDamage);
        contactSo.FindProperty("damage").floatValue = spriteFolder == "Skeleton" ? 8f : 5f;
        contactSo.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<ListRendererCache>();

        CombatFeedback feedback = root.AddComponent<CombatFeedback>();
        SerializedObject feedbackSo = new SerializedObject(feedback);
        feedbackSo.FindProperty("spawnDamageText").boolValue = true;
        feedbackSo.FindProperty("damageTextColor").colorValue = new Color(1f, 0.92f, 0.35f, 1f);
        feedbackSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadFirstSprite(spriteFolder, GetIdleSheet(spriteFolder));
        renderer.sortingOrder = 9;

        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateController(spriteFolder);
        visual.AddComponent<EnemyAnimationEventRelay>();

        CreateHealthBar(root.transform, health);
        return root;
    }

    private static void CreateHealthBar(Transform parent, Health health)
    {
        Sprite black = CreateSolidSprite("EnemyHealthBarBack", new Color(0.03f, 0.03f, 0.04f, 1f));
        Sprite red = CreateSolidSprite("EnemyHealthBarFill", new Color(0.95f, 0.12f, 0.12f, 1f));

        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.78f, 0f);

        Vector3 barScale = new Vector3(0.7f, 0.08f, 1f);
        SpriteRenderer back = CreateBarRenderer(root.transform, "Back", black, Vector3.zero, barScale, 30);

        GameObject fillAnchor = new GameObject("FillAnchor");
        fillAnchor.transform.SetParent(root.transform, false);
        fillAnchor.transform.localPosition = Vector3.zero;

        SpriteRenderer fill = CreateBarRenderer(fillAnchor.transform, "Fill", red, Vector3.zero, barScale, 31);

        EnemyWorldHealthBar healthBar = root.AddComponent<EnemyWorldHealthBar>();
        SerializedObject so = new SerializedObject(healthBar);
        so.FindProperty("health").objectReferenceValue = health;
        so.FindProperty("fillRoot").objectReferenceValue = fillAnchor.transform;
        so.FindProperty("barRoot").objectReferenceValue = root;
        so.ApplyModifiedPropertiesWithoutUndo();

        back.enabled = true;
        fill.enabled = true;
    }

    private static SpriteRenderer CreateBarRenderer(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void ApplyMeleeBrain(Object brain, Rigidbody2D rb, Health health, Animator animator, Hitbox hitbox, AttackData attackData, float cooldown, float speed, float attackRange, float detectRange)
    {
        SerializedObject so = new SerializedObject(brain);
        so.FindProperty("rb").objectReferenceValue = rb;
        so.FindProperty("health").objectReferenceValue = health;
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("moveSpeed").floatValue = speed;
        so.FindProperty("attackRange").floatValue = attackRange;
        so.FindProperty("detectRange").floatValue = detectRange;
        so.FindProperty("attackHitbox").objectReferenceValue = hitbox;
        so.FindProperty("attackData").objectReferenceValue = attackData;
        so.FindProperty("attackCooldown").floatValue = cooldown;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateHitboxChild(Transform parent, string name, Vector2 size, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.enabled = false;
        collider.size = size;
        go.AddComponent<Hitbox>();
        return go;
    }

    private static RuntimeAnimatorController CreateController(string spriteFolder)
    {
        string path = $"{ControllerRoot}/{spriteFolder}_Enemy.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;

        controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters[0]);

        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine sm = layer.stateMachine;
        foreach (ChildAnimatorState state in sm.states)
            sm.RemoveState(state.state);
        foreach (AnimatorStateTransition transition in sm.anyStateTransitions)
            sm.RemoveAnyStateTransition(transition);

        AnimatorState idle = sm.AddState("Idle", new Vector3(180f, 0f, 0f));
        AnimatorState move = sm.AddState("Move", new Vector3(410f, 0f, 0f));
        AnimatorState attack = sm.AddState("Attack", new Vector3(300f, 160f, 0f));
        AnimatorState hurt = sm.AddState("Hurt", new Vector3(560f, 160f, 0f));
        idle.motion = CreateClip(spriteFolder, GetIdleSheet(spriteFolder), true, 8f);
        move.motion = CreateClip(spriteFolder, GetMoveSheet(spriteFolder), true, 10f);
        attack.motion = CreateClip(spriteFolder, "Attack1", false, 12f);
        hurt.motion = CreateClip(spriteFolder, "Take Hit", false, 12f);
        sm.defaultState = idle;

        AddBoolTransition(idle, move, "IsMoving", true);
        AddBoolTransition(move, idle, "IsMoving", false);
        AddAnyTrigger(sm, attack, "Attack");
        AddAnyTrigger(sm, hurt, "Hurt");
        AddExitTransition(attack, idle, 0.9f);
        AddExitTransition(hurt, idle, 0.85f);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip CreateClip(string spriteFolder, string sheetName, bool loop, float frameRate)
    {
        string clipPath = $"{ControllerRoot}/{spriteFolder}_{sheetName.Replace(" ", string.Empty)}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip != null)
            return clip;

        clip = new AnimationClip();
        AssetDatabase.CreateAsset(clip, clipPath);

        Sprite[] sprites = LoadSprites(spriteFolder, sheetName);
        if (sprites.Length == 0)
            return clip;

        clip.frameRate = frameRate;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            frames[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        }, frames);

        if (sheetName.StartsWith("Attack"))
            AddAttackAnimationEvents(clip, sprites.Length / frameRate);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddAttackAnimationEvents(AnimationClip clip, float clipLength)
    {
        float openTime = Mathf.Max(0f, clipLength * 0.35f);
        float closeTime = Mathf.Max(openTime + 0.01f, clipLength * 0.68f);

        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = openTime, functionName = "OpenEnemyAttackHitbox" },
            new AnimationEvent { time = closeTime, functionName = "CloseEnemyAttackHitbox" }
        });
    }

    private static Sprite[] LoadSprites(string spriteFolder, string sheetName)
    {
        string path = $"{SpriteRoot}/{spriteFolder}/{sheetName}.png";
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        Sprite[] sprites = assets.OfType<Sprite>().OrderBy(sprite => ParseTrailingIndex(sprite.name)).ToArray();
        if (sprites.Length > 0)
            return sprites;

        Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return single != null ? new[] { single } : new Sprite[0];
    }

    private static Sprite LoadFirstSprite(string spriteFolder, string sheetName)
    {
        Sprite[] sprites = LoadSprites(spriteFolder, sheetName);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static string GetIdleSheet(string spriteFolder)
    {
        return spriteFolder == "Flying eye" ? "Flight" : "Idle";
    }

    private static string GetMoveSheet(string spriteFolder)
    {
        if (spriteFolder == "Flying eye") return "Flight";
        if (spriteFolder == "Skeleton") return "Walk";
        return "Run";
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddAnyTrigger(AnimatorStateMachine sm, AnimatorState to, string trigger)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.04f;
    }

    private static int ParseTrailingIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int index) ? index : 0;
    }

    private static AttackData CreateAttackData(string name, float damage, Vector2 knockback, float hitStop, float activeTime, float cooldown)
    {
        EnsureFolder("Assets/9.SO/Attack");
        string path = $"Assets/9.SO/Attack/{name}.asset";
        AttackData data = AssetDatabase.LoadAssetAtPath<AttackData>(path);
        if (data != null)
            return data;

        data = ScriptableObject.CreateInstance<AttackData>();
        AssetDatabase.CreateAsset(data, path);

        data.damage = damage;
        data.knockback = knockback;
        data.hitStopTime = hitStop;
        data.activeTime = activeTime;
        data.cooldown = cooldown;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject CreateProjectilePrefab(string name, Color color, float speed, float lifeTime)
    {
        EnsureFolder("Assets/7.Prefab/Projectile");
        string path = $"Assets/7.Prefab/Projectile/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSolidSprite($"{name}Sprite", color);
        renderer.sortingOrder = 12;
        root.transform.localScale = new Vector3(0.32f, 0.32f, 1f);

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.18f;

        Projectile projectile = root.AddComponent<Projectile>();
        SerializedObject projectileSo = new SerializedObject(projectile);
        projectileSo.FindProperty("speed").floatValue = speed;
        projectileSo.FindProperty("lifeTime").floatValue = lifeTime;
        projectileSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Sprite CreateSolidSprite(string name, Color color)
    {
        EnsureFolder("Assets/4.Sprite");
        string path = $"Assets/4.Sprite/{name}.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(16, 16);
            Color[] pixels = Enumerable.Repeat(color, 16 * 16).ToArray();
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

    private static void SavePrefab(GameObject root, string name)
    {
        string path = $"{GeneratedRoot}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static bool PrefabExists(string name)
    {
        string path = $"{GeneratedRoot}/{name}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        Directory.CreateDirectory(folder);
        AssetDatabase.ImportAsset(folder);
    }
}
