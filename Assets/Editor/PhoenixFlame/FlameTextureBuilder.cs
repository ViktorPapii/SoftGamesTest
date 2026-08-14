using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoftGames.PhoenixFlame.EditorTools
{
    /// <summary>
    /// Draws the shapes that are not painted art: a dot for the embers, a soft falloff for the
    /// ground glow, and the backdrop the scene sits on. Flame and smoke are flipbook sheets instead
    /// — see Assets/Images/PhoenixFlame/README.md.
    ///
    /// Particle textures are white with the shape in the alpha channel — colour arrives per particle
    /// from <see cref="FlamePalette"/>, so one texture serves every colour in the cycle.
    /// </summary>
    public static class FlameTextureBuilder
    {
        private const string TextureFolder = "Assets/Images/PhoenixFlame";
        private const string MaterialFolder = "Assets/Materials/PhoenixFlame";
        private const string ShaderName = "SoftGames/Flame Particle";

        [MenuItem("Tools/SoftGames/Phoenix Flame/Rebuild Textures And Materials")]
        public static void Rebuild()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Phoenix Flame: shader '{ShaderName}' not found; materials would import pink.");
                return;
            }

            EditorFolders.Ensure(TextureFolder);
            EditorFolders.Ensure(MaterialFolder);

            Texture2D ember = WriteParticle("ember_dot", 64, (x, y) => Mask(Dot(x, y)));
            Texture2D glow = WriteParticle("flame_glow", 256, (x, y) => Mask(Glow(x, y)));
            WriteSprite("backdrop", 512, Backdrop);

            CreateMaterial(shader, "Ember_Additive", ember, BlendMode.One, BlendMode.One);
            CreateMaterial(shader, "Glow_Additive", glow, BlendMode.One, BlendMode.One);

            AssetDatabase.SaveAssets();
            Debug.Log($"Phoenix Flame: textures written to {TextureFolder}, materials to {MaterialFolder}.");
        }

        private static float Dot(float x, float y)
        {
            float edge = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y));

            return Mathf.Pow(edge, 3f) * 0.7f + Mathf.Pow(edge, 12f);
        }

        private static float Glow(float x, float y)
        {
            return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)), 2.4f);
        }

        // Night sky the fire is seen against: a lit sprite, so the flame's 2D light plays over it.
        private static Color Backdrop(float x, float y)
        {
            Color edge = new(0.02f, 0.022f, 0.04f);
            Color middle = new(0.17f, 0.19f, 0.29f);

            float centre = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)), 1.4f);

            // A little grain: a gradient this smooth bands visibly at 8 bits per channel.
            float grain = (Mathf.PerlinNoise(x * 6f + 71f, y * 6f + 23f) - 0.5f) * 0.012f;

            return Color.Lerp(edge, middle, centre) + new Color(grain, grain, grain, 0f);
        }

        private static Color Mask(float alpha) => new(1f, 1f, 1f, Mathf.Clamp01(alpha));

        private static Texture2D WriteParticle(string name, int size, Func<float, float, Color> shade)
        {
            string path = Write(name, size, shade);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void WriteSprite(string name, int size, Func<float, float, Color> shade)
        {
            string path = Write(name, size, shade);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        // Shades every pixel over -1..1 in both axes and writes the result as a PNG.
        private static string Write(string name, int size, Func<float, float, Color> shade)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, mipChain: false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    pixels[y * size + x] = shade(nx, ny);
                }
            }

            texture.SetPixels(pixels);

            string path = $"{TextureFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return path;
        }

        private static void CreateMaterial(Shader shader, string name, Texture2D texture,
            BlendMode source, BlendMode destination)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.mainTexture = texture;
            material.SetFloat("_SrcBlend", (float)source);
            material.SetFloat("_DstBlend", (float)destination);
            EditorUtility.SetDirty(material);
        }
    }
}
