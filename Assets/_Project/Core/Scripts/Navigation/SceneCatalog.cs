using System;
using UnityEngine;

namespace SoftGames.Core
{
    // The one place a scene is named. Menu rows are authored in the scene and checked against
    // Games by MenuEntryTests; the navigator resolves every scene name through here.
    [CreateAssetMenu(fileName = "SceneCatalog", menuName = "SoftGames/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        /// <summary>
        /// Which scene, and how the menu presents it. SceneAsset is editor-only, so it is baked to
        /// a name on validate; renaming or moving the scene updates it.
        /// </summary>
        [Serializable]
        public class SceneInfo
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
        [SerializeField] private SceneInfo mainMenu;

        [Tooltip("One per task, in the order the menu should list them.")]
        [SerializeField] private SceneInfo[] games = Array.Empty<SceneInfo>();

        public SceneInfo MainMenu => mainMenu;

        public SceneInfo[] Games => games;

#if UNITY_EDITOR
        private void OnValidate()
        {
            mainMenu?.SyncSceneName();

            foreach (SceneInfo scene in games)
            {
                scene?.SyncSceneName();
            }
        }
#endif
    }
}
