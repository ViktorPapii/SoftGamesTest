using System;
using UnityEngine;

namespace SoftGames.PhoenixFlame
{
    /// <summary>
    /// The colours the fire walks through, in order. Colour values can be edited freely, including
    /// while the game runs. Adding, removing or renaming an entry is not free: the animator states
    /// in FlameColor.controller are named after these entries, so a matching state has to be added
    /// or renamed by hand. FlamePaletteGraphTests fails if the two drift.
    /// </summary>
    [CreateAssetMenu(menuName = "SoftGames/Flame Palette Set", fileName = "FlamePalettes")]
    public class FlamePaletteSet : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("Shown beside the colour button.")]
            public string name;

            public FlamePalette palette;
        }

        [SerializeField] private Entry[] entries;

        public int Count => entries?.Length ?? 0;

        public string NameAt(int index) => entries[index].name;

        public FlamePalette PaletteAt(int index) => entries[index].palette;
    }
}
