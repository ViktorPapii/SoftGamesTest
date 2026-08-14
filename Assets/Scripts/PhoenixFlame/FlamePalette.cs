using System;
using UnityEngine;

namespace SoftGames.PhoenixFlame
{
    /// <summary>
    /// One look of the fire, layer by layer. Authored in a <see cref="FlamePaletteSet"/> and mixed
    /// with the next one while the fire changes colour.
    /// </summary>
    [Serializable]
    public struct FlamePalette
    {
        [Tooltip("Hottest part, at the base of the flame.")]
        public Color core;

        [Tooltip("The bulk of the flame.")]
        public Color body;

        [Tooltip("Sparks lifting off the top.")]
        public Color ember;

        [Tooltip("Smoke above the flame. Alpha carries most of its weight.")]
        public Color smoke;

        [Tooltip("Ground glow and the 2D light the fire casts.")]
        public Color glow;

        public static FlamePalette Lerp(in FlamePalette from, in FlamePalette to, float t) => new()
        {
            core = Color.Lerp(from.core, to.core, t),
            body = Color.Lerp(from.body, to.body, t),
            ember = Color.Lerp(from.ember, to.ember, t),
            smoke = Color.Lerp(from.smoke, to.smoke, t),
            glow = Color.Lerp(from.glow, to.glow, t)
        };
    }
}
