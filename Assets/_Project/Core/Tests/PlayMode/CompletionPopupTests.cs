using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SoftGames.Core.Tests
{
    // Retry has to run the callback the game handed over; a Retry that only closes the popup is
    // the failure this replaced.
    public class CompletionPopupTests
    {
        [UnityTest]
        public IEnumerator RetryRunsTheCallbackTheGameSupplied()
        {
            yield return TestSession.Enter();

            ICompletionPopup popup = GameManager.Instance.Completion;
            int retries = 0;

            popup.Show("Finished.", () => retries++);
            yield return null;

            Assert.IsTrue(popup.IsOpen, "Show left the popup closed.");

            Click("RetryButton");
            yield return null;

            Assert.AreEqual(1, retries, "Retry did not run the game's restart.");
            Assert.IsFalse(popup.IsOpen, "Retry left the popup open on top of the restarted game.");
        }

        [UnityTest]
        public IEnumerator RetryIsHiddenWhenAGameOffersNoRestart()
        {
            yield return TestSession.Enter();

            ICompletionPopup popup = GameManager.Instance.Completion;
            popup.Show("Nothing to retry.", null);
            yield return null;

            Assert.IsFalse(Find("RetryButton").gameObject.activeSelf,
                "A popup with no retry callback still offered a Retry button.");

            popup.Dismiss();
        }

        [UnityTest]
        public IEnumerator RetryCallbackDoesNotSurviveASceneChange()
        {
            yield return TestSession.Enter();

            ICompletionPopup popup = GameManager.Instance.Completion;
            int retries = 0;

            // Standing in for a controller that the next load is about to destroy.
            popup.Show("Finished.", () => retries++);
            yield return TestSession.Navigate("PhoenixFlame");

            Assert.IsFalse(popup.IsOpen, "The popup rode along into the next scene.");

            // Invoked directly rather than through a pointer click: Button.Press bails on
            // IsInteractable, which the dismissed CanvasGroup already makes false, so a pointer
            // click would pass whether or not the callback was ever dropped. Going straight to
            // onClick runs the popup's own handler, which is the thing under test.
            Find("RetryButton").onClick.Invoke();
            yield return null;

            Assert.AreEqual(0, retries,
                "The previous scene's retry callback was still wired up after a scene change.");

            yield return TestSession.ReturnToMenu();
        }

        [UnityTest]
        public IEnumerator MenuButtonLeavesForTheMenu()
        {
            yield return TestSession.Enter();
            yield return TestSession.Navigate("AceOfShadows");

            ICompletionPopup popup = GameManager.Instance.Completion;
            popup.Show("Finished.", null);
            yield return null;

            Click("MenuButton");
            yield return null;

            IGameNavigation navigation = GameManager.Instance.Navigation;
            Assert.IsTrue(navigation.IsBusy, "The popup's Menu button started no transition.");

            while (navigation.IsBusy)
            {
                yield return null;
            }

            Assert.IsTrue(navigation.IsOnMenu, "The transition ended somewhere other than the menu.");
        }

        // Searched from the persistent root, so the test needs no concrete popup type.
        private static Button Find(string name)
        {
            Button button = GameManager.Instance
                .GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == name);

            Assert.IsNotNull(button, $"No '{name}' on the persistent root.");
            return button;
        }

        private static void Click(string name)
        {
            Button button = Find(name);
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
    }
}
