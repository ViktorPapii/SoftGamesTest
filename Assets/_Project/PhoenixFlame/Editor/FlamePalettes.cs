using UnityEngine;

namespace SoftGames.PhoenixFlame.EditorTools
{
    /// <summary>
    /// The three looks the brief asks for, in the order the button walks them. They are the source
    /// for both the animator clips and the colours the prefab ships with, so the fire is orange in
    /// the editor without entering play mode.
    /// </summary>
    internal static class FlamePalettes
    {
        internal static readonly (string Name, FlamePalette Palette)[] Cycle =
        {
            ("Orange", new FlamePalette
            {
                core = new Color(1f, 0.93f, 0.62f, 1f),
                body = new Color(1f, 0.42f, 0.08f, 1f),
                ember = new Color(1f, 0.7f, 0.28f, 1f),
                smoke = new Color(0.2f, 0.17f, 0.15f, 0.35f),
                glow = new Color(1f, 0.52f, 0.16f, 0.3f)
            }),
            ("Green", new FlamePalette
            {
                core = new Color(0.72f, 1f, 0.6f, 1f),
                body = new Color(0.11f, 0.72f, 0.2f, 1f),
                ember = new Color(0.55f, 1f, 0.42f, 1f),
                smoke = new Color(0.15f, 0.2f, 0.16f, 0.35f),
                glow = new Color(0.28f, 1f, 0.42f, 0.3f)
            }),
            ("Blue", new FlamePalette
            {
                core = new Color(0.66f, 0.86f, 1f, 1f),
                body = new Color(0.1f, 0.34f, 0.95f, 1f),
                ember = new Color(0.5f, 0.76f, 1f, 1f),
                smoke = new Color(0.15f, 0.17f, 0.22f, 0.35f),
                glow = new Color(0.24f, 0.55f, 1f, 0.3f)
            })
        };
    }
}
