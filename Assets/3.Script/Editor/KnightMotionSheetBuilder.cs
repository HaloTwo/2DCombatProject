using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KnightMotionSheetBuilder
{
    private const string AttackSourcePath = "Assets/4.Sprite/KnightMotion/KnightAttack.png";
    private const string ParticleSourcePath = "Assets/4.Sprite/KnightMotion/KnightPtk.png";
    private const string AttackClipFolder = "Assets/5.Animation/KnightMotion/Attack";
    private const string ParticleClipFolder = "Assets/5.Animation/KnightMotion/Particle";
    private const int Columns = 12;
    private const int Rows = 8;
    private const float AttackFrameRate = 12f;
    private const float ParticleFrameRate = 14f;
    private const float PixelsPerUnit = 64f;

    private readonly struct MotionRange
    {
        public readonly string Name;
        public readonly int Row;
        public readonly int StartColumn;
        public readonly int EndColumn;

        public MotionRange(string name, int row, int startColumn, int endColumn)
        {
            Name = name;
            Row = row;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }
    }

    private static readonly MotionRange[] AttackMotions =
    {
        new("Knight_IdleReady", 0, 0, 3),
        new("Knight_RunForward", 0, 6, 11),
        new("Knight_GuardMove", 1, 0, 6),
        new("Knight_ThrustReady", 1, 7, 11),
        new("Knight_WideSlash_A", 2, 0, 4),
        new("Knight_WideSlash_B", 2, 5, 11),
        new("Knight_UpperSlash", 3, 0, 5),
        new("Knight_Recover", 3, 6, 11),
        new("Knight_CombatIdle", 4, 0, 11),
        new("Knight_KnockdownDeath", 5, 0, 7),
        new("Knight_BlockImpact", 6, 8, 11),
        new("Knight_DodgeLow", 7, 0, 5),
        new("Knight_WalkReturn", 7, 6, 11),
    };

    private static readonly MotionRange[] ParticleMotions =
    {
        new("Ptk_DirtSmall", 0, 0, 11),
        new("Ptk_DustCloud", 1, 0, 11),
        new("Ptk_GroundSpike", 2, 0, 6),
        new("Ptk_RockDebris", 2, 7, 11),
        new("Ptk_SlashDust", 3, 0, 11),
        new("Ptk_WhiteSmoke", 4, 0, 11),
        new("Ptk_GraySmoke", 5, 0, 11),
        new("Ptk_ShieldFlash", 6, 0, 3),
        new("Ptk_ArrowSlash", 6, 4, 7),
        new("Ptk_FireProjectile", 6, 8, 11),
        new("Ptk_FireballLoop", 7, 0, 5),
        new("Ptk_FireImpact", 7, 6, 11),
    };

    [MenuItem("2DCombatProject/Import Knight Attack And Particles")]
    public static void ImportSheets()
    {
        SliceSheetAsMultipleSprites(AttackSourcePath, "KnightAttack");
        SliceSheetAsMultipleSprites(ParticleSourcePath, "KnightPtk");

        EnsureFolder(AttackClipFolder);
        EnsureFolder(ParticleClipFolder);

        CreateClipsFromSheet(AttackSourcePath, "KnightAttack", AttackClipFolder, AttackMotions, AttackFrameRate);
        CreateClipsFromSheet(ParticleSourcePath, "KnightPtk", ParticleClipFolder, ParticleMotions, ParticleFrameRate);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[KnightMotionSheetBuilder] Knight sheets sliced as Multiple sprites and clips generated.");
    }

    private static void SliceSheetAsMultipleSprites(string sourcePath, string spritePrefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[KnightMotionSheetBuilder] Source sheet not found: {sourcePath}");
            return;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (texture == null)
        {
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        }

        if (texture == null)
        {
            Debug.LogError($"[KnightMotionSheetBuilder] Texture load failed: {sourcePath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        float cellWidth = texture.width / (float)Columns;
        float cellHeight = texture.height / (float)Rows;
        SpriteMetaData[] sprites = new SpriteMetaData[Columns * Rows];

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int index = row * Columns + column;
                sprites[index] = new SpriteMetaData
                {
                    name = $"{spritePrefix}_{index}",
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(
                        Mathf.RoundToInt(column * cellWidth),
                        Mathf.RoundToInt(texture.height - (row + 1) * cellHeight),
                        Mathf.RoundToInt(cellWidth),
                        Mathf.RoundToInt(cellHeight))
                };
            }
        }

#pragma warning disable CS0618
        importer.spritesheet = sprites;
#pragma warning restore CS0618
        importer.SaveAndReimport();
    }

    private static void CreateClipsFromSheet(string sourcePath, string spritePrefix, string clipFolder, MotionRange[] motions, float frameRate)
    {
        Dictionary<int, Sprite> spritesByIndex = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath)
            .OfType<Sprite>()
            .Select(sprite => new { Sprite = sprite, Index = ParseSpriteIndex(sprite.name, spritePrefix) })
            .Where(item => item.Index >= 0)
            .ToDictionary(item => item.Index, item => item.Sprite);

        foreach (MotionRange motion in motions)
        {
            List<Sprite> sprites = new();
            for (int column = motion.StartColumn; column <= motion.EndColumn; column++)
            {
                int index = motion.Row * Columns + column;
                if (spritesByIndex.TryGetValue(index, out Sprite sprite))
                    sprites.Add(sprite);
            }

            CreateAnimationClip(clipFolder, motion.Name, sprites, frameRate);
        }
    }

    private static int ParseSpriteIndex(string spriteName, string prefix)
    {
        string expectedPrefix = $"{prefix}_";
        if (!spriteName.StartsWith(expectedPrefix))
            return -1;

        return int.TryParse(spriteName.Substring(expectedPrefix.Length), out int index) ? index : -1;
    }

    private static void CreateAnimationClip(string clipFolder, string motionName, List<Sprite> sprites, float frameRate)
    {
        if (sprites.Count == 0)
            return;

        AnimationClip clip = new();
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
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

        string clipPath = $"{clipFolder}/{motionName}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
            return;
        }

        EditorUtility.CopySerialized(clip, existing);
        EditorUtility.SetDirty(existing);
    }

    private static void EnsureFolder(string folderPath)
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
