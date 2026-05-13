using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrototypeSceneBuilder
{
    private const string MarkerPath = "Assets/9.SO/.prototype_generated";
    private const string HeroKnightAppliedMarkerPath = "Assets/9.SO/.hero_knight_applied";
    private const string GuardParryAppliedMarkerPath = "Assets/9.SO/.guard_parry_applied";
    private const string TrainingSceneAppliedMarkerPath = "Assets/9.SO/.training_scene_applied";
    private const string DashEffectAppliedMarkerPath = "Assets/9.SO/.dash_effect_applied";
    private const string CombatHudAppliedMarkerPath = "Assets/9.SO/.combat_hud_applied";
    private const string CombatPolishAppliedMarkerPath = "Assets/9.SO/.combat_polish_applied";
    private const string StartScenePath = "Assets/1.Scene/StartScene.unity";
    private const string LoadingScenePath = "Assets/1.Scene/LoadingScene.unity";
    private const string GameScenePath = "Assets/1.Scene/GameScene.unity";
    private const string MartialHeroSpriteFolder = "Assets/Martial Hero/Sprites";
    private const string MartialHeroClipFolder = "Assets/5.Animation/MartialHero";
    private const string MartialHeroControllerPath = "Assets/5.Animation/MartialHero/MartialHero_Player.controller";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnce()
    {
        // 자동 프로토타입 생성은 수동으로 맞춘 씬/프리팹/콜라이더 값을 덮어쓸 수 있어서 비활성화한다.
    }

    [MenuItem("2DCombatProject/Build Temporary Prototype")]
    public static void BuildPrototype()
    {
        EditorUtility.DisplayDialog("Prototype Builder Disabled", "수동 세팅을 보호하기 위해 임시 프로토타입 빌더 실행을 막았습니다.", "OK");
        return;

#pragma warning disable CS0162
        EnsureFolders();

        AttackData basicAttack = CreateAttackData("BasicAttack", 10f, new Vector2(7f, 2f), 0.055f, 0.11f, 0.22f);
        AttackData dashAttack = CreateAttackData("DashAttack", 14f, new Vector2(10f, 2f), 0.065f, 0.16f, 0.4f);
        AttackData areaAttack = CreateAttackData("AreaAttack", 18f, new Vector2(5f, 4f), 0.08f, 0.2f, 0.8f);
        AttackData projectileAttack = CreateAttackData("PlayerProjectileAttack", 11f, new Vector2(6f, 1.5f), 0.045f, 0.12f, 0.55f);
        AttackData risingSlashAttack = CreateAttackData("RisingSlashAttack", 16f, new Vector2(4f, 8f), 0.07f, 0.18f, 0.7f);
        AttackData groundSlamAttack = CreateAttackData("GroundSlamAttack", 22f, new Vector2(3f, 9f), 0.09f, 0.24f, 1.1f);
        AttackData backStepShotAttack = CreateAttackData("BackStepShotAttack", 12f, new Vector2(7f, 2f), 0.05f, 0.12f, 0.75f);
        AttackData enemyMelee = CreateAttackData("EnemyMeleeAttack", 8f, new Vector2(5f, 2f), 0.04f, 0.12f, 0.65f);
        AttackData enemyProjectile = CreateAttackData("EnemyProjectileAttack", 7f, new Vector2(4f, 2f), 0.035f, 0.12f, 0.8f);

        GameObject playerProjectile = CreateProjectilePrefab("PlayerProjectile", Color.cyan);
        GameObject enemyProjectilePrefab = CreateProjectilePrefab("EnemyProjectile", Color.magenta);
        GameObject parryEffectPrefab = CreateParryEffectPrefab();
        GameObject slashEffectPrefab = CreateSimpleEffectPrefab("SlashEffect", new Color(0.65f, 0.9f, 1f, 0.72f), new Vector3(1.25f, 0.22f, 1f), 0.18f);
        GameObject risingEffectPrefab = CreateSimpleEffectPrefab("RisingSlashEffect", new Color(0.75f, 0.9f, 1f, 0.72f), new Vector3(0.48f, 1.15f, 1f), 0.7f);
        GameObject slamEffectPrefab = CreateSimpleEffectPrefab("SlamShockwaveEffect", new Color(1f, 0.72f, 0.25f, 0.78f), new Vector3(1.85f, 0.18f, 1f), 0.24f);

        SkillData skillDash = CreateSkillData("Skill_DashAttack", SkillType.DashAttack, dashAttack, 1.2f, 0.14f, 2.5f, 20f, null);

        SkillData skillProjectile = CreateSkillData("Skill_ProjectileSlash", SkillType.Projectile, projectileAttack, 0.9f, 0.12f, 7f, 0f, playerProjectile);
        SkillData skillRising = CreateSkillData("Skill_RisingSlash", SkillType.RisingSlash, risingSlashAttack, 1.4f, 0.22f, 2.0f, 15f, null);
        SkillData skillSlam = CreateSkillData("Skill_GroundSlam", SkillType.GroundSlam, groundSlamAttack, 2.4f, 0.34f, 2.6f, 22f, null);
      
        SkillData[] playerSkills = { skillProjectile, skillDash, skillRising, skillSlam };

        GameObject playerPrefab = CreatePlayerPrefab(basicAttack, playerSkills, parryEffectPrefab, slashEffectPrefab, risingEffectPrefab, slamEffectPrefab);
        GameObject meleeEnemyPrefab = CreateMeleeEnemyPrefab(enemyMelee);
        GameObject rangedEnemyPrefab = CreateRangedEnemyPrefab(enemyProjectile, enemyProjectilePrefab);
        GameObject trainingDummyPrefab = CreateTrainingDummyPrefab();

        WaveData[] waves = CreateWaveData(meleeEnemyPrefab, rangedEnemyPrefab);

        CreateStartScene();
        CreateLoadingScene();
        CreateGameScene(playerPrefab, meleeEnemyPrefab, rangedEnemyPrefab, trainingDummyPrefab, playerProjectile, enemyProjectilePrefab, parryEffectPrefab, slashEffectPrefab, risingEffectPrefab, slamEffectPrefab, waves);
        SetBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PrototypeSceneBuilder] Temporary prototype generated.");
#pragma warning restore CS0162
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
            "Assets/7.Prefab/Effect",
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

    private static GameObject CreatePlayerPrefab(AttackData basicAttack, SkillData[] playerSkills, GameObject parryEffectPrefab, GameObject slashEffectPrefab, GameObject risingEffectPrefab, GameObject slamEffectPrefab)
    {
        GameObject root = new GameObject("Player");
        root.tag = "Player";
        root.layer = LayerMask.NameToLayer("Default");
        root.transform.position = Vector3.zero;

        Animator playerAnimator = AttachMartialHeroVisual(root);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.gravityScale = 3.2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D body = root.AddComponent<BoxCollider2D>();
        body.size = new Vector2(0.8f, 1.35f);
        body.offset = new Vector2(0f, 0.03f);

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.Player;
        healthSo.FindProperty("maxHp").floatValue = 100f;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Hurtbox>();
        root.AddComponent<PlayerInputReader>();
        PlayerMovement2D movement = root.AddComponent<PlayerMovement2D>();
        PlayerGuard guard = root.AddComponent<PlayerGuard>();
        root.AddComponent<ListRendererCache>();
        root.AddComponent<CombatFeedback>();
        AddPlayerHeroKnightAnimator(root);

        SerializedObject guardSo = new SerializedObject(guard);
        guardSo.FindProperty("input").objectReferenceValue = root.GetComponent<PlayerInputReader>();
        guardSo.FindProperty("rb").objectReferenceValue = rb;
        guardSo.FindProperty("animator").objectReferenceValue = playerAnimator;
        guardSo.FindProperty("parryEffectPrefab").objectReferenceValue = parryEffectPrefab;
        guardSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.72f, 0f);

        GameObject basicHitbox = CreateHitboxChild(root.transform, "BasicAttackHitbox", new Vector2(1.25f, 0.75f), new Vector3(0.85f, 0.05f, 0f));
        Hitbox basicHitboxComp = basicHitbox.GetComponent<Hitbox>();

        GameObject skillHitbox = CreateHitboxChild(root.transform, "SkillHitbox", new Vector2(2.45f, 1.15f), new Vector3(1.15f, 0.05f, 0f));
        Hitbox skillHitboxComp = skillHitbox.GetComponent<Hitbox>();

        GameObject projectileSpawn = new GameObject("ProjectileSpawnPoint");
        projectileSpawn.transform.SetParent(root.transform);
        projectileSpawn.transform.localPosition = new Vector3(0.85f, 0.28f, 0f);

        PlayerCombat combat = root.AddComponent<PlayerCombat>();
        SerializedObject combatSo = new SerializedObject(combat);
        combatSo.FindProperty("basicAttackHitbox").objectReferenceValue = basicHitboxComp;
        combatSo.FindProperty("basicAttack").objectReferenceValue = basicAttack;
        combatSo.FindProperty("animator").objectReferenceValue = playerAnimator;
        combatSo.ApplyModifiedPropertiesWithoutUndo();

        PlayerSkillController skills = root.AddComponent<PlayerSkillController>();
        SerializedObject skillSo = new SerializedObject(skills);
        skillSo.FindProperty("movement").objectReferenceValue = movement;
        skillSo.FindProperty("rb").objectReferenceValue = rb;
        skillSo.FindProperty("animator").objectReferenceValue = playerAnimator;
        skillSo.FindProperty("skillHitbox").objectReferenceValue = skillHitboxComp;
        skillSo.FindProperty("projectileSpawnPoint").objectReferenceValue = projectileSpawn.transform;
        skillSo.FindProperty("skillOne").objectReferenceValue = playerSkills.Length > 0 ? playerSkills[0] : null;
        skillSo.FindProperty("skillTwo").objectReferenceValue = playerSkills.Length > 1 ? playerSkills[1] : null;
        skillSo.FindProperty("slashEffectPrefab").objectReferenceValue = slashEffectPrefab;
        skillSo.FindProperty("risingEffectPrefab").objectReferenceValue = risingEffectPrefab;
        skillSo.FindProperty("slamEffectPrefab").objectReferenceValue = slamEffectPrefab;
        SerializedProperty availableSkills = skillSo.FindProperty("availableSkills");
        availableSkills.arraySize = playerSkills.Length;
        for (int i = 0; i < playerSkills.Length; i++)
            availableSkills.GetArrayElementAtIndex(i).objectReferenceValue = playerSkills[i];
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

    private static Animator AttachMartialHeroVisual(GameObject root)
    {
        AnimatorController controller = CreateMartialHeroAnimatorController();
        Sprite idleSprite = LoadMartialHeroSprites("Idle").FirstOrDefault();

        if (idleSprite == null)
        {
            SpriteRenderer fallbackRenderer = root.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = CreateSprite("PlayerSprite", new Color(0.2f, 0.65f, 1f));
            fallbackRenderer.sortingOrder = 10;
            Debug.LogWarning("[PrototypeSceneBuilder] Martial Hero sprites not found. Fallback sprite used.");
            return null;
        }

        GameObject visual = new GameObject("Visual_MartialHero");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.69f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = idleSprite;
        renderer.sortingOrder = 10;

        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        return animator;
    }

    private static AnimatorController CreateMartialHeroAnimatorController()
    {
        EnsureAssetFolder(MartialHeroClipFolder);

        AnimationClip idle = CreateMartialHeroClip("Idle", "MartialHero_Idle", 10f, true);
        AnimationClip run = CreateMartialHeroClip("Run", "MartialHero_Run", 12f, true);
        AnimationClip jump = CreateMartialHeroClip("Jump", "MartialHero_Jump", 10f, false);
        AnimationClip fall = CreateMartialHeroClip("Fall", "MartialHero_Fall", 10f, true);
        AnimationClip dash = CreateMartialHeroClip("Run", "MartialHero_Dash", 18f, false);
        AnimationClip attackOne = CreateMartialHeroClip("Attack1", "MartialHero_Attack1", 14f, false);
        AnimationClip attackTwo = CreateMartialHeroClip("Attack2", "MartialHero_Attack2", 14f, false);
        AnimationClip block = CreateMartialHeroClip("Take Hit - white silhouette", "MartialHero_Block", 14f, false);
        AnimationClip hurt = CreateMartialHeroClip("Take Hit", "MartialHero_Hurt", 12f, false);
        AnimationClip death = CreateMartialHeroClip("Death", "MartialHero_Death", 10f, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MartialHeroControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(MartialHeroControllerPath);

        ResetController(controller);
        AddMartialHeroParameters(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(260f, 0f, 0f));
        AnimatorState runState = stateMachine.AddState("Run", new Vector3(520f, 0f, 0f));
        AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(260f, 180f, 0f));
        AnimatorState fallState = stateMachine.AddState("Fall", new Vector3(520f, 180f, 0f));
        AnimatorState dashState = stateMachine.AddState("Dash", new Vector3(520f, 300f, 0f));
        AnimatorState attackOneState = stateMachine.AddState("Attack1", new Vector3(260f, -180f, 0f));
        AnimatorState attackTwoState = stateMachine.AddState("Attack2", new Vector3(520f, -180f, 0f));
        AnimatorState blockState = stateMachine.AddState("Block", new Vector3(780f, -260f, 0f));
        AnimatorState hurtState = stateMachine.AddState("Hurt", new Vector3(780f, -90f, 0f));
        AnimatorState deathState = stateMachine.AddState("Death", new Vector3(780f, 90f, 0f));

        idleState.motion = idle;
        runState.motion = run;
        jumpState.motion = jump;
        fallState.motion = fall;
        dashState.motion = dash;
        attackOneState.motion = attackOne;
        attackTwoState.motion = attackTwo;
        blockState.motion = block;
        hurtState.motion = hurt;
        deathState.motion = death;
        stateMachine.defaultState = idleState;

        AddBoolTransition(idleState, runState, "IsMoving", true);
        AddBoolTransition(runState, idleState, "IsMoving", false);
        AddBoolTransition(idleState, fallState, "Grounded", false);
        AddBoolTransition(runState, fallState, "Grounded", false);
        AddBoolTransition(fallState, idleState, "Grounded", true);
        AddExitTransition(jumpState, fallState, 0.85f);
        AddExitTransition(dashState, idleState, 0.85f);
        AddExitTransition(attackOneState, idleState, 0.9f);
        AddExitTransition(attackTwoState, idleState, 0.9f);
        AddExitTransition(blockState, idleState, 0.75f);
        AddExitTransition(hurtState, idleState, 0.9f);

        AddAnyStateTrigger(stateMachine, jumpState, "Jump");
        AddAnyStateTrigger(stateMachine, attackOneState, "Attack1");
        AddAnyStateTrigger(stateMachine, attackTwoState, "Attack2");
        AddAnyStateTrigger(stateMachine, attackTwoState, "Attack3");
        AddAnyStateTrigger(stateMachine, blockState, "Block");
        AddAnyStateTrigger(stateMachine, hurtState, "Hurt");
        AddAnyStateTrigger(stateMachine, deathState, "Death");
        AddAnyStateTrigger(stateMachine, dashState, "Dash");
        AddAnyStateTrigger(stateMachine, dashState, "Roll");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ResetController(AnimatorController controller)
    {
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters[0]);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        foreach (ChildAnimatorState state in stateMachine.states)
            stateMachine.RemoveState(state.state);

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            stateMachine.RemoveAnyStateTransition(transition);
    }

    private static void AddMartialHeroParameters(AnimatorController controller)
    {
        controller.AddParameter("AnimState", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Block", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
    }

    private static AnimationClip CreateMartialHeroClip(string spriteSheetName, string clipName, float frameRate, bool loop)
    {
        Sprite[] sprites = LoadMartialHeroSprites(spriteSheetName);
        string path = $"{MartialHeroClipFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new()
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static Sprite[] LoadMartialHeroSprites(string spriteSheetName)
    {
        string path = $"{MartialHeroSpriteFolder}/{spriteSheetName}.png";
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        Sprite[] sprites = assets.OfType<Sprite>()
            .OrderBy(sprite => ParseTrailingIndex(sprite.name))
            .ToArray();

        if (sprites.Length > 0)
            return sprites;

        Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return singleSprite != null ? new[] { singleSprite } : new Sprite[0];
    }

    private static int ParseTrailingIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int index) ? index : 0;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.04f;
    }

    private static void AddAnyStateTrigger(AnimatorStateMachine stateMachine, AnimatorState to, string trigger)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddPlayerHeroKnightAnimator(GameObject root)
    {
        AddComponentByTypeName(root, "PlayerHeroKnightAnimator");
    }

    private static Component AddComponentByTypeName(GameObject target, string typeName)
    {
        System.Type componentType = System.Type.GetType($"{typeName}, Assembly-CSharp");
        if (componentType == null)
        {
            Debug.LogWarning($"[PrototypeSceneBuilder] {typeName} type not found yet. Refresh scripts, then run the builder again.");
            return null;
        }

        return target.AddComponent(componentType);
    }

    private static void SetCameraFollowTarget(Component cameraFollow, Transform target)
    {
        if (cameraFollow == null || target == null)
            return;

        SerializedObject followSo = new SerializedObject(cameraFollow);
        followSo.FindProperty("target").objectReferenceValue = target;
        followSo.ApplyModifiedPropertiesWithoutUndo();
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

    private static GameObject CreateTrainingDummyPrefab()
    {
        GameObject root = new GameObject("TrainingDummy");

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite("TrainingDummySprite", new Color(0.95f, 0.72f, 0.25f));
        renderer.sortingOrder = 9;
        root.transform.localScale = new Vector3(0.9f, 1.55f, 1f);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = new Vector2(0.75f, 1.15f);
        collider.offset = new Vector2(0f, 0.03f);

        Health health = root.AddComponent<Health>();
        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
        healthSo.FindProperty("maxHp").floatValue = 999f;
        healthSo.FindProperty("invincibleTime").floatValue = 0.04f;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Hurtbox>();
        root.AddComponent<ListRendererCache>();
        CombatFeedback feedback = root.AddComponent<CombatFeedback>();
        SerializedObject feedbackSo = new SerializedObject(feedback);
        feedbackSo.FindProperty("applyPhysicalKnockback").boolValue = false;
        feedbackSo.FindProperty("spawnDamageText").boolValue = true;
        feedbackSo.FindProperty("damageTextColor").colorValue = new Color(1f, 0.92f, 0.22f);
        feedbackSo.ApplyModifiedPropertiesWithoutUndo();

        AddComponentByTypeName(root, "TrainingDummy");

        string path = "Assets/7.Prefab/Enemy/TrainingDummy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateEnemyRoot(string name, Color color)
    {
        GameObject root = new GameObject(name);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = name == "PlayerProjectile"
            ? CreateSprite("SlashEffectSprite", new Color(0.65f, 0.9f, 1f, 0.72f))
            : CreateSprite($"{name}Sprite", color);
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
        if (name == "PlayerProjectile")
            root.transform.localScale = new Vector3(2.35f, 0.28f, 1f);

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = name == "PlayerProjectile" ? 0.26f : 0.15f;

        Projectile projectile = root.AddComponent<Projectile>();
        if (name == "PlayerProjectile")
        {
            SerializedObject projectileSo = new SerializedObject(projectile);
            projectileSo.FindProperty("speed").floatValue = 10f;
            projectileSo.FindProperty("lifeTime").floatValue = 0.75f;
            projectileSo.ApplyModifiedPropertiesWithoutUndo();
        }

        string path = $"Assets/7.Prefab/Projectile/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSimpleEffectPrefab(string name, Color color, Vector3 scale, float lifeTime)
    {
        GameObject root = new GameObject(name);
        root.transform.localScale = scale;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite($"{name}Sprite", color);
        renderer.sortingOrder = 28;

        TimedAutoRelease autoRelease = root.AddComponent<TimedAutoRelease>();
        SerializedObject autoSo = new SerializedObject(autoRelease);
        autoSo.FindProperty("lifeTime").floatValue = lifeTime;
        autoSo.ApplyModifiedPropertiesWithoutUndo();

        string path = $"Assets/7.Prefab/Effect/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateParryEffectPrefab()
    {
        GameObject root = new GameObject("ParryEffect");

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite("ParryEffectSprite", new Color(1f, 0.82f, 0.15f, 0.9f));
        renderer.sortingOrder = 30;
        root.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        AnimationClip shieldFlash = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/5.Animation/KnightMotion/Particle/Ptk_ShieldFlash.anim");
        if (shieldFlash != null)
        {
            AnimatorController controller = CreateSingleClipController("Assets/5.Animation/KnightMotion/Particle/ParryEffect.controller", shieldFlash);
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
        }

        TimedAutoRelease autoRelease = root.AddComponent<TimedAutoRelease>();
        SerializedObject autoSo = new SerializedObject(autoRelease);
        autoSo.FindProperty("lifeTime").floatValue = 0.28f;
        autoSo.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/7.Prefab/Effect/ParryEffect.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static AnimatorController CreateSingleClipController(string controllerPath, AnimationClip clip)
    {
        EnsureAssetFolder(Path.GetDirectoryName(controllerPath).Replace("\\", "/"));

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        ResetController(controller);
        AnimatorState state = controller.layers[0].stateMachine.AddState(clip.name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
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

    private static Sprite CreateCircleSprite(string name, Color color)
    {
        string path = $"Assets/4.Sprite/{name}.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            const int size = 64;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(color.r, color.g, color.b, 0f);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size - 2) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= radius ? color : clear);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Bilinear;
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

    private static void CreateGameScene(GameObject playerPrefab, GameObject meleeEnemyPrefab, GameObject rangedEnemyPrefab, GameObject trainingDummyPrefab, GameObject playerProjectile, GameObject enemyProjectile, GameObject parryEffectPrefab, GameObject slashEffectPrefab, GameObject risingEffectPrefab, GameObject slamEffectPrefab, WaveData[] waves)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraRig = new GameObject("CameraRig");
        cameraRig.transform.position = new Vector3(0f, 1.2f, -10f);

        GameObject camera = new GameObject("Main Camera");
        camera.transform.SetParent(cameraRig.transform, false);
        Camera cam = camera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camera.tag = "MainCamera";
        camera.AddComponent<CameraShake>();
        Component cameraFollow = AddComponentByTypeName(cameraRig, "CameraFollow2D");

        GameObject managers = new GameObject("@Managers");
        managers.AddComponent<GameManager>().StartGame();
        ObjectPool pool = managers.AddComponent<ObjectPool>();
        SerializedObject poolSo = new SerializedObject(pool);
        SerializedProperty entries = poolSo.FindProperty("initialPools");
        entries.arraySize = 8;
        SetPoolEntry(entries.GetArrayElementAtIndex(0), meleeEnemyPrefab, 6);
        SetPoolEntry(entries.GetArrayElementAtIndex(1), rangedEnemyPrefab, 4);
        SetPoolEntry(entries.GetArrayElementAtIndex(2), enemyProjectile, 10);
        SetPoolEntry(entries.GetArrayElementAtIndex(3), playerProjectile, 8);
        SetPoolEntry(entries.GetArrayElementAtIndex(4), parryEffectPrefab, 6);
        SetPoolEntry(entries.GetArrayElementAtIndex(5), slashEffectPrefab, 8);
        SetPoolEntry(entries.GetArrayElementAtIndex(6), risingEffectPrefab, 4);
        SetPoolEntry(entries.GetArrayElementAtIndex(7), slamEffectPrefab, 4);
        poolSo.ApplyModifiedPropertiesWithoutUndo();

        CreateGround("Ground_LongArena", new Vector3(0f, -1.25f, 0f), new Vector2(38f, 0.6f), new Color(0.25f, 0.25f, 0.25f));
        CreateGround("LeftWall", new Vector3(-19.2f, 1.5f, 0f), new Vector2(0.4f, 5f), new Color(0.18f, 0.18f, 0.18f));
        CreateGround("RightWall", new Vector3(19.2f, 1.5f, 0f), new Vector2(0.4f, 5f), new Color(0.18f, 0.18f, 0.18f));
        CreateOneWayPlatform("Platform_Low_L", new Vector3(-11f, 0.35f, 0f), new Vector2(4.2f, 0.35f), new Color(0.32f, 0.32f, 0.32f));
        CreateOneWayPlatform("Platform_Low_R", new Vector3(11f, 0.35f, 0f), new Vector2(4.2f, 0.35f), new Color(0.32f, 0.32f, 0.32f));
        CreateOneWayPlatform("Platform_Mid_L", new Vector3(-5.5f, 1.55f, 0f), new Vector2(3.2f, 0.35f), new Color(0.34f, 0.34f, 0.34f));
        CreateOneWayPlatform("Platform_Mid_R", new Vector3(5.5f, 1.55f, 0f), new Vector2(3.2f, 0.35f), new Color(0.34f, 0.34f, 0.34f));
        CreateGround("CenterBlock", new Vector3(0f, 0.1f, 0f), new Vector2(1.8f, 2.1f), new Color(0.29f, 0.29f, 0.29f));
        CreateGround("StepBlock_L", new Vector3(-2.6f, -0.42f, 0f), new Vector2(1.2f, 1.05f), new Color(0.3f, 0.3f, 0.3f));
        CreateGround("StepBlock_R", new Vector3(2.6f, -0.42f, 0f), new Vector2(1.2f, 1.05f), new Color(0.3f, 0.3f, 0.3f));

        PrefabUtility.InstantiatePrefab(playerPrefab);
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(-4f, 0f, 0f);
            SetCameraFollowTarget(cameraFollow, player.transform);
        }

        GameObject dummy = PrefabUtility.InstantiatePrefab(trainingDummyPrefab) as GameObject;
        if (dummy != null)
            dummy.transform.position = new Vector3(3.5f, -0.35f, 0f);

        GameObject waveObject = new GameObject("WaveManager");
        waveObject.SetActive(false);
        WaveManager waveManager = waveObject.AddComponent<WaveManager>();
        SerializedObject waveSo = new SerializedObject(waveManager);
        SerializedProperty waveList = waveSo.FindProperty("waves");
        waveList.arraySize = waves.Length;
        for (int i = 0; i < waves.Length; i++)
            waveList.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];

        SerializedProperty spawnList = waveSo.FindProperty("spawnPoints");
        spawnList.arraySize = 5;
        spawnList.GetArrayElementAtIndex(0).objectReferenceValue = CreateSpawnPoint("SpawnPoint_Far_L", new Vector3(-15.5f, -0.5f, 0f));
        spawnList.GetArrayElementAtIndex(1).objectReferenceValue = CreateSpawnPoint("SpawnPoint_Far_R", new Vector3(15.5f, -0.5f, 0f));
        spawnList.GetArrayElementAtIndex(2).objectReferenceValue = CreateSpawnPoint("SpawnPoint_Mid_L", new Vector3(-8.5f, 0.95f, 0f));
        spawnList.GetArrayElementAtIndex(3).objectReferenceValue = CreateSpawnPoint("SpawnPoint_Mid_R", new Vector3(8.5f, 0.95f, 0f));
        spawnList.GetArrayElementAtIndex(4).objectReferenceValue = CreateSpawnPoint("SpawnPoint_Center", new Vector3(0f, 1.45f, 0f));
        waveSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject canvas = CreateCanvas();
        CreateText(canvas.transform, "GuideText", "Move  Jump  Dash  Attack  Guard  Skills", new Vector2(0f, 485f), 17, TextAnchor.MiddleCenter);
        CreateComboCounter(canvas.transform);
        if (player != null)
            CreateCombatHud(canvas.transform, player.GetComponent<Health>(), player.GetComponent<PlayerSkillController>());

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

    private static void CreateOneWayPlatform(string name, Vector3 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite($"{name}Sprite", color);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        EdgeCollider2D collider = go.AddComponent<EdgeCollider2D>();
        collider.points = new[]
        {
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        };
        collider.usedByEffector = true;

        PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 120f;
        effector.sideArc = 0f;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
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

    private static void CreateComboCounter(Transform parent)
    {
        Text comboText = CreateText(parent, "ComboText", string.Empty, new Vector2(0f, 145f), 34, TextAnchor.MiddleCenter);
        comboText.color = new Color(1f, 0.9f, 0.18f);
        comboText.gameObject.SetActive(false);

        Component counter = AddComponentByTypeName(parent.gameObject, "ComboCounterUI");
        if (counter != null)
        {
            SerializedObject counterSo = new SerializedObject(counter);
            counterSo.FindProperty("comboText").objectReferenceValue = comboText;
            counterSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateCombatHud(Transform parent, Health playerHealth, PlayerSkillController controller)
    {
        GameObject root = new GameObject("CombatHUD");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(520f, 158f);
        rect.anchoredPosition = new Vector2(30f, 26f);

        Image panel = root.AddComponent<Image>();
        panel.color = new Color(0.015f, 0.02f, 0.035f, 0.78f);

        CreateHudFrame(root.transform, new Vector2(0f, 0f), new Vector2(520f, 158f), new Color(0.12f, 0.22f, 0.35f, 0.95f));
        CreateSkillSlotBar(root.transform, controller);
        Slider hpSlider = CreateHpBar(root.transform, out Text hpText);

        HUDView hud = root.AddComponent<HUDView>();
        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        hudSo.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        hudSo.FindProperty("hpText").objectReferenceValue = hpText;
        hudSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Slider CreateHpBar(Transform parent, out Text hpText)
    {
        GameObject root = new GameObject("PlayerHpBar");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(410f, 30f);
        rect.anchoredPosition = new Vector2(88f, 18f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.035f, 0.04f, 0.07f, 0.95f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.08f, 0.88f, 0.48f, 1f);

        Slider slider = root.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;

        hpText = CreateText(root.transform, "HpText", "100 / 100", new Vector2(205f, 0f), 18, TextAnchor.MiddleCenter);
        hpText.rectTransform.anchorMin = new Vector2(0f, 0f);
        hpText.rectTransform.anchorMax = new Vector2(0f, 0f);
        hpText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        hpText.rectTransform.sizeDelta = new Vector2(180f, 28f);
        hpText.color = Color.white;

        Text hpLabel = CreateText(parent, "HpLabel", "HP", new Vector2(42f, 18f), 20, TextAnchor.MiddleCenter);
        hpLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        hpLabel.rectTransform.anchorMax = new Vector2(0f, 0f);
        hpLabel.rectTransform.sizeDelta = new Vector2(56f, 30f);
        hpLabel.color = new Color(0.7f, 0.95f, 1f);

        return slider;
    }

    private static void CreateHudFrame(Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject frame = new GameObject("Frame");
        frame.transform.SetParent(parent, false);

        RectTransform rect = frame.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = frame.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Outline outline = frame.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(3f, -3f);
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
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(250f, 82f);
        rect.anchoredPosition = new Vector2(22f, 64f);

        SkillSlotBarUI bar = root.AddComponent<SkillSlotBarUI>();
        SkillSlotUI slotOne = CreateSkillSlot(root.transform, "SkillSlot_A", new Vector2(56f, 40f));
        SkillSlotUI slotTwo = CreateSkillSlot(root.transform, "SkillSlot_S", new Vector2(178f, 40f));

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
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.anchoredPosition = anchoredPosition;

        Image background = slot.AddComponent<Image>();
        background.sprite = CreateCircleSprite("SkillSlotCircleSprite", Color.white);
        background.color = new Color(0.025f, 0.035f, 0.065f, 0.96f);
        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.48f, 0.78f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);
        slot.AddComponent<CanvasGroup>();

        Text keyText = CreateText(slot.transform, "KeyText", "A", new Vector2(0f, -47f), 18, TextAnchor.MiddleCenter);
        keyText.rectTransform.sizeDelta = new Vector2(36f, 24f);
        keyText.color = new Color(0.72f, 0.95f, 1f);

        Text skillText = CreateText(slot.transform, "SkillText", "Skill", new Vector2(0f, 0f), 15, TextAnchor.MiddleCenter);
        skillText.rectTransform.sizeDelta = new Vector2(62f, 42f);
        skillText.color = Color.white;

        Image cooldownFill = CreateCooldownFill(slot.transform);
        Text cooldownText = CreateText(slot.transform, "CooldownText", string.Empty, Vector2.zero, 22, TextAnchor.MiddleCenter);
        cooldownText.rectTransform.anchorMin = Vector2.zero;
        cooldownText.rectTransform.anchorMax = Vector2.one;
        cooldownText.rectTransform.offsetMin = Vector2.zero;
        cooldownText.rectTransform.offsetMax = Vector2.zero;
        cooldownText.fontStyle = FontStyle.Bold;
        cooldownText.raycastTarget = false;
        cooldownText.enabled = false;

        SkillSlotUI ui = slot.AddComponent<SkillSlotUI>();
        SerializedObject uiSo = new SerializedObject(ui);
        uiSo.FindProperty("keyText").objectReferenceValue = keyText;
        uiSo.FindProperty("skillText").objectReferenceValue = skillText;
        uiSo.FindProperty("background").objectReferenceValue = background;
        uiSo.FindProperty("cooldownFill").objectReferenceValue = cooldownFill;
        uiSo.FindProperty("cooldownText").objectReferenceValue = cooldownText;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        return ui;
    }

    private static Image CreateCooldownFill(Transform parent)
    {
        GameObject fillObject = new GameObject("CooldownFill");
        fillObject.transform.SetParent(parent, false);

        RectTransform rect = fillObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = fillObject.AddComponent<Image>();
        image.sprite = CreateCircleSprite("SkillSlotCircleSprite", Color.white);
        image.color = new Color(0f, 0f, 0f, 0.68f);
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        image.fillAmount = 0f;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
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

    private static void EnsureAssetFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
