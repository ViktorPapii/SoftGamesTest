using System;
using UnityEngine;

namespace SoftGames.Core
{
    // The one place a scene is named. The menu builds its buttons from Games; the navigator
    // resolves every scene name through here.
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "SoftGames/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        /// <summary>
        /// A scene plus how the menu presents it. SceneAsset is editor-only, so it is baked to a
        /// name on validate; renaming or moving the scene updates the entry.
        /// </summary>
        [Serializable]
        public class Entry
        {
#if UNITY_EDITOR
            [Tooltip("Drag the scene here. Its name is baked into the field below on validate.")]
            [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif

            [Tooltip("Baked from the scene asset. Must also be listed in Build Settings.")]
            [SerializeField] private string sceneName;

            [SerializeField] private string title;

            [Tooltip("One line under the title on the menu button.")]
            [SerializeField] private string tagline;

            public string SceneName => sceneName;

            public string Title => string.IsNullOrWhiteSpace(title) ? sceneName : title;

            public string Tagline => tagline;

#if UNITY_EDITOR
            internal void SyncSceneName()
            {
                if (sceneAsset != null)
                {
                    sceneName = sceneAsset.name;
                }
            }
#endif
        }

        [Tooltip("Where the Exit button in every game scene returns to.")]
        [SerializeField] private Entry mainMenu;

        [Tooltip("One per task, in the order the menu should list them.")]
        [SerializeField] private Entry[] games = Array.Empty<Entry>();

        public Entry MainMenu => mainMenu;

        public Entry[] Games => games;

#if UNITY_EDITOR
        private void OnValidate()
        {
            mainMenu?.SyncSceneName();

            foreach (Entry entry in games)
            {
                entry?.SyncSceneName();
            }
        }
#endif
    }
}
