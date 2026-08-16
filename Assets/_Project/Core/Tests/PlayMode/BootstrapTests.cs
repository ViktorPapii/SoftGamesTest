using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoftGames.Core.Tests
{
    // Every shipped scene must stand up a GameManager by itself. A scene that forgot its
    // GameBootstrap fails silently: no HUD, no way back, no EventSystem, and nothing logged.
    public class BootstrapTests
    {
        // Tears the manager down first, or the DontDestroyOnLoad one from an earlier test would
        // satisfy every case.
        [UnityTest]
        public IEnumerator EveryBuiltScene_StandsUpOnItsOwn(
            [ValueSource(nameof(BuiltSceneNames))] string sceneName)
        {
            if (GameManager.Instance != null)
            {
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            }

            Assert.IsNull(GameManager.Instance, "Tearing the manager down did not clear the singleton.");

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            Assert.IsNotNull(GameManager.Instance,
                $"'{sceneName}' produced no GameManager. It is missing a GameBootstrap, or the one " +
                "it has has no manager prefab assigned — opened on its own it would have no HUD, no " +
                "way back to the menu and no working UI input.");

            Assert.IsNotNull(GameManager.Instance.Navigation, $"'{sceneName}' built a manager with no navigator.");
            Assert.IsNotNull(GameManager.Instance.Completion, $"'{sceneName}' built a manager with no popup.");
        }

        // Read from Build Settings, so a newly built scene is covered without editing this.
        private static string[] BuiltSceneNames() =>
            Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(index => System.IO.Path.GetFileNameWithoutExtension(
                    SceneUtility.GetScenePathByBuildIndex(index)))
                .ToArray();
    }
}
