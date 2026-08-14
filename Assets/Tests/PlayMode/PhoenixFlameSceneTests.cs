using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SoftGames.Tests
{
    /// <summary>
    /// Presses the scene's colour button and watches the fire change colour. The chain under test is
    /// the whole one — button, animator, palette, particle systems — and none of it is named here,
    /// so the test judges the result rather than the wiring.
    /// </summary>
    public class PhoenixFlameSceneTests
    {
        private const string ScenePath = "Assets/Scenes/PhoenixFlame.unity";

        [UnityTest]
        public IEnumerator ColourButtonChangesTheFiresColour()
        {
            SceneManager.LoadScene(ScenePath);
            yield return null;
            yield return null;

            ParticleSystem body = GameObject.Find("Flame/Body").GetComponent<ParticleSystem>();
            Button button = GameObject.Find("UI/ColourButton").GetComponent<Button>();

            Color before = body.main.startColor.colorMin;
            button.onClick.Invoke();

            // Longer than the fade clip, so the blend has finished.
            yield return new WaitForSeconds(2f);

            Color after = body.main.startColor.colorMin;
            float moved = Mathf.Abs(after.r - before.r) + Mathf.Abs(after.g - before.g) + Mathf.Abs(after.b - before.b);

            // Deliberately not "orange to green": the palettes are an asset anyone may retune, and
            // what the button promises is a different colour, not a particular one.
            Assert.Greater(moved, 0.5f, $"The fire barely moved: {before} to {after}.");
        }

        // Without this the fire keeps burning in whatever runs next.
        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            Scene empty = SceneManager.CreateScene("AfterPhoenixFlame");
            SceneManager.SetActiveScene(empty);

            yield return SceneManager.UnloadSceneAsync(ScenePath);
        }
    }
}
