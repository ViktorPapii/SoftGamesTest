using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace SoftGames.MagicWords.EditorTools
{
    /// <summary>
    /// Rebuilds the TextMeshPro sprite asset from the emoji atlas. Each cell is named after its
    /// Unicode codepoint and registered under it, so a plain emoji character in a string resolves to
    /// the coloured sprite instead of rendering as tofu in a build with no system emoji font.
    /// </summary>
    public static class EmojiSpriteAssetBuilder
    {
        private const string AtlasPath = "Assets/Fonts/Emoji/EmojiAtlas.png";
        private const string SpriteAssetPath = "Assets/Fonts/Emoji/EmojiAtlas.asset";
        private const int CellSize = 64;
        private const int Columns = 3;

        // Grid order, left to right and top to bottom. Mirrors Assets/Fonts/Emoji/README.md.
        private static readonly string[] Codepoints =
        {
            "1F60C", "1F928", "1F610", "1F44D", "1F602", "1F3C6"
        };

        [MenuItem("Tools/SoftGames/Rebuild Emoji Sprite Asset")]
        public static void Rebuild()
        {
            if (!SliceAtlas(out Texture2D texture))
            {
                return;
            }

            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);

            if (spriteAsset == null)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, SpriteAssetPath);
            }

            spriteAsset.spriteSheet = texture;
            spriteAsset.faceInfo = new FaceInfo
            {
                familyName = "Emoji",
                pointSize = CellSize,
                scale = 1f,
                lineHeight = CellSize,
                ascentLine = CellSize,
                baseline = 0f,
                descentLine = 0f
            };

            BuildTables(spriteAsset);
            AssignMaterial(spriteAsset, texture);

            spriteAsset.UpdateLookupTables();
            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"Emoji sprite asset rebuilt with {spriteAsset.spriteCharacterTable.Count} glyphs.", spriteAsset);
        }

        private static bool SliceAtlas(out Texture2D texture)
        {
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (texture == null)
            {
                Debug.LogError($"No emoji atlas at {AtlasPath}.");
                return false;
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            int rows = Mathf.CeilToInt(Codepoints.Length / (float)Columns);
            List<SpriteMetaData> sprites = new(Codepoints.Length);

            for (int i = 0; i < Codepoints.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;

                sprites.Add(new SpriteMetaData
                {
                    name = Codepoints[i],
                    // Texture space is bottom-left origin; the atlas is laid out top-left.
                    rect = new Rect(column * CellSize, (rows - 1 - row) * CellSize, CellSize, CellSize),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = Vector2.zero
                });
            }

#pragma warning disable CS0618 // spritesheet is the only writable slicing API for a Multiple sprite.
            importer.spritesheet = sprites.ToArray();
#pragma warning restore CS0618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            return texture != null;
        }

        private static void BuildTables(TMP_SpriteAsset spriteAsset)
        {
            List<TMP_SpriteGlyph> glyphs = new();
            List<TMP_SpriteCharacter> characters = new();

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
            Dictionary<string, Sprite> sprites = new();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites[sprite.name] = sprite;
                }
            }

            for (uint i = 0; i < Codepoints.Length; i++)
            {
                string name = Codepoints[i];
                if (!sprites.TryGetValue(name, out Sprite sprite))
                {
                    Debug.LogWarning($"Atlas has no sprite named {name}; skipped.");
                    continue;
                }

                Rect rect = sprite.rect;
                TMP_SpriteGlyph glyph = new()
                {
                    index = i,
                    sprite = sprite,
                    scale = 1f,
                    glyphRect = new GlyphRect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height),
                    metrics = new GlyphMetrics(rect.width, rect.height, 0f, rect.height, rect.width)
                };

                uint unicode = uint.Parse(name, NumberStyles.HexNumber);
                characters.Add(new TMP_SpriteCharacter(unicode, glyph) { name = name, scale = 1f });
                glyphs.Add(glyph);
            }

            // Both tables are read-only properties; the lists themselves are what gets serialised.
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteGlyphTable.AddRange(glyphs);
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteCharacterTable.AddRange(characters);
        }

        private static void AssignMaterial(TMP_SpriteAsset spriteAsset, Texture2D texture)
        {
            Material material = spriteAsset.material;

            if (material == null)
            {
                material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "EmojiAtlas Material" };
                spriteAsset.material = material;
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
            }

            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
        }
    }
}
