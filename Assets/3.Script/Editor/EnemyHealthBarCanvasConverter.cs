using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EnemyHealthBarCanvasConverter
{
    private const string AutoConvertKey = "2DCombatProject.EnemyHealthBarCanvasConverter.V1";

    private static readonly string[] PrefabPaths =
    {
        "Assets/7.Prefab/Enemy/TrainingDummy.prefab",
        "Assets/7.Prefab/Enemy/Fantasy/FlyingEyeEnemy.prefab",
        "Assets/7.Prefab/Enemy/Fantasy/GoblinEnemy.prefab",
        "Assets/7.Prefab/Enemy/Fantasy/MushroomEnemy.prefab",
        "Assets/7.Prefab/Enemy/Fantasy/SkeletonEnemy.prefab",
    };

    [MenuItem("Tools/2DCombat/Convert Enemy Health Bars To Canvas")]
    public static void ConvertKnownEnemyPrefabs()
    {
        for (int i = 0; i < PrefabPaths.Length; i++)
            ConvertPrefab(PrefabPaths[i]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Enemy health bars converted to World Space Canvas.");
    }

    [InitializeOnLoadMethod]
    private static void AutoConvertOnceAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(AutoConvertKey, false))
                return;

            bool convertedAny = false;
            for (int i = 0; i < PrefabPaths.Length; i++)
                convertedAny |= ConvertPrefabIfNeeded(PrefabPaths[i]);

            EditorPrefs.SetBool(AutoConvertKey, true);

            if (convertedAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Enemy health bars auto-converted to World Space Canvas.");
            }
        };
    }

    // 적 프리팹 안의 HealthBar 자식만 World Space Canvas 기반 체력바로 교체한다.
    private static void ConvertPrefab(string path)
    {
        ConvertPrefabInternal(path);
    }

    private static bool ConvertPrefabIfNeeded(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            return false;

        try
        {
            Transform healthBar = root.transform.Find("HealthBar");
            if (healthBar != null && healthBar.GetComponent<Canvas>() != null)
                return false;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        ConvertPrefabInternal(path);
        return true;
    }

    private static void ConvertPrefabInternal(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
            return;

        try
        {
            Health health = root.GetComponent<Health>();
            Transform oldBar = root.transform.Find("HealthBar");
            Vector3 localPosition = oldBar != null ? oldBar.localPosition : new Vector3(0f, 1.25f, 0f);
            Quaternion localRotation = oldBar != null ? oldBar.localRotation : Quaternion.identity;
            Vector3 localScale = oldBar != null ? oldBar.localScale : Vector3.one;

            if (oldBar != null)
                Object.DestroyImmediate(oldBar.gameObject);

            GameObject bar = new GameObject("HealthBar", typeof(RectTransform));
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = localRotation;
            bar.transform.localScale = localScale;

            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(84f, 10f);

            Canvas canvas = bar.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            CanvasScaler scaler = bar.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 32f;
            bar.AddComponent<GraphicRaycaster>();

            Image back = CreateImage(bar.transform, "Back", new Color(0.02f, 0.025f, 0.03f, 0.95f), new Vector2(84f, 10f));
            Image fill = CreateImage(bar.transform, "Fill", new Color(0.95f, 0.08f, 0.12f, 1f), new Vector2(78f, 6f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            EnemyWorldHealthBar healthBar = bar.AddComponent<EnemyWorldHealthBar>();
            SerializedObject serialized = new SerializedObject(healthBar);
            serialized.FindProperty("health").objectReferenceValue = health;
            serialized.FindProperty("barRoot").objectReferenceValue = bar;
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("fillImage").objectReferenceValue = fill;
            serialized.FindProperty("backImage").objectReferenceValue = back;
            serialized.FindProperty("hideOnDead").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Image CreateImage(Transform parent, string name, Color color, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
