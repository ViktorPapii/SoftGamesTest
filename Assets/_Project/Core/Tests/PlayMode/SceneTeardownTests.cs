using System.Collections;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace SoftGames.Core.Tests
{
    // What happens when a scene is left. Each drives the scene long enough to have tweens running
    // first, and one test asserts exactly that, to keep the others honest.
    public class SceneTeardownTests
    {
        // Long enough for Ace of Shadows to launch cards and leave the decks mid-restack.
        private const float DwellSeconds = 2.5f;

        [UnityTest]
        public IEnumerator BootstrapBuildsThePersistentManager()
        {
            yield return TestSession.Enter();

            Assert.IsNotNull(GameManager.Instance, "The scene's GameBootstrap produced no GameManager.");
            Assert.IsNotNull(GameManager.Instance.Navigation, "GameManager has no navigator.");

            // Answering this at all means the navigator's catalog is wired; it reads the menu's
            // name out of it.
            Assert.IsTrue(GameManager.Instance.Navigation.IsOnMenu, "Entering did not land on the menu.");
            Assert.AreEqual("DontDestroyOnLoad", GameManager.Instance.gameObject.scene.name,
                "The manager did not mark itself DontDestroyOnLoad, so it dies with the menu.");
        }

        [UnityTest]
        public IEnumerator AceOfShadows_IsTweening_WhileItRuns()
        {
            yield return TestSession.Enter();
            yield return TestSession.Navigate("AceOfShadows");
            yield return new WaitForSeconds(DwellSeconds);

            Assert.Greater(DOTween.TotalPlayingTweens(), 0,
                "Expected cards in motion. Without this the teardown test below would pass for " +
                "the wrong reason.");

            yield return TestSession.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator LeavingAGameScene_LeavesNoTweenRunning(
            [ValueSource(nameof(GameSceneNames))] string sceneName)
        {
            yield return TestSession.Enter();
            yield return TestSession.Navigate(sceneName);
            yield return new WaitForSeconds(DwellSeconds);
            yield return TestSession.ReturnToMenu();

            // DOTween reaps killed tweens on its own update; give it a couple.
            yield return null;
            yield return null;

            Assert.AreEqual(0, DOTween.TotalPlayingTweens(),
                $"Tweens survived the unload of {sceneName}. One outliving its scene means a " +
                "missing SetLink, and it will run against a destroyed transform.");
        }

        [UnityTest]
        public IEnumerator EachScene_HasExactlyOneEventSystem(
            [ValueSource(nameof(GameSceneNames))] string sceneName)
        {
            yield return TestSession.Enter();
            yield return TestSession.Navigate(sceneName);

            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);

            Assert.AreEqual(1, systems.Length,
                $"{sceneName} has {systems.Length} EventSystems. Unity disables the extras, and " +
                "which one survives is not something to rely on.");

            yield return TestSession.ReturnToMenu();
        }

        private static string[] GameSceneNames() => new[] { "AceOfShadows", "MagicWords", "PhoenixFlame" };
    }
}
