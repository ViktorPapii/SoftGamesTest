using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoftGames.MagicWords
{
    // Token to Unicode emoji table. The v3 endpoint ships {tokens} but no emoji of its own.
    [CreateAssetMenu(fileName = "EmojiCatalog", menuName = "SoftGames/Emoji Catalog")]
    public class EmojiCatalog : ScriptableObject, IEmojiResolver
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("Token as it appears between braces in the payload, without the braces.")]
            public string token;

            [Tooltip("Unicode emoji rendered in its place.")]
            public string emoji;
        }

        [SerializeField] private Entry[] entries;

        private Dictionary<string, string> _map;

        public bool TryResolve(string token, out string emoji)
        {
            if (_map == null)
            {
                BuildMap();
            }

            return _map.TryGetValue(token, out emoji);
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (entries == null)
            {
                return;
            }

            foreach (Entry entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.token) && !string.IsNullOrEmpty(entry.emoji))
                {
                    _map[entry.token.Trim()] = entry.emoji;
                }
            }
        }

        private void OnValidate()
        {
            _map = null;
        }
    }
}
