using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace SoftGames.Core.Tests
{
    // A scene in the catalog but not in Build Settings is a menu button that throws on click, and
    // nothing shows it until someone presses it.
    public class SceneCatalogTests
    {
        private const string CatalogPath = "Assets/_Project/Core/ScriptableObjects/SceneCatalog.asset";

        private SceneCatalog _catalog;
        private HashSet<string> _builtScenes;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);

            _builtScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToHashSet();
        }

        [Test]
        public void CatalogExists()
        {
            Assert.IsNotNull(_catalog, $"No scene catalog at {CatalogPath}.");
        }

        [Test]
        public void MainMenuIsInBuildSettings()
        {
            Assert.IsNotNull(_catalog.MainMenu, "The catalog has no main menu.");
            Assert.Contains(_catalog.MainMenu.SceneName, _builtScenes.ToList(),
                "The Exit button on every scene returns here, so it has to ship.");
        }

        [Test]
        public void MainMenuLoadsFirst()
        {
            string first = Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path);
            Assert.AreEqual(_catalog.MainMenu.SceneName, first,
                "Build index 0 is what a player sees on launch.");
        }

        [Test]
        public void EveryGameIsInBuildSettings()
        {
            Assert.IsNotEmpty(_catalog.Games, "The catalog lists no games; the menu would be empty.");

            foreach (SceneCatalog.SceneInfo scene in _catalog.Games)
            {
                Assert.IsNotEmpty(scene.SceneName, "A catalog entry has no scene assigned.");
                Assert.Contains(scene.SceneName, _builtScenes.ToList(),
                    $"'{scene.SceneName}' is on the menu but not in Build Settings, so its button " +
                    "would throw on click. Add it under File > Build Profiles.");
            }
        }

        [Test]
        public void EveryGameHasATitle()
        {
            foreach (SceneCatalog.SceneInfo scene in _catalog.Games)
            {
                Assert.IsNotEmpty(scene.Title, $"'{scene.SceneName}' would show a blank menu button.");
            }
        }
    }
}
