using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace SoftGames.AceOfShadows
{
    /// <summary>
    /// Flat array of card face sprites, filled by scanning a folder. Nothing here compares or
    /// matches cards, so there is no suit/rank lookup.
    /// </summary>
    [CreateAssetMenu(fileName = "CardDeck", menuName = "Ace of Shadows/Card Deck")]
    public class CardDeckAsset : ScriptableObject
    {
        [SerializeField] private Sprite[] faces = Array.Empty<Sprite>();

        [Header("Editor import")]
        [Tooltip("Folder scanned by the 'Load Sprites From Folder' context menu.")]
        [SerializeField] private string sourceFolder = "Assets/_Project/AceOfShadows/Art/Images";

        public bool IsValid => faces is { Length: > 0 };

        // Face for a card id. Wraps: 144 cards draw from ~53 sprites, so faces repeat.
        public Sprite GetFace(int cardId)
        {
            if (!IsValid)
            {
                return null;
            }

            return faces[cardId % faces.Length];
        }

#if UNITY_EDITOR
        // Fills faces from sourceFolder. Re-run after adding or renaming art.
        [ContextMenu("Load Sprites From Folder")]
        private void LoadSpritesFromFolder()
        {
            if (!AssetDatabase.IsValidFolder(sourceFolder))
            {
                Debug.LogError($"[{name}] '{sourceFolder}' is not a valid project folder.", this);
                return;
            }

            List<Sprite> loaded = new();

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { sourceFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (sprite == null)
                {
                    continue;
                }

                loaded.Add(sprite);
            }

            // FindAssets order is undefined; sort for a stable asset diff.
            loaded.Sort(static (a, b) => string.CompareOrdinal(a.name, b.name));
            faces = loaded.ToArray();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);

            Debug.Log($"[{name}] Imported {faces.Length} face sprites from '{sourceFolder}'.", this);
        }
#endif
    }
}