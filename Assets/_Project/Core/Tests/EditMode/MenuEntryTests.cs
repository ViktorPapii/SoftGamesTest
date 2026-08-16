using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoftGames.Core.Tests
{
    // Menu rows are authored in the scene now, so nothing regenerates them from the catalog. This
    // is what catches a row pointing at a scene that was renamed, removed or never shipped.
    public class MenuEntryTests
    {
        private const string ScenePath = "Assets/_Project/Core/Scenes/MainMenu.unity";
        private const string CatalogPath = "Assets/_Project/Core/ScriptableObjects/SceneCatalog.asset";

        private MenuEntryButton[] _rows;
        private SceneCatalog _catalog;

        [OneTimeSetUp]
        public void OpenMenu()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _rows = Object.FindObjectsByType<MenuEntryButton>(FindObjectsInactive.Include);
            _catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);
        }

        [Test]
        public void MenuHasRows()
        {
            Assert.IsNotEmpty(_rows, "The menu scene has no MenuEntryButton; it would come up empty.");
        }

        [Test]
        public void EveryRowNamesASceneInTheCatalog()
        {
            string[] known = _catalog.Games.Select(entry => entry.SceneName).ToArray();

            foreach (MenuEntryButton row in _rows)
            {
                Assert.Contains(row.SceneName, known,
                    $"'{row.name}' opens '{row.SceneName}', which is not a game in the catalog.");
            }
        }

        [Test]
        public void EveryRowNamesABuiltScene()
        {
            string[] built = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            foreach (MenuEntryButton row in _rows)
            {
                Assert.Contains(row.SceneName, built,
                    $"'{row.name}' opens '{row.SceneName}', which is not in Build Settings — the " +
                    "button would throw on click.");
            }
        }

        [Test]
        public void EveryCatalogGameHasARow()
        {
            string[] onMenu = _rows.Select(row => row.SceneName).ToArray();

            foreach (SceneCatalog.Entry entry in _catalog.Games)
            {
                Assert.Contains(entry.SceneName, onMenu,
                    $"'{entry.SceneName}' is in the catalog but has no row in the menu scene, so " +
                    "there is no way to reach it.");
            }
        }

        [Test]
        public void EveryRowHasItsButton()
        {
            foreach (MenuEntryButton row in _rows)
            {
                Assert.IsNotNull(row.Button, $"'{row.name}' has no Button assigned; it cannot be clicked.");
            }
        }
    }
}
